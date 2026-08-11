using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

/// <summary>
/// visual cargo loading (bulk cargo)
/// </summary>
[HarmonyPatch(typeof(CargoModelController))]
[HarmonyPatch(nameof(CargoModelController.OnCargoLoaded))]
public class CargoModelController_OnCargoLoaded_Patch 
{
	private static bool Prefix(CargoModelController __instance, CargoType _)
	{
		if(!BulkMachine.IsCargoTypeSupported(_)) return true;

		if (!__instance.currentCargoModel)
		{
			CreateCargoModel(__instance, _);
		}
		
		//TODO nullref exception

		var trainCar = __instance.trainCar;
		if (trainCar.IsCargoLoadedUnloadedByMachine &&
		    trainCar.IsFull())
		{
			CMCPatchesShared.PlayCarFullEmptySound("full");
		}

		CMCPatchesShared.UpdateCargoLevel(__instance, _, true);
		return false;
	}

	private static void CreateCargoModel(CargoModelController __instance, CargoType cargoType)
	{
		var trainCarType = __instance.trainCar.carLivery.parentType;
		var cargoPrefabs = cargoType.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(CargoModelController_OnCargoLoaded_Patch)}.{nameof(CreateCargoModel)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {cargoType}");
			return;
		}

		if (!__instance.currentCargoModelIndex.HasValue)
			__instance.currentCargoModelIndex = (byte) Random.Range(0, cargoPrefabs.Length);
		__instance.currentCargoModel = Object.Instantiate(cargoPrefabs[Mathf.Min(__instance.currentCargoModelIndex.Value, cargoPrefabs.Length - 1)], __instance.trainCar.interior, false);
		__instance.currentCargoModel.transform.localPosition = Vector3.zero;
		__instance.currentCargoModel.transform.localRotation = Quaternion.identity;
		
		__instance.trainColliders.SetupCargo(__instance.currentCargoModel);
	}
}