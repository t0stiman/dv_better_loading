using System;
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
			__instance.trainCar.logicCar == null ||
			!__instance.trainCar.LoadedCargo.IsSupportedBulkType()
			)
		{
			return;
		}
		
		__instance.cargoMassText += $", {__instance.trainCar.GetFillPercent()}%";
		__instance.RefreshDerivedCargoJobData();
	}
}