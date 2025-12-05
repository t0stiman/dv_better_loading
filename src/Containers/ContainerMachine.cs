using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class ContainerMachine: AdvancedMachine
{
	private bool initialized = false;

	private Crane crane;
	private CraneInfo craneInfo;
	public ContainerArea MyContainerArea;
	private Coroutine moveToHorizontalCoroutine;
	private List<WarehouseTask> spawnQueue = new();
	
	public static bool IsInShippingContainer(CargoType cargoType)
	{
		//todo AmmoniumNitrate? cilinder containers?
		
		switch (cargoType)
		{
			case CargoType.ScrapContainers:
			case CargoType.ElectronicsIskar:
			case CargoType.ElectronicsKrugmann:
			case CargoType.ElectronicsAAG:
			case CargoType.ElectronicsNovae:
			case CargoType.ElectronicsTraeg:
			case CargoType.ToolsIskar:
			case CargoType.ToolsBrohm:
			case CargoType.ToolsAAG:
			case CargoType.ToolsNovae:
			case CargoType.ToolsTraeg:
			case CargoType.ClothingObco:
			case CargoType.ClothingNeoGamma:
			case CargoType.ClothingNovae:
			case CargoType.ClothingTraeg:
			case CargoType.EmptySunOmni:
			case CargoType.EmptyIskar:
			case CargoType.EmptyObco:
			case CargoType.EmptyGoorsk:
			case CargoType.EmptyKrugmann:
			case CargoType.EmptyBrohm:
			case CargoType.EmptyAAG:
			case CargoType.EmptySperex:
			case CargoType.EmptyNovae:
			case CargoType.EmptyTraeg:
			case CargoType.EmptyChemlek:
			case CargoType.EmptyNeoGamma:
				return true;
		}

		//todo well cars, other CCL
		
		return false;
	}
	
	public override bool IsSupportedCargoType(CargoType cargoType)
	{
		return IsInShippingContainer(cargoType);
	}

	private void OnDisable()
	{
		StopTransferSequence();
	}

	public void PreStart(WarehouseMachineController vanillaMachineController_,
		WarehouseMachineController clonedMachineController_,
		CargoType[] cargoTypes_,
		CraneInfo craneInfo_)
	{
		base.PreStart(vanillaMachineController_, clonedMachineController_, cargoTypes_);
		craneInfo = craneInfo_;
	}

	private void Start()
	{
		SetupTexts("Container\ntransfer");
		clonedMachineController.DisplayIdleText(); //todo verkeerde cargos in text

		StartCoroutine(Initialize());
	}

	private IEnumerator Initialize()
	{
		//when Start() is called the crane object does not yet exist
		ObjectWaiter.FindPlease(craneInfo.Path);

		GameObject craneFoundationObject;
		while (!ObjectWaiter.IsFound(craneInfo.Path, out craneFoundationObject))
		{
			yield return new WaitForSeconds(5);
		}
		
		Main.Debug($"Found {craneInfo.Path}");
		ObjectWaiter.Deregister(craneInfo.Path);
		
		crane = craneFoundationObject.AddComponent<Crane>();
		crane.Initialize(craneInfo);
		
		var craneFoundationTransform = craneFoundationObject.transform;
		
		var areaCenter = craneFoundationTransform.position +
			(crane.info.PlaceContainersAtLongSideOfCrane ?
			-craneFoundationTransform.forward :
			craneFoundationTransform.forward)
			* 18.5f;
		
		var containerAreaObject = Utilities.CreateGameObject(craneFoundationTransform, areaCenter, craneFoundationTransform.rotation, nameof(ContainerArea));
		MyContainerArea = containerAreaObject.AddComponent<ContainerArea>();

		HandleSpawnQueue();
		
		initialized = true;
	}

	private void HandleSpawnQueue()
	{
		Main.Debug($"{nameof(HandleSpawnQueue)} {spawnQueue.Count}");
		foreach (var task in spawnQueue)
		{
			MyContainerArea.SpawnContainers(task);
		}
		spawnQueue = new();
	}

	protected override void OnLeverPositionChange(int positionState)
	{
		switch (positionState)
		{
			case -1:
				StartTransferSequence(true);
				break;
			case 1:
				StartTransferSequence(false);
				break;
		}
	}

	private void StartTransferSequence(bool isLoading)
	{
		if (loadUnloadCoroutine != null)
			return;
		
		clonedMachineController.ClearTrainInRangeText();
		loadUnloadCoroutine = StartCoroutine(LoadingUnloading(isLoading));
	}

	private void StopTransferSequence()
	{
		if (loadUnloadCoroutine != null)
		{
			StopCoroutine(loadUnloadCoroutine);
		}
		
		clonedMachineController.DisplayIdleText();
	}
	
	protected IEnumerator LoadingUnloading(bool isLoading)
	{
		yield return null;
		Main.Debug($"{nameof(ContainerMachine)}.{nameof(LoadingUnloading)}");
		
		SetScreen(WarehouseMachineController.TextPreset.ClearDesc);
		VanillaMachineController.machineSound.Play(transform.position, parent: transform);
		var anythingProcessed = false;

		var currentTasks = VanillaMachineController.warehouseMachine.currentTasks;
		if (currentTasks.Count == 0)
		{
			Main.Debug("No tasks");
			SetScreen(WarehouseMachineController.TextPreset.NoTrains, isLoading);
		}

		var readyTasks = GetReadyTasks().ToArray();
		MovingCarsCheck(ref readyTasks);
		Main.Debug($"{nameof(readyTasks)}: {readyTasks.Length}");
		
		if (readyTasks.Length > 0 && moveToHorizontalCoroutine != null)
		{
			StopCoroutine(moveToHorizontalCoroutine);
		}

		// ================ Loading ================
		
		var loadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Loading).ToArray();
		
		foreach (var task in loadTasks)
		{
			var cargoName = task.cargoType.ToV2().LocalizedName();

			var carsToLoad = task.cars.Where(tc => tc.CurrentCargoTypeInCar == CargoType.None);
			foreach (var taskCar in carsToLoad)
			{
				var trainCar = taskCar.TrainCar();
				var pair = MyContainerArea.GetSlotContainerPair(taskCar);
				var containerInfo = pair.Value;
				var containerTransform = containerInfo.containerObject.transform;
				anythingProcessed = true;
			
				SetScreen(WarehouseMachineController.TextPreset.Busy, isLoading);
				SetDisplayDescriptionText($"Loading {cargoName} onto {taskCar.ID}");
				
				yield return crane.MoveTo(containerTransform.position + containerInfo.roofOffset);
				crane.Grab(containerTransform);
				yield return crane.MoveTo(trainCar.transform.position + containerInfo.roofOffset);
				
				MyContainerArea.Destroy(pair);
				
				var amountToLoad = task.cargoAmount >= taskCar.capacity ? taskCar.capacity : task.cargoAmount;
				//by setting currentCargoModelIndex we ensure the container on the train car looks the same as the one we just moved
				trainCar.CargoModelController.currentCargoModelIndex = containerInfo.cargoModelIndex;
				taskCar.LoadCargo(amountToLoad, task.cargoType, VanillaMachineController.warehouseMachine);
			}
		}
		
		// ================ Unloading ================
		
		var unloadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Unloading).ToArray();
		foreach (var task in unloadTasks)
		{
			//todo unloading
		}

		//to idle position
		moveToHorizontalCoroutine = StartCoroutine(crane.MoveToHorizontalMoveAltitude());
		
		if (anythingProcessed)
		{
			Main.Debug("something done");
			SetScreen(WarehouseMachineController.TextPreset.Completed, isLoading);
		}
		else
		{
			Main.Debug("nothing done");
			SetScreen(WarehouseMachineController.TextPreset.Failed, isLoading);
			//todo play error sound
		}
		
		yield return clonedMachineController.StartCoroutine(clonedMachineController.ResetTextToIdleDisplay(anythingProcessed ? 
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_LONG :
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_SHORT));
		loadUnloadCoroutine = null;
	}

	private void MovingCarsCheck(ref WarehouseTask[] readyTasks)
	{
		foreach (var rt in readyTasks)
		{
			if (!clonedMachineController.AnyCarMoving(rt.cars)) continue;
			
			SetScreen(WarehouseMachineController.TextPreset.Moving, rt.warehouseTaskType == WarehouseTaskType.Loading, rt.Job.ID);
			break;
		}

		readyTasks = readyTasks.Where(t => !clonedMachineController.AnyCarMoving(t.cars)).ToArray();
	}

	private void SetupTexts(string titleText)
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), titleText);
		FilterCargoOnScreen(clonedMachineController, cargoTypes, false);
	}

	public void SpawnContainers(WarehouseTask loadTask)
	{
		if (initialized)
		{
			MyContainerArea.SpawnContainers(loadTask);
			return;
		}
		
		//spawn them later
		spawnQueue.Add(loadTask);
		Main.Debug($"spawnQueue added -> {spawnQueue.Count}");
	}
}