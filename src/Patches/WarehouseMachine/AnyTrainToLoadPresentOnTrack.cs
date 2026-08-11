using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// Prevent loading bulk cargo with the default machine
/// </summary>
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.AnyTrainToLoadPresentOnTrack))]
public class WarehouseMachine_AnyTrainToLoadPresentOnTrack_Patch
{
	private static bool Prefix(WarehouseMachine __instance, ref bool __result)
	{
		if (!AdvancedMachine.TryGetAdvancedMachine(__instance, out var advancedMachine)) return true;
		
		foreach (var currentTask in __instance.currentTasks)
		{
			// =============
			if (!advancedMachine.IsSupportedCargoType(currentTask.cargoType) &&
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