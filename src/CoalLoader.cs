using System;
using System.Collections;
using DV;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace better_loading;

//all the animation and sound stuff is based on LocoResourceModule
public class CoalLoader: SingletonBehaviour<CoalLoader>
{
	private WarehouseMachineController machineController;
	private const CargoType loaderCargoType = CargoType.Coal;
	private CargoType_v2 loaderCargoType_V2;
	// How fast the cargo is loaded, in kg/s
	private const int loadSpeed = 500;
	private LocoResourceModule coalModule;
	private GameObject cube;
	private Coroutine loadingUnloadingCoroutine;
	
	//position of the opening of the chute the coal falls out, relative to the coal loader object 
	private readonly Vector3 chuteOpeningPositionLocal = new (-85.976f, 8.554f, 1.999f);
	
	//true if cargo is flowing into the car
	private bool isFlowing;

	private LayeredAudio audioSource;
	private const ResourceFlowMode flowMode = ResourceFlowMode.Air;
	private float flowVolume;
	private float curVolumeVelocity;
	
	private const string LOADING_SOUND_OBJECT_NAME = "LoadingSound";

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
		if (!TimeUtil.IsFlowing) return;
		DoSound();
		DoAnimations();
	}

	private void DoSound()
	{
		if(!audioSource) return;

		if (isFlowing)
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
			flowVolume = Mathf.SmoothDamp(flowVolume, 0.0f, ref curVolumeVelocity, 0.15f);
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
	
	public void StartLoadingUnloading(WarehouseMachineController warehouseMachineController, bool isLoading)
	{
		CleanUp();
		
		machineController = warehouseMachineController;
		
		var coalLoader = GameObject.Find("Industry_Coal");
		if (!coalLoader)
		{
			Main.Error("coal loader could not be found");
			return;
		}

		CreateTestCube(coalLoader.transform, chuteOpeningPositionLocal);
		
		//TODO this may fail on stations without steam service? doe het bij het laden, net zoals mapify
		
		//we will copy the coal loading animations from the tender coal loader
		coalModule = GameObject.Find("CoalLocoResourceModule")?.GetComponent<LocoResourceModule>();
		if (!coalModule)
		{
			Main.Error("coal loco module could not be found");
			return;
		}

		InitializeAudioSource(coalLoader.transform);

		// Instantiate(coalModule.plugFlowingEffects[(int)flowMode].gameObject, chuteOpeningPosition, Quaternion.identity);
		
		loadingUnloadingCoroutine = StartCoroutine(LoadingUnloading(isLoading));
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
			audioObject.transform.position = cube.transform.position;
		}

		audioSource = audioObject.GetComponentInChildren<LayeredAudio>();
	}

	private void CreateTestCube(Transform parent, Vector3 localPosition)
	{
		Main.Debug("create cube");
		cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.name = "testcube";
		cube.transform.SetParent(parent);
		cube.transform.localPosition = localPosition;
		cube.transform.localEulerAngles = Vector3.zero;
	}

	private void CleanUp()
	{
		if (cube)
		{
			Destroy(cube);
		}

		//TODO dit is nooit want hier hebben we al op gecheckt
		if (loadingUnloadingCoroutine != null)
		{
			StopCoroutine(loadingUnloadingCoroutine);
		}
	}

	///*

	public IEnumerator LoadingUnloading(bool isLoading)
	{
		yield return null;
		Main.Debug(nameof(LoadingUnloading));
		
		machineController.SetScreen(WarehouseMachineController.TextPreset.ClearDesc);
		machineController.displayTitleText.text = "Loading, jwz";
		
		machineController.machineSound.Play(machineController.transform.position, parent: machineController.transform);
		
		machineController.LoadOrUnloadOngoing = true;
		
		TrainCar carUnderLoader = null;
		// var ray = new Ray(cube.transform.position, Vector3.down); //todo
		var hits = new RaycastHit[5];

		while (true)
		{
			yield return null;
			
			var thereIsACarHere = false;
			do
			{
				// yield return null;
				yield return WaitFor.Seconds(0.2f); //optimization 
				
				var hitCount = Physics.RaycastNonAlloc(new Ray(cube.transform.position, Vector3.down), hits, 30f);
				if (hitCount == 0) continue;

				for (int i = 0; i < hitCount; i++)
				{
					// Main.Debug("hit "+i+" "+hits[i].transform.gameObject.GetPath());
					
					carUnderLoader = hits[i].transform.GetComponentInParent<TrainCar>();
					if (!carUnderLoader)
					{
						carUnderLoader = hits[i].transform.GetComponentInParents<TrainCarInteriorObject>()?.actualTrainCar;
						if (!carUnderLoader)
						{
							StopLoading();
							continue;
						}
					}
					
					Main.Debug($"car under loader: {carUnderLoader.carType}");
					thereIsACarHere = true;
					break;
				}
			}
			while (!thereIsACarHere);
			
			if (!carUnderLoader.CanLoad(loaderCargoType_V2))
			{
				Main.Debug($"{carUnderLoader.carType} can't contain {loaderCargoType_V2}");
				StopLoading();
				continue;
			}
			
			var logicCar = carUnderLoader.logicCar;

			var cargoInCar = logicCar.CurrentCargoTypeInCar;
			if (cargoInCar != CargoType.None && cargoInCar.ToV2() != loaderCargoType_V2)
			{
				Main.Debug($"{carUnderLoader.carType} can't load {loaderCargoType_V2} because it already contains {cargoInCar}");
				StopLoading();
				continue;
			}
			
			//full
			if (logicCar.LoadedCargoAmount >= logicCar.capacity)
			{
				Main.Debug($"{carUnderLoader.carType} can't load because it is full");
				StopLoading();
				continue;
			}

			StartLoading();

			var kgToLoad = loadSpeed * Time.deltaTime;
			// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
			var unitsToLoad = loaderCargoType_V2.massPerUnit / kgToLoad;
			
			//would overfill?
			if (logicCar.LoadedCargoAmount + unitsToLoad > logicCar.capacity)
			{
				//fill to capacity
				unitsToLoad = logicCar.capacity - logicCar.LoadedCargoAmount;
				StopLoading();
			}

			logicCar.LoadCargo(unitsToLoad, loaderCargoType);
			machineController.SetScreen(WarehouseMachineController.TextPreset.CarUpdated, isLoading, carUnderLoader.ID, logicCar, loaderCargoType_V2);
		}
	}

	private void StartLoading()
	{
		if(isFlowing) return;
		
		Main.Debug(nameof(StartLoading));
		
		// foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		// 	raycastFlowingEffect.Play();
		// 	
		// foreach (ParticleSystem raycastStartFlowEffect in raycastStartFlowEffects)
		// 	raycastStartFlowEffect.Play();
		
		isFlowing = true;
	}
	
	private void StopLoading()
	{
		if(!isFlowing) return;
		
		Main.Debug(nameof(StopLoading));
		
		// foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		// 	raycastFlowingEffect.Stop();
		// 	
		// foreach (ParticleSystem raycastStopFlowEffect in raycastStopFlowEffects)
		// 	raycastStopFlowEffect.Play();
		
		isFlowing = false;
	}
	
	
	//*/

	//todo was hier niet al een functie voor
	// public void StopLoad()
	// {
	//  machineController.LoadOrUnloadOngoing = false;
	//  // StopCoroutine();
	// }
	
}