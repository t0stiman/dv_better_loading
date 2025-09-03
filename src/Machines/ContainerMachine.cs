using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using Random = UnityEngine.Random;

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

	private void OnEnable()
	{
		// StartCoroutine(TrainInRangeCheck()); //todo
	}
	
	private void OnDisable()
	{
		StopAllCoroutines();
		loadUnloadCoro = null;
	}

	public void PreStart(WarehouseMachineController vanillaMachineController,
		WarehouseMachineController clonedMachineController,
		CargoType[] cargoTypes_,
		CraneInfo craneInfo_)
	{
		base.PreStart(vanillaMachineController, clonedMachineController, cargoTypes_);
		craneInfo = craneInfo_;
	}

	private void Start()
	{
		SetupTexts("Container\ntransfer");
		DisplayIdleText();
		base.Start_();
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

	protected override void StartLoadingSequence()
	{
		if (loadUnloadCoro != null)
			return;
		
		Initialize();
		loadUnloadCoro = StartCoroutine(LoadingUnloading());
	}

	protected override void StopLoadingSequence()
	{
		if (loadUnloadCoro != null)
		{
			StopCoroutine(loadUnloadCoro);
		}
		
		DisplayIdleText();
	}
	
	public IEnumerator LoadingUnloading()
	{
		yield return null;
		Main.Debug($"{nameof(ContainerMachine)}.{nameof(LoadingUnloading)}");
		
		MachineController.machineSound.Play(transform.position, parent: transform);
		var anythingDone = false;

		var currentTasks = MachineController.warehouseMachine.currentTasks;
		if (currentTasks.Count == 0)
		{
			Main.Debug("No tasks");
		}

		var readyTasks = currentTasks.Where(task =>
			MachineController.warehouseMachine.CarsPresentOnWarehouseTrack(task.cars) &&
			task.readyForMachine &&
			!MachineController.AnyCarMoving(task.cars)
		).ToArray();
		
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
				anythingDone = true;
				
				displayText.SetText($"Loading {cargoName} onto {taskCar.ID}");
				
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
				taskCar.LoadCargo(amountToLoad, task.cargoType, MachineController.warehouseMachine);
				//todo fakecargo ook amount?
			}
		}
		
		var unloadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Unloading).ToArray();
		foreach (var task in unloadTasks)
		{
			//todo
		}
		
		if (anythingDone)
		{
			Main.Debug("something done");
			displayText.SetText("Completed");
		}
		else
		{
			Main.Debug("nothing done");
			displayText.SetText("Failed");
			//todo play error sound
		}
		
		yield return StartCoroutine(ResetTextToIdleDisplay(anythingDone ? 
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_LONG :
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_SHORT));
		loadUnloadCoro = null;
	}
	
	private IEnumerator ResetTextToIdleDisplay(float resetTextAfter)
	{
		yield return WaitFor.Seconds(resetTextAfter);
		DisplayIdleText();
	}
	
	// private IEnumerator TrainInRangeCheck()
	// {
	// 	while (true)
	// 	{
	// 		bool loadPresentOnTrack;
	//
	// 		do
	// 		{
	// 			yield return WaitFor.Seconds(1f);
	// 			loadPresentOnTrack = machineController.warehouseMachine.AnyTrainToLoadPresentOnTrack();
	//
	// 			if (loadPresentOnTrack && loadUnloadCoro == null)
	// 				goto label_4;
	// 		}
	// 		while (displayTrainInRangeText.text.Length == 0);
	// 		
	// 		ClearTrainInRangeText();
	// 		continue;
	// 		
	// 		label_4:
	// 		
	// 		SetScreen(WarehouseMachineController.TextPreset.TrainInRange, extra: !loadPresentOnTrack ? "whm/unload_brackets" : (!unloadPresentOnTrack ? "whm/load_brackets" : "whm/load_unload_brackets"));
	// 	}
	// }
	
	protected override void DisplayIdleText()
	{
		displayTitleText.SetText("This machine loads shipping containers");
		displayText.SetText("Move the handle to start");
	}
}