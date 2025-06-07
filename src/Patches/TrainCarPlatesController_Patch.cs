using System;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

/// <summary>
/// Show fill % on traincar plates
/// </summary>
[HarmonyPatch(typeof(TrainCarPlatesController))]
[HarmonyPatch(nameof(TrainCarPlatesController.UpdateCargoData))]
[HarmonyPatch(new Type[] {})]
public class TrainCarPlatesController_UpdateCargoData_Patch 
{
	private static void Postfix(TrainCarPlatesController __instance)
	{
		if (
			// __instance.trainCar.LoadedCargo != CargoType.Coal ||
		    __instance.trainCar.logicCar == null)
		{
			return;
		}

		var capacity = __instance.trainCar.logicCar.capacity;
		var loadedCargoAmount = __instance.trainCar.logicCar.LoadedCargoAmount;
		
		__instance.cargoMassText += $", {Mathf.RoundToInt(loadedCargoAmount / capacity * 100f)}%";
		__instance.RefreshDerivedCargoJobData();
	}
}