using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class BulkUnloader: BulkMachine
{
	private List<TrainCarCache> carsOnUnloader = new();
	private MeshRenderer overlapBoxRenderer;

	// This layer is not used by DV. See DV.Layers.DVLayer .
	private const int UNLOADER_LAYER = 18;
	private static readonly LayerMask UNLOADER_MASK = Misc_Extensions.LayerMaskFromInt(UNLOADER_LAYER);
	
	private record struct TrainCarCache(TrainCar trainCar, ShuteEffectsManager effects)
	{
		public readonly TrainCar trainCar = trainCar;
		public readonly ShuteEffectsManager effects = effects;
	}
	
	#region setup
	
	protected override void Initialize()
	{
		if(initialized) return;
		
		var unloadPlatform = GameObject.Find(LoaderInfo.building.name)?.transform;
		if (unloadPlatform)
		{
			Main.Debug($"Unload platform on bulk machine {gameObject.name}: {unloadPlatform.gameObject.GetPath()}");
		}
		else
		{
			Main.Error($"Failed to find unload platform {LoaderInfo.building.name} on bulk machine {gameObject.name}");
			return;
		}
		
		SetupOverlapBox(unloadPlatform);
		initialized = true;
	}
	
	private void SetupOverlapBox(Transform unloadPlatform)
	{
		var box = unloadPlatform.GetComponentInChildren<BoxCollider>();
		
		overlapBoxObject = Utilities.CreateDebugCube(box.transform.parent, nameof(overlapBoxObject));
		overlapBoxObject.transform.localScale = box.size;
		overlapBoxObject.transform.localPosition = box.center;
		overlapBoxObject.transform.localEulerAngles = Vector3.zero;
		
		//above ground
		overlapBoxObject.transform.Translate(0, overlapBoxObject.transform.localScale.y, 0);
		overlapBoxObject.transform.parent = unloadPlatform;
		
		overlapBoxObject.layer = UNLOADER_LAYER;
		overlapBoxRenderer = overlapBoxObject.GetComponent<MeshRenderer>();
	}
	
	protected override void SetupTexts()
	{
		SetupTexts2("un");
	}
	
	#endregion
	
	protected override void SetEnabledText()
	{
		SetDisplayTitleText("Machine enabled");
		SetDisplayDescriptionText("Slowly drive the train over the unloading area");
	}
	
	protected override void DisableMachine()
	{
		if (!coroutineIsRunning)
			return;

		StopAllTransferring(nameof(DisableMachine));
		StopCoroutine(loadUnloadCoroutine);
		coroutineIsRunning = false;
		clonedMachineController.DisplayIdleText();
	}
	
	#region update
	
	protected void Update()
	{
		if(!initialized) return;
		overlapBoxRenderer.enabled = Main.MySettings.EnableDebugBoxes;
	}
	
	protected override IEnumerator LoadingUnloading()
	{
		const float stopUnloadingWaitTime = 0.2f;

		coroutineIsRunning = true;
		Main.Debug($"{nameof(BulkUnloader)}.{nameof(LoadingUnloading)}");
		SetEnabledText();
		clonedMachineController.machineSound.Play(transform.position, parent: transform);

		while (true)
		{
			yield return null;
			
			//game is paused?
			if (!TimeUtil.IsFlowing) continue;

			FindCarsOnUnloader();
			
			if (carsOnUnloader.Count == 0)
			{
				yield return WaitFor.Seconds(stopUnloadingWaitTime);
				continue;
			}

			var displayDescriptionBuilder = new StringBuilder();

			foreach (var (trainCar, effects) in carsOnUnloader)
			{
				if (!effects)
				{
					continue;
				}
				
				if (!TryGetTask(trainCar, out WarehouseTask task))
				{
					effects.StopTransferring("has no active task");
					continue;
				}

				if (task.warehouseTaskType != WarehouseTaskType.Unloading)
				{
					effects.StopTransferring($"wrong task type {task.warehouseTaskType}");
					continue;
				}
				
				if (!IsCargoTypeSupported(task.cargoType))
				{
					effects.StopTransferring($"{task.cargoType} is not supported by {nameof(BulkUnloader)}");
					continue;
				}
		
				var logicCar = trainCar.logicCar;
				Main.DebugVerbose($"LoadedCargoAmount: {logicCar.LoadedCargoAmount} capacity: {logicCar.capacity}");
		
				//empty
				if (logicCar.IsEmpty())
				{
					effects.StopTransferring("is already empty");
					continue;
				}
			
				DoUnloadStep(logicCar, effects, task.cargoType);
				displayDescriptionBuilder.AppendLine($"{logicCar.ID} {task.cargoType.ToV2().GetLocalizedName()} {trainCar.GetFillPercent()}%");
			}

			SetDisplayDescriptionText(displayDescriptionBuilder.ToString());
		}
	}
	
	private void FindCarsOnUnloader()
	{
		var carsWithBothBogiesOnUnloader = VanillaMachineController.warehouseTrack.BogiesOnTrack()
			//is in box
			.Where(bogie => OverlapBoxContains(bogie.transform.position))
			//group by car
			.GroupBy(bogie => bogie.Car)
			//both bogies need to be in position
			.Where(group => group.Count() >= 2)
			.Select(bogieGroup => bogieGroup.First()._car)
			.ToArray();

		// remove TrainCars that aren't there anymore
		foreach (var notFoundCar in carsOnUnloader.Where(cache => !carsWithBothBogiesOnUnloader.Contains(cache.trainCar)))
		{
			notFoundCar.effects?.StopTransferring("not there anymore");
		}
		carsOnUnloader.RemoveAll(cache => !carsWithBothBogiesOnUnloader.Contains(cache.trainCar));

		var newCars = carsWithBothBogiesOnUnloader.Where(car => carsOnUnloader.All(cache => cache.trainCar != car)).ToArray();
		foreach (var newCar in newCars)
		{
			Main.Debug($"car under loader: {newCar.carType}");
			carsOnUnloader.Add(new TrainCarCache(newCar, newCar.gameObject.GetComponent<ShuteEffectsManager>()));
		}
	}

	private static readonly Vector3 smallExtents = new(0.001f, 0.001f, 0.001f);
	
	// Returns true if position is inside overlapBox
	private static bool OverlapBoxContains(Vector3 position)
	{
		return Physics.CheckBox(position, smallExtents, Quaternion.identity, UNLOADER_MASK);
	}

	private void DoUnloadStep(Car logicCar, ShuteEffectsManager effects, CargoType cargoType)
	{
		effects.StartTransferring();
		
		var cargoTypeV2 = cargoType.ToV2();
		var kgToUnload = Main.MySettings.BulkLoadSpeedMultiplier * loadUnloadSpeed[cargoType] * Time.deltaTime;
		
		// DV remembers the amount of cargo on a car in units, not kg. For example, the open hoppers can carry 1 unit of coal, which is 56000 kg.
		var unitsToUnload = kgToUnload / cargoTypeV2.massPerUnit;
		
		Main.DebugVerbose($"{nameof(DoUnloadStep)}: {kgToUnload} kg, {unitsToUnload} units");
			
		// prevent going under 0
		if (logicCar.LoadedCargoAmount - unitsToUnload < 0)
		{
			Main.DebugVerbose($"{nameof(DoUnloadStep)}: {logicCar.LoadedCargoAmount} - {unitsToUnload} < 0");
			
			//empty
			unitsToUnload = logicCar.LoadedCargoAmount;
			Main.DebugVerbose($"{nameof(DoUnloadStep)}: {unitsToUnload} units");
		}
		
		logicCar.UnloadCargo(unitsToUnload, cargoType, VanillaMachineController.warehouseMachine);

		//UnloadCargo sets CurrentCargoTypeInCar to CargoType.None
		if (!logicCar.IsEmpty())
		{
			logicCar.CurrentCargoTypeInCar = cargoType;
		}
	}
	
	private void StopAllTransferring(string reason)
	{
		foreach (var car in carsOnUnloader)
		{
			car.effects?.StopTransferring(reason);
		}
		
		SetEnabledText();
	}

	#endregion
}