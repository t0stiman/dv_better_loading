using System.Collections;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class ContainerMachine: AdvancedMachine
{
	private const float craneSpeed = 2f; //todo speed in CraneInfo

	private bool initialized = false;
	
	private GameObject crane_base;
	private GameObject crane_cab;
	// private GameObject crane_grabber;
	private CraneInfo craneInfo;
	
	private ContainerArea containerArea;
	
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
		clonedMachineController.DisplayIdleText();
	}

	private void Initialize()
	{
		if(initialized) return;
		
		var crane = GameObject.Find("Portal_Crane");
		crane.GetComponent<Animator>().enabled = false;
		crane_base = crane.FindChildByName("Portal_Crane_Base");
		crane_cab = crane_base.FindChildByName("Portal_Crane_Cab");

		var postfix = craneInfo.FirstRail ? "" : " (1)";
		var craneRail = crane.transform.parent.Find($"CraneRail{postfix}/HarborFloorRail");

		//todo center is not on ground
		var areaCenter = craneRail.position +
			(craneInfo.PlaceContainersRightOfRail ? craneRail.right : -craneRail.right) * 5f;
		containerArea = new ContainerArea(areaCenter, craneRail.rotation, craneRail.forward);
		
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.SetParent(craneRail);
		cube.transform.localPosition = Vector3.zero;
		cube.transform.localEulerAngles = Vector3.zero;

		initialized = true;
	}
	
	protected override void OnLeverPositionChange(int positionState)
	{
		switch (positionState)
		{
			case -1:
				StartTransferSequence(false);
				break;
			case 1:
				StartTransferSequence(true);
				break;
		}
	}

	private void StartTransferSequence(bool isLoading)
	{
		if (loadUnloadCoro != null)
			return;
		
		Initialize();
		clonedMachineController.ClearTrainInRangeText();
		loadUnloadCoro = StartCoroutine(LoadingUnloading(isLoading));
	}

	// protected void StopTransferSequence()
	// {
	// 	if (loadUnloadCoro != null)
	// 	{
	// 		StopCoroutine(loadUnloadCoro);
	// 	}
	// 	
	// 	clonedMachineController.DisplayIdleText();
	// }
	
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

		var loadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Loading).ToArray();

		foreach (var task in loadTasks)
		{
			containerArea.SpawnContainers(task.cars, task.cargoType);
		}
		
		foreach (var task in loadTasks)
		{
			var cargoName = task.cargoType.ToV2().LocalizedName();

			var carsToLoad = task.cars.Where(tc => tc.CurrentCargoTypeInCar == CargoType.None);
			foreach (var taskCar in carsToLoad)
			{
				var trainCar = taskCar.TrainCar();
				var containerInfo = containerArea.GetContainerObject(taskCar);
				var containerObject = containerInfo.gameObject;
				anythingProcessed = true;
			
				SetScreen(WarehouseMachineController.TextPreset.Busy, isLoading);
				SetDisplayDescriptionText($"Loading {cargoName} onto {taskCar.ID}");
				
				var carTransform = trainCar.transform;
				var containerTransform = containerObject.transform;
				
				//move it:
				//up
				var target = new Vector3(containerTransform.position.x, containerArea.center.y + 4f, containerTransform.position.z);
				do
				{
					yield return null;
				}
				while (!containerTransform.MoveTowards(target, craneSpeed * Time.deltaTime));
				
				//to above the car
				do
				{
					yield return null;
					target = new Vector3(carTransform.position.x, containerTransform.position.y, carTransform.position.z);
				}
				while (!containerTransform.MoveTowards(target, craneSpeed * Time.deltaTime));
				
				//down
				do
				{
					yield return null;
					target = carTransform.position;
				}
				while (!containerTransform.MoveTowards(target, craneSpeed * Time.deltaTime));
				
				containerArea.Destroy(containerInfo);
				
				var amountToLoad = task.cargoAmount >= taskCar.capacity ? taskCar.capacity : task.cargoAmount;
				//by setting currentCargoModelIndex we ensure the container on the train car looks the same as the one we just moved
				trainCar.CargoModelController.currentCargoModelIndex = containerInfo.cargoModelIndex;
				taskCar.LoadCargo(amountToLoad, task.cargoType, VanillaMachineController.warehouseMachine);
				//todo fakecargo ook amount?
			}
		}
		
		var unloadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Unloading).ToArray();
		foreach (var task in unloadTasks)
		{
			//todo unloading
		}
		
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
		loadUnloadCoro = null;
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
	}
}