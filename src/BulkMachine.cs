using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DV;
using DV.CashRegister;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using TMPro;
using UnityEngine;

namespace better_loading;

//all the animation and sound stuff is based on LocoResourceModule
public class BulkMachine: MonoBehaviour
{
	//kg/s
	private static readonly Dictionary<CargoType, float> loadSpeed = new()
	{
		{ CargoType.Coal, 56000 / 74f },		//based on a YT video
		{ CargoType.IronOre, 62000 / 30f }	//made up
		//todo
		//wheat
		//ballast?
		//sunflower
		//flour
		//corn
	};
	
	public static bool IsSupportedBulkType(CargoType cargoType)
	{
		return loadSpeed.Keys.Contains(cargoType);
	}
	
	// all WarehouseMachines that have a BulkMachine
	public static List<WarehouseMachine> AllWarehouseMachinesWithBulk = new();
	
	private WarehouseMachineController machineController;
	private IndustryBuildingInfo industryBuildingInfo;
	private bool start2Done = false;
	
	private Coroutine loadUnloadCoro;
	private bool coroutineIsRunning = false;
	
	private CargoType[] cargoTypes;
	private CargoType_v2[] cargoTypesV2;
	
	private static LocoResourceModule tenderCoalModule;
	private GameObject shuteOpeningMarker;
	
	private const int TRAINCAR_LAYER = (int)Layers.DVLayer.Train_Big_Collider;
	private static readonly LayerMask TRAINCAR_MASK = Misc_Extensions.LayerMaskFromInt(TRAINCAR_LAYER);
	
	//approximate vertical distance of the chute to the track
	private const int CHUTE_HEIGHT = 10;
	
	//true if cargo is flowing into the car
	private bool cargoIsFlowing;
	private Stopwatch stopwatch = new();
	private bool timeWasFlowing;

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
	
	//box
	private GameObject debugBox;
	private readonly Collider[] overlapBoxResults = new Collider[2];
	private Vector3 overlapBoxCenter;
	private Vector3 overlapBoxHalfSize;
	private Quaternion overlapBoxRotation;

	private struct TrainCarCache
	{
		public TrainCar trainCar;
		public GameObject collisionObject;
	}
	
	private TrainCarCache previousCarCache;

	#region setup

	public void PreStart(
		WarehouseMachineController vanillaMachineController, 
		WarehouseMachineController clonedMachineController, 
		CargoType[] cargoTypes_,
		IndustryBuildingInfo industryBuildingInfo_)
	{
		machineController = vanillaMachineController;
		AllWarehouseMachinesWithBulk.Add(machineController.warehouseMachine);
		
		cargoTypes = cargoTypes_;
		cargoTypesV2 = cargoTypes_.Select(v1 => v1.ToV2()).ToArray();
		
		displayTitleText = clonedMachineController.displayTitleText;
		displayText = clonedMachineController.displayText;

		industryBuildingInfo = industryBuildingInfo_;
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

		// finding industry buildings in Start() doesn't work for some reason
		var industryBuilding = GameObject.Find(industryBuildingInfo.name)?.transform;
		if (industryBuilding)
		{
			Main.Debug($"Industry building on bulk machine {gameObject.name}: {industryBuilding.gameObject.GetPath()}");
		}
		else
		{
			Main.Error($"Failed to find industry building {industryBuildingInfo.name} on bulk machine {gameObject.name}");
			return;
		}

		CreateShuteMarker(industryBuilding);

		InitializeAudioSource(industryBuilding);
		InitializeLoadingEffects(industryBuilding);
		
		SetupOverlapBox(industryBuilding);

		start2Done = true;
	}

	private void SetupOverlapBox(Transform industryBuilding)
	{
		overlapBoxCenter = shuteOpeningMarker.transform.position - new Vector3(0, CHUTE_HEIGHT / 2f, 0);
		var overlapBoxSize = new Vector3(0.1f, CHUTE_HEIGHT, 0.1f);
		overlapBoxHalfSize = overlapBoxSize/2f;
		overlapBoxRotation = industryBuilding.rotation;
		
		debugBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
		debugBox.name = nameof(debugBox);
		Destroy(debugBox.GetComponent<BoxCollider>());

		debugBox.transform.position = overlapBoxCenter;
		debugBox.transform.rotation = industryBuilding.rotation;
		debugBox.transform.localScale = overlapBoxSize; //localscale == actual size
		debugBox.transform.SetParent(industryBuilding);
		
		debugBox.SetActive(Main.MySettings.EnableDebugBoxes);
	}

	private void SetupTexts()
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), "Bulk cargo\ntransfer");
		ChangeText(gameObject.FindChildByName("TextUnload"), "Stop");
		ChangeText(gameObject.FindChildByName("TextLoad"), "Start");

		SetDisabledText();
	}

	private void ChangeText(GameObject textTitleObject, string text)
	{
		var tmp = textTitleObject.GetComponent<TextMeshPro>();
		tmp.SetText(text);
	}

	private void InitializeLoadingEffects(Transform industryBuilding)
	{
		var effectsObject = Instantiate(tenderCoalModule.raycastFlowingEffects[0].transform.parent, shuteOpeningMarker.transform.position, Quaternion.identity);
		effectsObject.SetParent(industryBuilding, true);
		effectsObject.name = nameof(effectsObject);
		raycastFlowingEffects = effectsObject.GetComponentsInChildren<ParticleSystem>();

		if (raycastFlowingEffects.Length == 0)
		{
			Main.Error("no effects");
		}
	}

	private void InitializeAudioSource(Transform industryBuilding)
	{
		Main.Log("Creating audio object");
		
		var OG = tenderCoalModule.audioSourcesPerFlow[(int)flowMode];
		var audioObject = Instantiate(OG.gameObject, industryBuilding);
		audioObject.name = "LoadingSound";
		audioObject.transform.position = shuteOpeningMarker.transform.position;

		audioSource = audioObject.GetComponentInChildren<LayeredAudio>();
	}

	private void CreateShuteMarker(Transform parent)
	{
		shuteOpeningMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
		shuteOpeningMarker.name = nameof(shuteOpeningMarker);
		//invisible 
		shuteOpeningMarker.GetComponent<MeshRenderer>().enabled = Main.MySettings.EnableDebugBoxes;
		
		shuteOpeningMarker.transform.SetParent(parent);
		shuteOpeningMarker.transform.localPosition = industryBuildingInfo.shutePosition;
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
		if (coroutineIsRunning)
			return;
		
		Start2();
		loadUnloadCoro = StartCoroutine(Loading());
		
		if (debugBox)
		{
			debugBox.SetActive(true);
		}
	}
	
	private void StopLoadingSequence()
	{
		if (!coroutineIsRunning)
			return;
		
		StopLoading(nameof(StopLoadingSequence));
		StopCoroutine(loadUnloadCoro);
		coroutineIsRunning = false;
		SetDisabledText();
		
		if (debugBox)
		{
			debugBox.SetActive(false);
		}
	}
	
	private void SetDisabledText()
	{
		displayTitleText.SetText($"This machine loads {cargoTypesV2.Select(cargoType => cargoType.LocalizedName()).Join(", ")}");
		displayText.SetText("Move the handle to start");
	}

	private void SetEnabledText()
	{
		displayTitleText.text = $"Machine enabled"; //todo
		displayText.text = "Slowly drive the train under the chute";
	}
	
	#region update

	private void Update()
	{
		//game paused?
		if (!TimeUtil.IsFlowing) return;
		
		DoSound();
		debugBox?.SetActive(Main.MySettings.EnableDebugBoxes);
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
		const float stopLoadingWaitTime = 0.2f;

		coroutineIsRunning = true;
		Main.Debug(nameof(Loading));

		SetEnabledText();
		
		machineController.machineSound.Play(transform.position, parent: transform);

		while (true)
		{
			yield return null;
			
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
				
				continue;
			}
			
			if (!IsCarUnderLoader(out TrainCar carUnderLoader))
			{
				StopLoading("there is no car");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}

			if (!TryGetTask(carUnderLoader, out WarehouseTask task))
			{
				StopLoading("has no active task");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
			
			var taskCargoType = task.cargoType;

			if (!cargoTypes.Contains(taskCargoType))
			{
				StopLoading($"Can't load {taskCargoType} because it is unsupported by warehouse machine");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
		
			var logicCar = carUnderLoader.logicCar;
			Main.Debug($"LoadedCargoAmount: {logicCar.LoadedCargoAmount} capacity: {logicCar.capacity}");
		
			//full
			if (logicCar.LoadedCargoAmount >= logicCar.capacity)
			{
				StopLoading("can't load because it is full");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
			
			DoLoadStep(carUnderLoader, taskCargoType);
		}
	}
	
	private bool IsCarUnderLoader(out TrainCar carUnderLoader)
	{
		carUnderLoader = null;
		
		var resultsCount = Physics.OverlapBoxNonAlloc(overlapBoxCenter, overlapBoxHalfSize, overlapBoxResults, overlapBoxRotation, TRAINCAR_MASK);

		for (int i = 0; i < resultsCount; i++)
		{
			// use cache to reduce calls to expensive GetComponentInParent
			if (overlapBoxResults[i].gameObject == previousCarCache.collisionObject)
			{
				carUnderLoader = previousCarCache.trainCar;
				return true;
			}
			
			// on DV train cars GetComponent is sufficient, but on CCL cars the collider can be on a deeper level
			var trainCar = overlapBoxResults[i].transform.parent.GetComponentInParent<TrainCar>(); 
			if (trainCar)
			{
				carUnderLoader = trainCar;
				previousCarCache.trainCar = trainCar;
				previousCarCache.collisionObject = overlapBoxResults[i].gameObject;
				Main.Debug($"car under loader: {trainCar.carType}");
				return true;
			}
			
			Main.Error($"Could not get {nameof(TrainCar)} in '{gameObject.GetPath()}'");
		}
		
		return false;
	}

	private bool TryGetTask(TrainCar aCar, out WarehouseTask task)
	{
		if (aCar.logicCar == null)
		{
			task = null;
			return false;
		}
		
		foreach (var aTask in machineController.warehouseMachine.currentTasks)
		{
			if(!aTask.cars.Contains(aCar.logicCar)) continue;
			
			task = aTask;
			return true;
		}

		task = null;
		return false;
	}

	private void DoLoadStep(TrainCar carToLoad, CargoType cargoToLoad)
	{
		StartLoading();
		
		var logicCar = carToLoad.logicCar;
		var cargoToLoadV2 = cargoToLoad.ToV2();

		stopwatch.Stop();
		var kgToLoad = Main.MySettings.LoadSpeedMultipler * loadSpeed[cargoToLoad] * (float)stopwatch.Elapsed.TotalSeconds;
		stopwatch.Restart();
		
		// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
		var unitsToLoad = kgToLoad / cargoToLoadV2.massPerUnit;
		
		Main.Debug($"{nameof(DoLoadStep)}: {kgToLoad} kg, {unitsToLoad} units");
			
		// prevent overfill
		if (logicCar.LoadedCargoAmount + unitsToLoad >= logicCar.capacity)
		{
			Main.Debug($"{nameof(DoLoadStep)}: {logicCar.LoadedCargoAmount} + {unitsToLoad} >= {logicCar.capacity} ");
			
			//fill to capacity
			unitsToLoad = logicCar.capacity - logicCar.LoadedCargoAmount;
			Main.Debug($"{nameof(DoLoadStep)}: {unitsToLoad} units");
		}
		
		// the following line prevents an exception in Car.LoadCargo
		logicCar.CurrentCargoTypeInCar = CargoType.None;
		logicCar.LoadCargo(unitsToLoad, cargoToLoad, machineController.warehouseMachine);
		
		displayText.text = $"Loading {carToLoad.logicCar.ID} with {cargoToLoadV2.LocalizedName()}, {carToLoad.GetFillPercent()}%";
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

		SetEnabledText();
	}

	#endregion
}