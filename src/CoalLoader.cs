using System.Collections;
using System.Diagnostics;
using System.Linq;
using DV;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.Utils;
using UnityEngine;
using VRTK;

namespace better_loading;

//all the animation and sound stuff is based on LocoResourceModule
public class CoalLoader: SingletonBehaviour<CoalLoader>
{
	private WarehouseMachineController machineController;
	private const CargoType loaderCargoType = CargoType.Coal;
	private CargoType_v2 loaderCargoType_V2;
	// How fast the cargo is loaded, in kg/s
	private const int loadSpeed = 5000;
	private LocoResourceModule coalModule;
	private GameObject shuteOpeningMarker;
	private Coroutine loadingUnloadingCoroutine;
	
	private const int MY_LAYER = 23;
	private LayerMask MY_LAYER_MASK = Extensions.LayerMaskFromInt(MY_LAYER);
	
	//position of the opening of the chute the coal falls out, relative to the coal loader object 
	private readonly Vector3 chuteOpeningPositionLocal = new (-85.976f, 8.554f, 1.999f);
	
	//approximate vertical distance of the chute to the track
	private const int CHUTE_HEIGHT = 10;
	
	//true if cargo is flowing into the car
	private bool isLoading;
	private Stopwatch stopwatch = new();
	private bool timeWasFlowing;

	private LayeredAudio audioSource;
	private const ResourceFlowMode flowMode = ResourceFlowMode.Air;
	private float flowVolume;
	private float curVolumeVelocity;
	
	private const string LOADING_SOUND_OBJECT_NAME = "LoadingSound";
	
	// private GameObject visualBox;

	// private ParticleSystem[] plugStartFlowEffects;
	// private ParticleSystem[] plugStopFlowEffects;
	
	public new static string AllowAutoCreate() => "[CoalLoader]";
	
	private void Start()
	{
		Main.Debug(nameof(CoalLoader)+".Start()");
		loaderCargoType_V2 = loaderCargoType.ToV2();
	}

	private void OnEnable()
	{
		Main.Debug(nameof(CoalLoader)+".OnEnable()");
	}

	private void Update()
	{
		//game paused?
		if (!TimeUtil.IsFlowing) return;
		
		DoSound();
		DoAnimations();
	}

	private void DoSound()
	{
		if(!audioSource) return;

		if (isLoading)
		{
			if (flowVolume < 1.0)
			{
				//increase volume
				flowVolume = Mathf.SmoothDamp(flowVolume, 1f, ref curVolumeVelocity, 0.15f);
			}
		}
		else if (flowVolume > 0.0)
		{
			//decrease volume
			flowVolume = Mathf.SmoothDamp(flowVolume, 0f, ref curVolumeVelocity, 0.15f);
		}
		
		audioSource.Set(flowVolume);
	}

	private void DoAnimations()
	{
		// if (isDraining != isDraining) //todo wasdraining?
		// {
		// 	if (isDraining)
		// 	{
		// 		System.Action drainStopped = DrainStopped;
		// 		if (drainStopped != null)
		// 			drainStopped();
		// 	}
		// 	else
		// 	{
		// 		System.Action drainStarted = DrainStarted;
		// 		if (drainStarted != null)
		// 			drainStarted();
		// 	}
		// }
	}

	private void OnDisable()
	{
		//TODO prevent unloading?
		Main.Debug(nameof(CoalLoader)+".OnDisable()");
	}
	
	public void EnterLoadingMode(WarehouseMachineController warehouseMachineController)
	{
		CleanUp();
		
		machineController = warehouseMachineController;
		
		var coalLoader = GameObject.Find("Industry_Coal");
		if (!coalLoader)
		{
			Main.Error("coal loader could not be found");
			return;
		}

		CreateShuteMarker(coalLoader.transform, chuteOpeningPositionLocal);
		
		//TODO this may fail on stations without steam service? doe het bij het laden, net zoals mapify
		
		//we will copy the coal loading animations and sounds from the tender coal loader
		coalModule = GameObject.Find("CoalLocoResourceModule")?.GetComponent<LocoResourceModule>();
		if (!coalModule)
		{
			Main.Error("coal loco module could not be found");
			return;
		}

		InitializeAudioSource(coalLoader.transform);

		// Instantiate(coalModule.plugFlowingEffects[(int)flowMode].gameObject, chuteOpeningPosition, Quaternion.identity);
		
		loadingUnloadingCoroutine = StartCoroutine(Loading());
	}

	private void InitializeAudioSource(Transform coalLoader)
	{
		var audioObject = coalLoader.Find(LOADING_SOUND_OBJECT_NAME)?.gameObject;
		if (!audioObject)
		{
			Main.Log("Creating audio object");
			var OG = coalModule.audioSourcesPerFlow[(int)flowMode];
			audioObject = Instantiate(OG.gameObject, coalLoader);
			audioObject.name = LOADING_SOUND_OBJECT_NAME;
			audioObject.transform.position = shuteOpeningMarker.transform.position;
		}

		audioSource = audioObject.GetComponentInChildren<LayeredAudio>();
	}

	private void CreateShuteMarker(Transform parent, Vector3 localPosition)
	{
		shuteOpeningMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
		shuteOpeningMarker.name = "shuteOpeningMarker";
		//invisible 
		Destroy(shuteOpeningMarker.GetComponent<MeshRenderer>());
		shuteOpeningMarker.layer = MY_LAYER;
		
		shuteOpeningMarker.transform.SetParent(parent);
		shuteOpeningMarker.transform.localPosition = localPosition;
		shuteOpeningMarker.transform.localEulerAngles = Vector3.zero;
	}

	private void CleanUp()
	{
		if (shuteOpeningMarker)
		{
			Destroy(shuteOpeningMarker);
		}
		
		if (loadingUnloadingCoroutine != null)
		{
			StopCoroutine(loadingUnloadingCoroutine);
		}
	}

	public IEnumerator Loading()
	{
		Main.Debug(nameof(Loading));
		
		machineController.SetScreen(WarehouseMachineController.TextPreset.ClearDesc);
		machineController.displayTitleText.text = "Loading, jwz";
		
		machineController.machineSound.Play(machineController.transform.position, parent: machineController.transform);
		
		machineController.LoadOrUnloadOngoing = true;

		while (true)
		{
			//game is paused?
			if (TimeUtil.IsFlowing)
			{
				if (!timeWasFlowing)
				{
					stopwatch.Start();
					timeWasFlowing = true;
				}
			}
			else
			{
				if (timeWasFlowing)
				{
					//stop = pause
					stopwatch.Stop();
					timeWasFlowing = false;
				}
				
				yield return null;
				continue;
			}
			
			//wait for 2 frames
			yield return 2;

			var carUnderLoader = DetectCarUnderLoader();

			if (!carUnderLoader)
			{
				StopLoading("there is no car");
				continue;
			}
			
			Main.Debug($"car under loader: {carUnderLoader.carType}");
			
			if (!ShouldLoadCar(carUnderLoader))
			{
				StopLoading("should not load car");
				continue;
			}
			
			DoLoadStep(carUnderLoader);
		}
	}
	
	private TrainCar DetectCarUnderLoader()
	{
		var carsOnLoadTrack = machineController.warehouseTrack.BogiesOnTrack()
			.Select(bogie => bogie._car)
			.Distinct()
			.ToList();
		
		//no cars on track
		if(!carsOnLoadTrack.Any()) return null;

		var closestCar = carsOnLoadTrack
			.OrderBy(car => Vector3.Distance(car.transform.position, shuteOpeningMarker.transform.position))
			.First();

		var carCollider = closestCar.carColliders.collisionRoot.GetComponent<BoxCollider>();
		if (!carCollider)
		{
			//this happens on BE2
			return null;
		}

		var boxSize = new Vector3(carCollider.size.x, CHUTE_HEIGHT*1.5f, carCollider.size.z);
		//extend up
		var boxCenter = carCollider.transform.TransformPoint(carCollider.center) + new Vector3(0, boxSize.y/2f, 0);
		var overlaps = Physics.OverlapBox(boxCenter, boxCenter/2f, carCollider.transform.rotation, MY_LAYER_MASK);

		// if (!visualBox)
		// {
		// 	visualBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
		// 	visualBox.name = "visualBox";
		// 	Destroy(visualBox.GetComponent<BoxCollider>());
		// }
		// visualBox.transform.position = boxCenter;
		// visualBox.transform.rotation = carCollider.transform.rotation;
		// visualBox.transform.SetGlobalScale(boxSize);
		
		if (overlaps.FirstOrDefault(coll => coll.gameObject == shuteOpeningMarker.gameObject))
		{
			//this car is under the chute
			return closestCar;
		}
		
		return null;
	}

	//TODO don't load tenders
	private bool ShouldLoadCar(TrainCar aCar)
	{
		if (!aCar.CanLoad(loaderCargoType_V2))
		{
			Main.Debug($"{aCar.carType} can't contain {loaderCargoType_V2}");
			return false;
		}
			
		var logicCar = aCar.logicCar;

		var cargoInCar = logicCar.CurrentCargoTypeInCar;
		if (cargoInCar != CargoType.None && cargoInCar.ToV2() != loaderCargoType_V2)
		{
			Main.Debug($"{aCar.carType} can't load {loaderCargoType_V2} because it already contains {cargoInCar}");
			return false;
		}
			
		Main.Debug($"LoadedCargoAmount: {logicCar.LoadedCargoAmount} capacity: {logicCar.capacity}");
		
		//full
		if (logicCar.LoadedCargoAmount >= logicCar.capacity)
		{
			Main.Debug($"{aCar.carType} can't load because it is full");
			return false;
		}

		return true;
	}

	private void DoLoadStep(TrainCar carToLoad)
	{
		StartLoading();
		
		var logicCar = carToLoad.logicCar;

		stopwatch.Stop();
		var kgToLoad = loadSpeed * (float)stopwatch.Elapsed.TotalSeconds;
		stopwatch.Restart();
		
		// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
		var unitsToLoad = kgToLoad / loaderCargoType_V2.massPerUnit;
		
		Main.Debug($"{nameof(DoLoadStep)}: {kgToLoad} kg, {unitsToLoad} units");
			
		// prevent overfill
		if (logicCar.LoadedCargoAmount + unitsToLoad >= logicCar.capacity)
		{
			Main.Debug($"{nameof(DoLoadStep)}: {logicCar.LoadedCargoAmount} + {unitsToLoad} >= {logicCar.capacity} ");
			
			//fill to capacity
			unitsToLoad = logicCar.capacity - logicCar.LoadedCargoAmount;
			Main.Debug($"{nameof(DoLoadStep)}: {unitsToLoad} units");
		}
		
		// this prevents an exception in LoadCargo
		logicCar.CurrentCargoTypeInCar = CargoType.None;
		logicCar.LoadCargo(unitsToLoad, loaderCargoType, machineController.warehouseMachine);
		
		machineController.SetScreen(WarehouseMachineController.TextPreset.CarUpdated, true, carToLoad.ID, logicCar, loaderCargoType_V2);
	}

	private void StartLoading()
	{
		if(isLoading) return;
		
		Main.Debug(nameof(StartLoading));
		
		// foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		// 	raycastFlowingEffect.Play();
		// 	
		// foreach (ParticleSystem raycastStartFlowEffect in raycastStartFlowEffects)
		// 	raycastStartFlowEffect.Play();
		
		isLoading = true;
		stopwatch = Stopwatch.StartNew();
	}
	
	private void StopLoading(string reason = "")
	{
		if(!isLoading) return;
		
		Main.Debug($"{nameof(StopLoading)}, {reason}");
		
		// foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		// 	raycastFlowingEffect.Stop();
		// 	
		// foreach (ParticleSystem raycastStopFlowEffect in raycastStopFlowEffects)
		// 	raycastStopFlowEffect.Play();
		
		isLoading = false;
		stopwatch.Reset();
	}
	
	
	//*/

	//todo was hier niet al een functie voor
	// public void StopLoad()
	// {
	//  machineController.LoadOrUnloadOngoing = false;
	//  // StopCoroutine();
	// }
	
}