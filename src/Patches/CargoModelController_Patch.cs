using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.Utils;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

/// <summary>
/// visual cargo loading
/// </summary>
[HarmonyPatch(typeof(CargoModelController))]
[HarmonyPatch(nameof(CargoModelController.OnCargoLoaded))]
public class CargoModelController_OnCargoLoaded_Patch 
{
	private static bool Prefix(CargoModelController __instance, CargoType _)
	{
		if(_ != CargoType.Coal) return true;

		if (!__instance.currentCargoModel)
		{
			CreateCargoModel(__instance, _);
		}

		if (__instance.trainCar.IsCargoLoadedUnloadedByMachine &&
		    __instance.trainCar.LoadedCargoAmount >= __instance.trainCar.cargoCapacity)
		{
			PlayCarFullSound(__instance);
		}

		UpdateCargoLevel(__instance);
		
		return false;
	}

	private const float MIN_COAL_LEVEL = -2.8f;
	private const float MAX_COAL_LEVEL = 0;
	
	//TODO use IndicatorModelChanger & IndicatorPortReader
	private static void UpdateCargoLevel(CargoModelController modelController)
	{
		var modelTransform = modelController.currentCargoModel.transform;
		var loadLevel01 = modelController.trainCar.LoadedCargoAmount / modelController.trainCar.cargoCapacity;
		var yLevel = Stuff.Map(loadLevel01, 0, 1, MIN_COAL_LEVEL, MAX_COAL_LEVEL);
		
		modelTransform.localPosition = new Vector3(modelTransform.localPosition.x, yLevel, modelTransform.localPosition.z);
	}

	private static void PlayCarFullSound(CargoModelController __instance)
	{
		Main.Debug("Car is full, playing sound");
		
		SingletonBehaviour<AudioManager>.Instance.cargoLoadUnload?.Play(__instance.trainCar.transform.position,
			minDistance: 10f, parent: __instance.trainCar.transform);
	}

	private static void CreateCargoModel(CargoModelController __instance, CargoType cargoType)
	{
		var trainCarType = __instance.trainCar.carLivery.parentType;
		var cargoPrefabs = __instance.trainCar.LoadedCargo.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(CargoModelController_OnCargoLoaded_Patch)}.{nameof(CreateCargoModel)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {cargoType}");
			return;
		}

		if (!__instance.currentCargoModelIndex.HasValue)
			__instance.currentCargoModelIndex = (byte) Random.Range(0, cargoPrefabs.Length);
		__instance.currentCargoModel = Object.Instantiate(cargoPrefabs[Mathf.Min(__instance.currentCargoModelIndex.Value, cargoPrefabs.Length - 1)], __instance.trainCar.interior.transform, false);
		__instance.currentCargoModel.transform.localPosition = Vector3.zero;
		__instance.currentCargoModel.transform.localRotation = Quaternion.identity;
		
		__instance.trainColliders.SetupCargo(__instance.currentCargoModel);
	}
}