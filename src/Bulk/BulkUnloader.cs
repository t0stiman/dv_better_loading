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
	
	private record struct TrainCarCache(TrainCar trainCar, Collider collider, ShuteEffectsManager effects)
	{
		public readonly TrainCar trainCar = trainCar;
		public readonly Collider collider = collider;
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
		
		debugBox = Utilities.CreateDebugCube(box.transform.parent, nameof(debugBox));
		debugBox.transform.localScale = box.size;
		debugBox.transform.localPosition = box.center; 
		debugBox.transform.localEulerAngles = Vector3.zero;
		
		//above ground
		debugBox.transform.Translate(0, debugBox.transform.localScale.y, 0);

		overlapBoxRotation = debugBox.transform.rotation;
		overlapBoxHalfSize = debugBox.transform.lossyScale / 2f;
		overlapBoxCenter = debugBox.transform.position;
			
		Destroy(debugBox.GetComponent<BoxCollider>());
		debugBox.transform.parent = unloadPlatform;
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
		
		if (debugBox)
		{
			debugBox.SetActive(false);
		}
	}
	
	#region update
	
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

			foreach (var carCache in carsOnUnloader)
			{
				var trainCar = carCache.trainCar;
				
				if (!carCache.effects)
				{
					continue;
				}
				
				if (!TryGetTask(trainCar, out WarehouseTask task))
				{
					carCache.effects.StopTransferring("has no active task");
					continue;
				}

				if (task.warehouseTaskType != WarehouseTaskType.Unloading)
				{
					carCache.effects.StopTransferring($"wrong task type {task.warehouseTaskType}");
					continue;
				}
				
				if (!IsCargoTypeSupported(task.cargoType))
				{
					carCache.effects.StopTransferring($"{task.cargoType} is not supported by {nameof(BulkUnloader)}");
					continue;
				}
		
				var logicCar = trainCar.logicCar;
				Main.DebugVerbose($"LoadedCargoAmount: {logicCar.LoadedCargoAmount} capacity: {logicCar.capacity}");
		
				//empty
				if (logicCar.IsEmpty())
				{
					carCache.effects.StopTransferring("is already empty");
					continue;
				}
			
				DoUnloadStep(carCache, task.cargoType);
				displayDescriptionBuilder.AppendLine($"{logicCar.ID} {task.cargoType.ToV2().LocalizedName()} {carCache.trainCar.GetFillPercent()}%");
			}

			SetDisplayDescriptionText(displayDescriptionBuilder.ToString());
		}
	}
	
	private void FindCarsOnUnloader()
	{
		var resultsCount = Physics.OverlapBoxNonAlloc(overlapBoxCenter, overlapBoxHalfSize, overlapBoxResults, overlapBoxRotation, TRAINCAR_MASK);

		var notFoundColliders = carsOnUnloader.Select(cache => cache.collider).ToList();

		for (int i = 0; i < resultsCount; i++)
		{
			// use cache to reduce calls to expensive GetComponentInParent
			if(carsOnUnloader.Any(cache => cache.collider == overlapBoxResults[i]))
			{
				notFoundColliders.Remove(overlapBoxResults[i]);
				continue;
			}
			
			// on DV train cars GetComponent is sufficient, but on CCL cars the collider can be on a deeper level
			var trainCar = overlapBoxResults[i].transform.parent.GetComponentInParent<TrainCar>(); 
			if (trainCar)
			{
				var effects = trainCar.gameObject.GetComponent<ShuteEffectsManager>();
				
				carsOnUnloader.Add(new TrainCarCache(trainCar, overlapBoxResults[i], effects));
				Main.Debug($"car under loader: {trainCar.carType}");
				continue;
			}
			
			Main.Error($"Could not get {nameof(TrainCar)} in '{gameObject.GetPath()}'");
		}
		
		var notFoundCars = carsOnUnloader.Where(cache => notFoundColliders.Contains(cache.collider)).ToList(); 
		foreach (var carCache in notFoundCars)
		{
			carCache.effects?.StopTransferring("left the unloader");
		}
		
		//remove cars from the cache that are no longer there
		carsOnUnloader.RemoveAll(cache => notFoundColliders.Contains(cache.collider));
	}

	private void DoUnloadStep(TrainCarCache carCache, CargoType cargoType)
	{
		carCache.effects.StartTransferring();
		
		var logicCar = carCache.trainCar.logicCar;
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