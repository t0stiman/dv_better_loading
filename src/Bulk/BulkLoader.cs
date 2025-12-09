using System.Collections;
using System.Diagnostics;
using DV;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class BulkLoader: BulkMachine
{
	//approximate vertical distance of the chute to the track
	private const int CHUTE_HEIGHT = 10;
	
	private GameObject shute;
	private TrainCarCache previousCarCache;
	private ShuteEffectsManager effectsMan;
	
	private const int TRAINCAR_LAYER = (int)Layers.DVLayer.Train_Big_Collider;
	private static readonly LayerMask TRAINCAR_MASK = Misc_Extensions.LayerMaskFromInt(TRAINCAR_LAYER);
	
	private readonly Collider[] overlapBoxResults = new Collider[3];
	private Vector3 overlapBoxCenter;
	private Vector3 overlapBoxHalfSize;
	private Quaternion overlapBoxRotation;
	
	//true if cargo is flowing into the car
	private bool cargoIsFlowing;
	private Stopwatch stopwatch = new();
	
	private record struct TrainCarCache(TrainCar trainCar, Collider collider)
	{
		public TrainCar trainCar = trainCar;
		public Collider collider = collider;
	}
	
	#region setup
	
	protected override void Initialize()
	{
		if(initialized) return;

		// finding industry buildings in Start() doesn't work for some reason
		var industryBuilding = GameObject.Find(LoaderInfo.building.name)?.transform;
		if (industryBuilding)
		{
			Main.Debug($"Industry building on bulk machine {gameObject.name}: {industryBuilding.gameObject.GetPath()}");
		}
		else
		{
			Main.Error($"Failed to find industry building {LoaderInfo.building.name} on bulk machine {gameObject.name}");
			return;
		}

		shute = Utilities.CreateGameObject(
			industryBuilding, 
			LoaderInfo.building.shuteLocalPosition,
			Quaternion.identity,
			nameof(shute),
			false
		);
		
		effectsMan = shute.AddComponent<ShuteEffectsManager>();
		effectsMan.CreateShutes(new []{Vector3.zero});
		effectsMan.InitializeEffects();
		
		SetupOverlapBox(industryBuilding);

		initialized = true;
	}
	
	private void SetupOverlapBox(Transform industryBuilding)
	{
		overlapBoxCenter = shute.transform.position - new Vector3(0, CHUTE_HEIGHT / 2f, 0);
		var overlapBoxSize = new Vector3(0.1f, CHUTE_HEIGHT, 0.1f);
		overlapBoxHalfSize = overlapBoxSize/2f;
		overlapBoxRotation = industryBuilding.rotation;
		
		overlapBoxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		overlapBoxObject.name = nameof(overlapBoxObject);
		Destroy(overlapBoxObject.GetComponent<BoxCollider>());

		overlapBoxObject.transform.position = overlapBoxCenter;
		overlapBoxObject.transform.rotation = industryBuilding.rotation;
		overlapBoxObject.transform.localScale = overlapBoxSize; //localscale == actual size
		overlapBoxObject.transform.SetParent(industryBuilding);
		
		overlapBoxObject.SetActive(Main.MySettings.EnableDebugBoxes);
	}
	
	protected override void SetupTexts()
	{
		SetupTexts2("");
	}
	
	#endregion
	
	protected override void SetEnabledText()
	{
		SetDisplayTitleText("Machine enabled");
		SetDisplayDescriptionText("Slowly drive the train under the chute");
	}
	
	protected override void DisableMachine()
	{
		if (!coroutineIsRunning)
			return;
		
		StopTransferring(nameof(DisableMachine));
		StopCoroutine(loadUnloadCoroutine);
		coroutineIsRunning = false;
		clonedMachineController.DisplayIdleText();
		
		if (overlapBoxObject)
		{
			overlapBoxObject.SetActive(false);
		}
	}
	
	#region update
	
	protected void Update()
	{
		if(!initialized) return;
		overlapBoxObject.SetActive(Main.MySettings.EnableDebugBoxes);
	}
	
	protected override IEnumerator LoadingUnloading()
	{
		const float stopLoadingWaitTime = 0.2f;

		coroutineIsRunning = true;
		Main.Debug($"{nameof(BulkLoader)}.{nameof(LoadingUnloading)}");
		SetEnabledText();
		clonedMachineController.machineSound.Play(transform.position, parent: transform);

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
				StopTransferring("there is no car");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}

			if (!TryGetTask(carUnderLoader, out WarehouseTask task))
			{
				StopTransferring("has no active task");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
			
			if (task.warehouseTaskType != WarehouseTaskType.Loading)
			{
				StopTransferring($"wrong task type {task.warehouseTaskType}");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
			
			var taskCargoType = task.cargoType;
			if (!IsCargoTypeSupported(taskCargoType))
			{
				StopTransferring($"Can't load {taskCargoType} because it is unsupported by warehouse machine");
				yield return WaitFor.Seconds(stopLoadingWaitTime);
				continue;
			}
		
			var logicCar = carUnderLoader.logicCar;
			Main.DebugVerbose($"LoadedCargoAmount: {logicCar.LoadedCargoAmount} capacity: {logicCar.capacity}");
		
			//full
			if (logicCar.LoadedCargoAmount >= logicCar.capacity)
			{
				StopTransferring("can't load because it is full");
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
			if (overlapBoxResults[i] == previousCarCache.collider)
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
				previousCarCache.collider = overlapBoxResults[i];
				Main.Debug($"car under loader: {trainCar.carType}");
				return true;
			}
			
			Main.Error($"Could not get {nameof(TrainCar)} in '{gameObject.GetPath()}'");
		}
		
		return false;
	}
	
	private void DoLoadStep(TrainCar carToLoad, CargoType cargoToLoad)
	{
		StartTransferring();
		
		var logicCar = carToLoad.logicCar;
		var cargoToLoadV2 = cargoToLoad.ToV2();

		stopwatch.Stop();
		var kgToLoad = Main.MySettings.BulkLoadSpeedMultiplier * loadUnloadSpeed[cargoToLoad] * (float)stopwatch.Elapsed.TotalSeconds;
		stopwatch.Restart();
		
		// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
		var unitsToLoad = kgToLoad / cargoToLoadV2.massPerUnit;
		
		Main.DebugVerbose($"{nameof(DoLoadStep)}: {kgToLoad} kg, {unitsToLoad} units");
			
		// prevent overfill
		if (logicCar.LoadedCargoAmount + unitsToLoad >= logicCar.capacity)
		{
			Main.DebugVerbose($"{nameof(DoLoadStep)}: {logicCar.LoadedCargoAmount} + {unitsToLoad} >= {logicCar.capacity} ");
			
			//fill to capacity
			unitsToLoad = logicCar.capacity - logicCar.LoadedCargoAmount;
			Main.DebugVerbose($"{nameof(DoLoadStep)}: {unitsToLoad} units");
		}
		
		// the following line prevents an exception in Car.LoadCargo
		logicCar.CurrentCargoTypeInCar = CargoType.None;
		logicCar.LoadCargo(unitsToLoad, cargoToLoad, VanillaMachineController.warehouseMachine);
		
		SetDisplayDescriptionText($"Loading {carToLoad.logicCar.ID} with {cargoToLoadV2.GetLocalizedName()}, {carToLoad.GetFillPercent()}%");
	}
	
	protected void StartTransferring()
	{
		if(cargoIsFlowing) return;
		Main.Debug(nameof(StartTransferring));

		effectsMan.StartTransferring();

		cargoIsFlowing = true;
		stopwatch = Stopwatch.StartNew();
	}
	
	protected void StopTransferring(string reason = "")
	{
		if(!cargoIsFlowing) return;
		Main.Debug($"{nameof(StopTransferring)}, {reason}");
		
		effectsMan.StopTransferring(reason);
		
		cargoIsFlowing = false;
		stopwatch.Reset();

		SetEnabledText();
	}
	
	#endregion
}