using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using UnityEngine;

namespace better_loading;

public class VehiclesMachine: AdvancedMachine
{
	public static bool IsCargoTypeSupported(CargoType cargoType)
	{
		switch (cargoType)
		{
			case CargoType.NewCars:
			case CargoType.ImportedNewCars:
			// case CargoType.Tractors:
			// case CargoType.CityBuses:
			// case CargoType.Excavators:
			// case CargoType.Tanks:
			// case CargoType.MilitaryCars:
			// case CargoType.MilitaryTrucks:
				return true;
			
			default:
				return false;
		}
	}
	
	public override bool IsSupportedCargoType(CargoType cargoType)
	{
		return IsCargoTypeSupported(cargoType);
	}
	
	private void OnDisable()
	{
		// StopTransferSequence(); todo
	}
	
	private void Start()
	{
		SetupTexts("Car\nloader");
		clonedMachineController.DisplayIdleText();
	}
	
	private void SetupTexts(string titleText)
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), titleText);
		FilterCargoOnScreen(clonedMachineController, cargoTypes, false);
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
		Main.Debug($"{nameof(VehiclesMachine)}.{nameof(LoadingUnloading)}");
		
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

		// ================ Loading ================

		if (isLoading)
		{
			var queue = CreateLoadingQueue(readyTasks);
			foreach (var somethingToLoad in queue)
			{
				anythingProcessed = true;
				// SetBusyScreen(isLoading, somethingToLoad.task.cargoType, somethingToLoad.car);

				somethingToLoad.gameObject.SetActive(true);
				var transform_ = somethingToLoad.gameObject.transform;

				foreach (var trainCar in queue.Select(idk => idk.trainCar.transform))
				{
					transform_.position = trainCar.position;
					transform_.rotation = trainCar.rotation;
					yield return WaitFor.Seconds(1);
				}

				Destroy(somethingToLoad.gameObject);
				
				// LoadTrainCar(somethingToLoad.task, trainCar, somethingToLoad.slotContainer.Value.cargoModelIndex);
			}
		}

		// ================ Unloading ================
		
		//todo
		
		// ===========================================
		
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

	private List<InstantiatedCargo> CreateLoadingQueue(WarehouseTask[] readyTasks)
	{
		var loadTasks = readyTasks.Where(task => task.warehouseTaskType == WarehouseTaskType.Loading).ToArray();
		// List<InstantiatedCargo> loadingQueue = new();

		return SpawnCargos(loadTasks);
		
		//sort by car position
		// var aaa = loadTasks.SelectMany(task => task.cars)
		// 	.Where(car => car.CurrentCargoTypeInCar == CargoType.None)
		// 	.S
		// 	.ToArray();
	}

	private List<InstantiatedCargo> SpawnCargos(WarehouseTask[] loadTasks)
	{
		var cargos = new List<InstantiatedCargo>();
		
		foreach (var task in loadTasks)
		{
			foreach (var taskCar in task.cars.Where(car => car.CurrentCargoTypeInCar == CargoType.None))
			{
				var trainCar = taskCar.TrainCar();
				var instantiatedCargo = Utilities.InstantiateCargoPrefab(trainCar, task, Vector3.zero, Quaternion.identity, null,
					out var cargoModelIndex);
				instantiatedCargo.SetActive(false);
				cargos.Add(new InstantiatedCargo(instantiatedCargo, cargoModelIndex, trainCar));
			}
		}

		return cargos;
	}
}