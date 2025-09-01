using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class ContainerMachine: AdvancedMachine
{
	private RailTrackBogiesOnTrack bogiesOnTrackComponent;
	
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
		bogiesOnTrackComponent = MachineController.warehouseTrack.GetComponent<RailTrackBogiesOnTrack>();
	}

	protected override void StartLoadingSequence()
	{
		if (coroutineIsRunning)
			return;
		
		loadUnloadCoro = StartCoroutine(Loading());
	}

	protected override void StopLoadingSequence()
	{
		if (!coroutineIsRunning)
			return;
		
		StopCoroutine(loadUnloadCoro);
		coroutineIsRunning = false;
		DisplayIdleText();
	}
	
	public IEnumerator Loading()
	{
		coroutineIsRunning = true;
		Main.Debug(nameof(Loading));
		
		MachineController.machineSound.Play(transform.position, parent: transform);

		//todo
		var isLoading = true;
		
		var currentLoadUnloadData = MachineController.warehouseMachine.GetCurrentLoadUnloadData(isLoading ? WarehouseTaskType.Loading : WarehouseTaskType.Unloading);
		if (currentLoadUnloadData.Count == 0)
		{
			StopLoadingSequence();
		}
		else
		{
			MachineController.LoadOrUnloadOngoing = true;
      displayText.SetText("Busy");
      
      var anythingProcessed = false;
      foreach (var loadUnloadData in currentLoadUnloadData)
      {
        yield return null;
        
        if (loadUnloadData.state == WarehouseMachine.WarehouseLoadUnloadDataPerJob.State.SomeCarsPresentLoadUnloadForbiden)
        {
          displayText.SetText($"Error {loadUnloadData.id}");
        }
        else
        {
          var availableToProcess = loadUnloadData.tasksAvailableToProcess;
          if (availableToProcess != null)
          {
            if (availableToProcess.Any(warehouseTask => MachineController.AnyCarMoving(warehouseTask.cars)))
            {
	            displayText.SetText($"Moving {loadUnloadData.id}");
              continue;
            }
            foreach (var task in availableToProcess)
            {
              foreach (var taskCar in task.cars)
              {
	              if (taskCar.CurrentCargoTypeInCar == CargoType.None)
	              {
		              var amountToLoad = task.cargoAmount >= taskCar.capacity ? taskCar.capacity : task.cargoAmount;
		              taskCar.LoadCargo(amountToLoad, task.cargoType, MachineController.warehouseMachine);
		              
		              displayText.SetText($"CarUpdated {loadUnloadData.id} {taskCar} {task.cargoType.ToV2()}");
	              }
	              // else if() //todo 2 containers, 1 voor 1 laden
	              
                // var car2 = isLoading ? machineController.warehouseMachine.LoadOneCarOfTask(task) : machineController.warehouseMachine.UnloadOneCarOfTask(task);
                
                // var cargoType = isLoading ? car2.CurrentCargoTypeInCar : car2.LastUnloadedCargoType;
                
                yield return null;
              }
            }
          }
          
          anythingProcessed = true;
          if (loadUnloadData.state == WarehouseMachine.WarehouseLoadUnloadDataPerJob.State.PartialLoadUnloadPossible)
          {
	          displayText.SetText($"Partial {loadUnloadData.id}");
          }
          else if (loadUnloadData.state == WarehouseMachine.WarehouseLoadUnloadDataPerJob.State.FullLoadUnloadPossible)
          {
	          displayText.SetText($"Full {loadUnloadData.id}");
          }
        }
      }

      if (anythingProcessed)
      {
	      displayText.SetText("Completed");
      }
      else
      {
	      displayText.SetText("Failed");
      }

      MachineController.LoadOrUnloadOngoing = false;
      yield return StartCoroutine(ResetTextToIdleDisplay(anythingProcessed ? 10f : 4f));
      MachineController.loadUnloadCoro = null;
		}
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