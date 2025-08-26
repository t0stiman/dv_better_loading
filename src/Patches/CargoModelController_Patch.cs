using System.Collections.Generic;
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
	private record struct CarWithCargo(TrainCarType_v2 CarType, CargoType CargoType)
	{
		public readonly TrainCarType_v2 CarType = CarType;
		public readonly CargoType CargoType = CargoType;
	}
	
	private record struct MinMax(float minimum, float maximum)
	{
		public readonly float minimum = minimum;
		public readonly float maximum = maximum;
	}
	
	//only these car-cargo combinations will have a visibly rising cargo level. with others the cargo will appear when the car is full, just like the base game
	private static readonly Dictionary<CarWithCargo, MinMax> fullySupportedCarTypes = new() {
		{new CarWithCargo(TrainCarType.HopperBrown.ToV2().parentType, CargoType.Coal), new MinMax(-2.8f, 0f)},
		{new CarWithCargo(TrainCarType.HopperBrown.ToV2().parentType, CargoType.IronOre), new MinMax(-1.5f, 0f)}, //todo
	};
	
	private static bool Prefix(CargoModelController __instance, CargoType _)
	{
		if(!_.IsSupportedBulkType()) return true;

		if (!__instance.currentCargoModel)
		{
			CreateCargoModel(__instance, _);
		}

		if (__instance.trainCar.IsCargoLoadedUnloadedByMachine &&
		    __instance.trainCar.IsFull())
		{
			PlayCarFullSound(__instance);
		}

		UpdateCargoLevel(__instance, _);

		return false;
	}
	
	private static void UpdateCargoLevel(CargoModelController modelController, CargoType cargoType)
	{
		var cargoTransform = modelController.currentCargoModel.transform;
		var trainCarType = modelController.trainCar.carType.ToV2().parentType;
		
		if (fullySupportedCarTypes.TryGetValue(new CarWithCargo(trainCarType, cargoType), out var minMax))
		{
			var loadLevel01 = modelController.trainCar.LoadedCargoAmount / modelController.trainCar.cargoCapacity;
			var yLevel = Utilities.Map(loadLevel01, 0, 1, minMax.minimum, minMax.maximum);
			cargoTransform.localPosition = new Vector3(cargoTransform.localPosition.x, yLevel, cargoTransform.localPosition.z);
		}
		else
		{
			cargoTransform.gameObject.SetActive(modelController.trainCar.IsFull());
		}
	}

	private static void PlayCarFullSound(CargoModelController __instance)
	{
		Main.Debug("Car is full, playing sound");
		
		SingletonBehaviour<AudioManager>.Instance.cargoLoadUnload?.Play(
			__instance.trainCar.transform.position,
			minDistance: 10f,
			parent: __instance.trainCar.transform
		);
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
		__instance.currentCargoModel = Object.Instantiate(cargoPrefabs[Mathf.Min(__instance.currentCargoModelIndex.Value, cargoPrefabs.Length - 1)], __instance.trainCar.interior.transform, false);
		__instance.currentCargoModel.transform.localPosition = Vector3.zero;
		__instance.currentCargoModel.transform.localRotation = Quaternion.identity;
		
		__instance.trainColliders.SetupCargo(__instance.currentCargoModel);
	}
}