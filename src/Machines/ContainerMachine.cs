using System;
using System.Collections;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace better_loading;

public class ContainerMachine: AdvancedMachine
{
	private const float speed = 2f; //todo
	
	//todo use prefabs?
	private static readonly string[] endings =
	{
		"SunOmni",
		"Iskar",
		"Obco",
		"Goorsk",
		"Krugmann",
		"Brohm",
		"AAG",
		"Sperex",
		"Novae",
		"Traeg",
		"Chemlek",
		"NeoGamma"
	};
	
	public static bool IsInShippingContainer(CargoType cargoType)
	{
		var cargoTypeString = cargoType.ToString();
		return cargoType == CargoType.ScrapContainers || endings.Any(ending => cargoTypeString.EndsWith(ending));
	}
	
	public override bool IsSupportedCargoType(CargoType cargoType)
	{
		return IsInShippingContainer(cargoType);
	}

	private void OnEnable()
	{
		// StartCoroutine(TrainInRangeCheck()); //todo
		DisplayIdleText();
	}
	
	private void OnDisable()
	{
		StopAllCoroutines();
		loadUnloadCoro = null;
	}

	private void Start()
	{
		base.Start_();
		
		SetupTexts("Container\nloader");
	}

	protected override void StartLoadingSequence()
	{
		if (loadUnloadCoro != null)
			return;
		
		loadUnloadCoro = StartCoroutine(Loading());
	}

	protected override void StopLoadingSequence()
	{
		if (loadUnloadCoro != null)
		{
			StopCoroutine(loadUnloadCoro);
		}
		
		DisplayIdleText();
	}
	
	public IEnumerator Loading()
	{
		yield return null;
		Main.Debug($"{nameof(ContainerMachine)}.{nameof(Loading)}");
		
		MachineController.machineSound.Play(transform.position, parent: transform);
		var anythingDone = false;
		
		foreach (var task in MachineController.warehouseMachine.currentTasks)
		{
			if (!MachineController.warehouseMachine.CarsPresentOnWarehouseTrack(task.cars) ||
					!task.readyForMachine ||
					MachineController.AnyCarMoving(task.cars))
			{
				continue;
			}
			//todo andere checks? 
			
			if (task.warehouseTaskType != WarehouseTaskType.Loading) continue; //todo
			
			foreach (var taskCar in task.cars)
			{
				if (taskCar.CurrentCargoTypeInCar != CargoType.None) continue;
				yield return null;
				
				anythingDone = true;

				var taskCargoV2 = task.cargoType.ToV2();
				displayText.SetText($"Loading {taskCargoV2.LocalizedName()} onto {taskCar.ID}");
				
				var taskTrainCar = taskCar.TrainCar();
				var fakeCargo = CreateCargoModel(taskTrainCar, task.cargoType);
								
				// move it
				do
				{
					yield return null;
					fakeCargo.transform.position = Vector3.MoveTowards(fakeCargo.transform.position, taskTrainCar.interior.position, speed * Time.deltaTime);
				} 
				while (Vector3.Distance(fakeCargo.transform.position, taskTrainCar.interior.position) > 0.05f);
				
				Destroy(fakeCargo); //todo interrupt coroutine -> destroy
								
				//todo fakecargo ook amount?
				var amountToLoad = task.cargoAmount >= taskCar.capacity ? taskCar.capacity : task.cargoAmount;
				taskCar.LoadCargo(amountToLoad, task.cargoType, MachineController.warehouseMachine);
			}
		}
		
		if (anythingDone)
		{
			displayText.SetText("Completed");
		}
		else
		{
			displayText.SetText("Failed");
			// todo play error sound
		}
		
		yield return StartCoroutine(ResetTextToIdleDisplay(anythingDone ? 
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_LONG :
			WarehouseMachineController.CLEAR_MACHINE_ACTION_TEXT_AFTER_TIME_SHORT));
		loadUnloadCoro = null;
	}
	
	private static GameObject CreateCargoModel(TrainCar trainCar, CargoType cargoType)
	{
		var trainCarType = trainCar.carLivery.parentType;
		var cargoPrefabs = cargoType.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(ContainerMachine)}.{nameof(CreateCargoModel)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {cargoType}");
			return null;
		}
		
		var cargoModelIndex = (byte) Random.Range(0, cargoPrefabs.Length);
		var trainCarTransform = trainCar.transform;
		return Instantiate(cargoPrefabs[cargoModelIndex], trainCarTransform.position + trainCarTransform.up * 5f, trainCarTransform.rotation);
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