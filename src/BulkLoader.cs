using System.Collections;
using System.Diagnostics;
using System.Linq;
using DV;
using DV.CashRegister;
using DV.Localization;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using TMPro;
using UnityEngine;

namespace better_loading;

//all the animation and sound stuff is based on LocoResourceModule
public class BulkLoader: MonoBehaviour
{
	private WarehouseMachineController machineController;
	private bool start2Done = false;
	
	private CargoType cargoType;
	private CargoType_v2 cargoTypeV2;
	private string cargoNameLocalized => LocalizationAPI.L(cargoTypeV2.localizationKeyFull);
	
	private static LocoResourceModule tenderCoalModule;
	private GameObject shuteOpeningMarker;
	
	private const int MY_LAYER = 18; //18 is not used by DV. See DV.Layers.Layers.DVLayer .
	private static readonly LayerMask MY_LAYER_MASK = Extensions.LayerMaskFromInt(MY_LAYER);
	
	//approximate vertical distance of the chute to the track
	private const int CHUTE_HEIGHT = 10;
	
	//true if cargo is flowing into the car
	private bool cargoIsFlowing;
	private Stopwatch stopwatch = new();
	private bool timeWasFlowing;
	private bool stopLoadRequested = false;
	private bool currentCoroutineIsOurs = false;

	//sound
	private LayeredAudio audioSource;
	private const ResourceFlowMode flowMode = ResourceFlowMode.Air;
	private float flowVolume;
	private float curVolumeVelocity;
	
	//particle effects
	private ParticleSystem[] raycastFlowingEffects = {};
	
	//text
	private TextMeshPro displayTitleText;
	private TextMeshPro displayText;

	#region setup

	public void PreStart(WarehouseMachineController vanillaMachineController, WarehouseMachineController clonedMachineController, CargoType aCargoType)
	{
		machineController = vanillaMachineController;
		cargoType = aCargoType;
		cargoTypeV2 = aCargoType.ToV2();
		
		displayTitleText = clonedMachineController.displayTitleText;
		displayText = clonedMachineController.displayText;
	}
	
	private void Start()
	{
		if (tenderCoalModule == null)
		{
			tenderCoalModule = CashRegisterBase.allCashRegisters
				.OfType<CashRegisterWithModules>()
				.SelectMany(register => register.registerModules)
				.OfType<LocoResourceModule>()
				.First(resourceModule => resourceModule.resourceType == ResourceType.Coal);
		}
		
		SetupTexts();
		StartCoroutine(InitLeverHJAF());
	}

	private void Start2()
	{
		if(start2Done) return;
		
		// finding Industry_Coal in Start() doesn't work for some reason
		var industryCoal = GameObject.Find("Industry_Coal");
		if (!industryCoal)
		{
			Main.Error("Industry_Coal could not be found");
			return;
		}

		CreateShuteMarker(industryCoal.transform);

		InitializeAudioSource(industryCoal.transform);
		InitializeLoadingEffects(industryCoal.transform);

		start2Done = true;
	}

	private void SetupTexts()
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), "coal\nloader");
		ChangeText(gameObject.FindChildByName("TextUnload"), "Stop");
		ChangeText(gameObject.FindChildByName("TextLoad"), "Start");

		SetIdleText();
	}

	private void ChangeText(GameObject textTitleObject, string text)
	{
		var tmp = textTitleObject.GetComponent<TextMeshPro>();
		tmp.SetText(text);
	}

	private void InitializeLoadingEffects(Transform coalLoader)
	{
		var effectsObject = Instantiate(tenderCoalModule.raycastFlowingEffects[0].transform.parent, shuteOpeningMarker.transform.position, Quaternion.identity);
		effectsObject.SetParent(coalLoader, true);
		effectsObject.name = "effectsObject";
		raycastFlowingEffects = effectsObject.GetComponentsInChildren<ParticleSystem>();

		if (!raycastFlowingEffects.Any())
		{
			Main.Error("no effects");
		}
	}

	private void InitializeAudioSource(Transform coalLoader)
	{
		Main.Log("Creating audio object");
		
		var OG = tenderCoalModule.audioSourcesPerFlow[(int)flowMode];
		var audioObject = Instantiate(OG.gameObject, coalLoader);
		audioObject.name = "LoadingSound";
		audioObject.transform.position = shuteOpeningMarker.transform.position;

		audioSource = audioObject.GetComponentInChildren<LayeredAudio>();
	}

	private void CreateShuteMarker(Transform parent)
	{
		shuteOpeningMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
		shuteOpeningMarker.name = "shuteOpeningMarker";
		//invisible 
		shuteOpeningMarker.GetComponent<MeshRenderer>().enabled = Main.MySettings.EnableDebugLog;
		
		shuteOpeningMarker.transform.SetParent(parent);
		shuteOpeningMarker.transform.localPosition = new Vector3(-85.976f, 8.554f, 1.999f);
		shuteOpeningMarker.transform.localEulerAngles = Vector3.zero;
	}
	
	// WarehouseMachineController.InitLeverHJAF
	private IEnumerator InitLeverHJAF()
	{
		HingeJointAngleFix jointFix;
		while ((jointFix = gameObject.GetComponentInChildren<HingeJointAngleFix>()) == null)
			yield return WaitFor.Seconds(0.2f);
		var amplitudeChecker = jointFix.gameObject.AddComponent<RotaryAmplitudeChecker>();
		var hingeJoint = jointFix.gameObject.GetComponent<HingeJoint>();
		var amplitude = hingeJoint.limits.max - hingeJoint.limits.min;
		amplitudeChecker.checkThreshold = amplitude * 0.2f;
		amplitudeChecker.checkPeriod = 0.1f;
		amplitudeChecker.RotaryStateChanged += OnLeverPositionChange;
	}
	
	#endregion
	

	private void OnLeverPositionChange(int positionState)
	{
		switch (positionState)
		{
			case -1:
				StartLoadingSequence();
				break;
			case 1:
				StopLoadingSequence();
				break;
		}
	}

	private void StartLoadingSequence()
	{
		if (machineController.loadUnloadCoro != null || machineController.activateExternallyCoro != null)
			return;
		
		Start2();
		machineController.loadUnloadCoro = StartCoroutine(Loading());
		currentCoroutineIsOurs = true;
	}
	
	private void StopLoadingSequence()
	{
		if (!currentCoroutineIsOurs || machineController.loadUnloadCoro == null)
			return;

		stopLoadRequested = true;
	}
	
	private void SetIdleText()
	{
		displayTitleText.SetText($"This machine loads {cargoNameLocalized}");
		displayText.SetText("Move the handle to start");
	}
	
	#region update

	private void Update()
	{
		//game paused?
		if (!TimeUtil.IsFlowing) return;
		
		DoSound();
	}

	private void DoSound()
	{
		if(!audioSource) return;

		if (cargoIsFlowing)
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

	public IEnumerator Loading()
	{
		Main.Debug(nameof(Loading));
		
		machineController.SetScreen(WarehouseMachineController.TextPreset.ClearDesc);
		displayTitleText.text = $"Loading {cargoNameLocalized}";
		displayText.text = "Slowly drive the train under the chute";
		
		machineController.machineSound.Play(transform.position, parent: transform);
		
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

			if (stopLoadRequested)
			{
				StopLoading("stop load requested");
				stopLoadRequested = false;
				break;
			}

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
		
		currentCoroutineIsOurs = false;
	}
	
	//todo
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
		if (!aCar.CanLoad(cargoTypeV2))
		{
			Main.Debug($"{aCar.carType} can't contain {cargoTypeV2}");
			return false;
		}
			
		var logicCar = aCar.logicCar;

		var cargoInCar = logicCar.CurrentCargoTypeInCar;
		if (cargoInCar != CargoType.None && cargoInCar.ToV2() != cargoTypeV2)
		{
			Main.Debug($"{aCar.carType} can't load {cargoTypeV2} because it already contains {cargoInCar}");
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
		var kgToLoad = Main.MySettings.LoadSpeed * (float)stopwatch.Elapsed.TotalSeconds;
		stopwatch.Restart();
		
		// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
		var unitsToLoad = kgToLoad / cargoTypeV2.massPerUnit;
		
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
		logicCar.LoadCargo(unitsToLoad, cargoType, machineController.warehouseMachine);
		
		machineController.SetScreen(WarehouseMachineController.TextPreset.CarUpdated, true, carToLoad.ID, logicCar, cargoTypeV2);
	}

	private void StartLoading()
	{
		if(cargoIsFlowing) return;
		
		Main.Debug(nameof(StartLoading));

		foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		{
			raycastFlowingEffect.Play();
		}

		cargoIsFlowing = true;
		stopwatch = Stopwatch.StartNew();
	}
	
	private void StopLoading(string reason = "")
	{
		if(!cargoIsFlowing) return;
		
		Main.Debug($"{nameof(StopLoading)}, {reason}");
		
		foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		{
			raycastFlowingEffect.Stop();
		}
		
		cargoIsFlowing = false;
		stopwatch.Reset();

		SetIdleText();
	}

	#endregion
}