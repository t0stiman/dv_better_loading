using System;
using DV.Localization;
using DV.ThingTypes.TransitionHelpers;
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
		if (__instance.trainCar.logicCar == null)
		{
			return;
		}
		
		//loading -> LoadedCargo
		//unloading -> LastUnloadedCargoType
		if(BulkMachine.IsCargoTypeSupported(__instance.trainCar.LoadedCargo))
		{
			__instance.cargoMassText += $", {__instance.trainCar.GetFillPercent()}%";
		}
		else if (BulkMachine.IsCargoTypeSupported(__instance.trainCar.logicCar.LastUnloadedCargoType) &&
		         !__instance.trainCar.IsEmpty())
		{
			var cargoType = __instance.trainCar.logicCar.LastUnloadedCargoType;
			__instance.cargoTypeText = LocalizationAPI.L(cargoType.ToV2().localizationKeyShort);
			var cargoMass = __instance.trainCar.massController.CargoMass;
			__instance.cargoMassText = $"{Mathf.RoundToInt(cargoMass)}kg, {__instance.trainCar.GetFillPercent()}%";
		}
		else
		{
			return;
		}
		
		__instance.RefreshDerivedCargoJobData();
	}
}