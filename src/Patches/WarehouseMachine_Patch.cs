using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using HarmonyLib;

namespace better_loading.Patches;

// Prevent loading bulk cargo with the default machine
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.AnyTrainToLoadPresentOnTrack))]
public class WarehouseMachine_AnyTrainToLoadPresentOnTrack_Patch
{
	private static bool Prefix(WarehouseMachine __instance, ref bool __result)
	{
		if (!BulkMachine.AllWarehouseMachinesWithBulk.Contains(__instance)) return true;
		
		foreach (var currentTask in __instance.currentTasks)
		{
			// =============
			if (!currentTask.cargoType.IsSupportedBulkType() &&
			// =============
				currentTask.readyForMachine &&
		    currentTask.warehouseTaskType == WarehouseTaskType.Loading &&
		    __instance.CarsPresentOnWarehouseTrack(currentTask.cars))
			{
				__result = true;
				return false;
			}
		}
		if (__instance.specialDeliveries.Count > 0)
		{
			List<Car> cars = null;
			foreach (var specialDelivery in __instance.specialDeliveries)
			{
				if (specialDelivery.deliveryType != WarehouseTaskType.Loading) continue;
				if (cars == null)
				{
					cars = __instance.WarehouseTrack.GetCarsFullyOnTrack().Where(c =>
							__instance.currentTasks.All(t => !t.cars.Contains(c)))
						.ToList();
				}

				if (!__instance.CanCarsHandleSpecialDelivery(cars, specialDelivery)) continue;
				__result = true;
				return false;
			}
		}
		__result = false;
		return false;
	}
}

// Prevent unloading bulk cargo with the default machine
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.AnyTrainToUnloadPresentOnTrack))]
public class WarehouseMachine_AnyTrainToUnloadPresentOnTrack_Patch
{
	private static bool Prefix(WarehouseMachine __instance, ref bool __result)
	{
		if (!BulkMachine.AllWarehouseMachinesWithBulk.Contains(__instance)) return true;
		
		foreach (var currentTask in __instance.currentTasks)
		{
			// =============
			if (!currentTask.cargoType.IsSupportedBulkType() &&
			// =============
			    currentTask.readyForMachine && 
			    currentTask.warehouseTaskType == WarehouseTaskType.Unloading &&
			    __instance.CarsPresentOnWarehouseTrack(currentTask.cars))
			{
				__result = true;
				return false;
			}
		}
		if (__instance.specialDeliveries.Count > 0)
		{
			List<Car> cars = null;
			foreach (var specialDelivery in __instance.specialDeliveries)
			{
				if (specialDelivery.deliveryType != WarehouseTaskType.Unloading) continue;
				if (cars == null)
				{
					cars = __instance.WarehouseTrack.GetCarsFullyOnTrack().Where(c =>
							__instance.currentTasks.All(t => !t.cars.Contains(c)))
						.ToList();
				}

				if (!__instance.CanCarsHandleSpecialDelivery(cars, specialDelivery)) continue;
				__result = true;
				return false;
			}
		}
		__result = false;
		return false;
	}
}

// Prevent (un)loading bulk cargo with the default machine
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.GetCurrentLoadUnloadData))]
public class WarehouseMachine_GetCurrentLoadUnloadData_Patch
{
	private static void Postfix(WarehouseMachine __instance, ref List<WarehouseMachine.WarehouseLoadUnloadDataPerJob> __result)
	{
		if (!BulkMachine.AllWarehouseMachinesWithBulk.Contains(__instance)) return;
		
		// remove bulk cargo jobs
		__result = __result
			.Where(dataPerJob => !dataPerJob.tasksAvailableToProcess.Any(task => task.cargoType.IsSupportedBulkType()))
			.ToList();
	}
}


