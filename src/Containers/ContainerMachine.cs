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
		SetupTexts("Container\ncrane");
		clonedMachineController.DisplayIdleText();

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
		crane.Initialize();
		
		var craneFoundationTransform = craneFoundationObject.transform;
		
		var areaCenter = craneFoundationTransform.position +
			craneFoundationTransform.forward * craneInfo.ContainerAreaOffset;
		
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
			if (task.cars.Count == 0)
			{
				Main.Error("task has no cars!");
				continue;
			}
			
			MyContainerArea.SpawnContainersForLoading(task);
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

		if (isLoading)
		{
			foreach (var somethingToLoad in CreateLoadingQueue(readyTasks))
			{
				anythingProcessed = true;

				var trainCar = somethingToLoad.car.TrainCar();
				var containerInfo = somethingToLoad.slotContainer.Value;
				var containerTransform = containerInfo.containerObject.transform;

				SetBusyScreen(isLoading, somethingToLoad.task.cargoType, somethingToLoad.car);

				// to container
				yield return crane.MoveTo(containerTransform.position + containerInfo.roofOffset);
				crane.Grab(containerTransform);
				// to train car
				yield return crane.MoveTo(trainCar.transform.position + containerInfo.roofOffset);

				MyContainerArea.Destroy(somethingToLoad.slotContainer);
				AddContainerToTrainCar(somethingToLoad.task, trainCar, somethingToLoad.slotContainer.Value.cargoModelIndex);
			}
		}

		// ================ Unloading ================
		
		else
		{
			foreach (var somethingToUnload in CreateUnloadingQueue(readyTasks))
			{
				anythingProcessed = true;

				var trainCar = somethingToUnload.car.TrainCar();
				var containerInfo = somethingToUnload.slotContainer.Value;

				SetBusyScreen(isLoading, somethingToUnload.task.cargoType, somethingToUnload.car);

				// move crane to container on traincar
				yield return crane.MoveTo(trainCar.CargoModelController.currentCargoModel.transform.position + containerInfo.roofOffset);

				containerInfo.containerObject.transform.position =
					trainCar.CargoModelController.currentCargoModel.transform.position;
				RemoveContainerFromTrainCar(somethingToUnload.task, somethingToUnload.car);
				
				containerInfo.containerObject.SetActive(true);
				crane.Grab(containerInfo.containerObject.transform);

				// move crane to slot in ContainerArea
				var slotPosition = MyContainerArea.GetSlotPosition(somethingToUnload.slotContainer.Key);
				yield return crane.MoveTo(slotPosition + containerInfo.roofOffset - Vector3.up * ShippingContainer.CARGO_ORIGIN_OFFSET);

				MyContainerArea.PlaceInArea(containerInfo.containerObject.transform);
			}
		}

		// move crane to idle position
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

	// Creates an ordered list of all cars that will be loaded. Top level containers need to be loaded first
	private List<ContainerTransferQueueEntry> CreateLoadingQueue(WarehouseTask[] readyTasks)
	{
		var loadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Loading).ToArray();

		List<ContainerTransferQueueEntry> loadingQueue = new();
		
		foreach (var task in loadTasks)
		{
			foreach (var taskCar in task.cars.Where(car => car.CurrentCargoTypeInCar == CargoType.None))
			{
				var pair = MyContainerArea.GetSlotContainerPair(taskCar);
				loadingQueue.Add(new ContainerTransferQueueEntry(taskCar, pair, task));
			}
		}
		
		// From top to bottom
		loadingQueue.Sort((x, y) => y.slotContainer.Key.layer.CompareTo(x.slotContainer.Key.layer));
		
		return loadingQueue;
	}
	
	private List<ContainerTransferQueueEntry> CreateUnloadingQueue(WarehouseTask[] readyTasks)
	{
		var unloadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Unloading).ToArray();

		List<ContainerTransferQueueEntry> queue = new();
		
		foreach (var task in unloadTasks)
		{
			foreach (var taskCar in task.cars.Where(car => car.CurrentCargoTypeInCar != CargoType.None))
			{
				var pair = MyContainerArea.SpawnContainerForUnloading(task, taskCar);
				queue.Add(new ContainerTransferQueueEntry(taskCar, pair, task));
			}
		}
		
		// from bottom to top
		queue.Sort((x, y) => x.slotContainer.Key.layer.CompareTo(y.slotContainer.Key.layer));
		
		return queue;
	}
	
	private void AddContainerToTrainCar(WarehouseTask task, TrainCar trainCar, byte cargoModelIndex)
	{
		var logicCar = trainCar.logicCar;
		
		var amountToLoad = task.cargoAmount >= logicCar.capacity ? logicCar.capacity : task.cargoAmount;
		//by setting currentCargoModelIndex we ensure the container on the train car looks the same as the one we just moved
		trainCar.CargoModelController.currentCargoModelIndex = cargoModelIndex;
		logicCar.LoadCargo(amountToLoad, task.cargoType, VanillaMachineController.warehouseMachine);
	}

	private void RemoveContainerFromTrainCar(WarehouseTask task, Car logicCar)
	{
		logicCar.UnloadCargo(logicCar.LoadedCargoAmount, task.cargoType, VanillaMachineController.warehouseMachine);
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
	
	private void SetBusyScreen(bool isLoading, CargoType cargoType, Car car)
	{
		SetScreen(WarehouseMachineController.TextPreset.Busy, isLoading);
		SetDisplayDescriptionText($"Loading {cargoType.ToV2().GetLocalizedName()} onto {car.ID}");
	}

	public void SpawnContainers(WarehouseTask loadTask)
	{
		if (initialized)
		{
			MyContainerArea.SpawnContainersForLoading(loadTask);
			return;
		}
		
		//spawn them later
		spawnQueue.Add(loadTask);
		Main.Debug($"spawnQueue added -> {spawnQueue.Count}");
	}
}