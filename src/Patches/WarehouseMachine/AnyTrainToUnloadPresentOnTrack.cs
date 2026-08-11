using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// Prevent unloading bulk cargo with the default machine
/// </summary>
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.AnyTrainToUnloadPresentOnTrack))]
public class WarehouseMachine_AnyTrainToUnloadPresentOnTrack_Patch
{
	private static bool Prefix(WarehouseMachine __instance, ref bool __result)
	{
		if (Main.MySettings.AllowUsingDefaultMachine ||
		    !AdvancedMachine.TryGetAdvancedMachine(__instance, out var advancedMachine))
		{
			return true;
		}
		
		foreach (var currentTask in __instance.currentTasks)
		{
			// =============
			if (!advancedMachine.IsSupportedCargoType(currentTask.cargoType) &&
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