using System.Collections.Generic;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

/// <summary>
/// Common code for the loading and unloading patches in this file 
/// </summary>
public static class CMCPatchesShared
{
	public static AudioClip ChingSound;
	
	private record struct CarWithCargo(TrainCarType_v2 CarType, CargoType CargoType)
	{
		public readonly TrainCarType_v2 CarType = CarType;
		public readonly CargoType CargoType = CargoType;
	}
	
	//only these car-cargo combinations will have a visibly rising cargo level. with others the cargo will appear when the car is full, just like the base game
	private static readonly Dictionary<CarWithCargo, Utilities.MinMax> fullySupportedCarTypes = new() 
	{
		{new CarWithCargo(TrainCarType.HopperBrown.ToV2().parentType, CargoType.Coal), new Utilities.MinMax(-2.8f, 0f)},
		{new CarWithCargo(TrainCarType.HopperBrown.ToV2().parentType, CargoType.IronOre), new Utilities.MinMax(-1.2f, 0f)},
	};
	
	public static void PlayCarFullEmptySound(string fullOrEmpty)
	{
		Main.Debug($"Car is {fullOrEmpty}, playing sound");
		ChingSound.Play2D();
	}
	
	public static void UpdateCargoLevel(CargoModelController modelController, CargoType cargoType, bool isLoading)
	{
		var cargoTransform = modelController.currentCargoModel.transform;
		var trainCarType = modelController.trainCar.carLivery.parentType;
		
		if (fullySupportedCarTypes.TryGetValue(new CarWithCargo(trainCarType, cargoType), out var minMax))
		{
			var loadLevel01 = modelController.trainCar.LoadedCargoAmount / modelController.trainCar.cargoCapacity;
			var yLevel = Utilities.Map(loadLevel01, 0, 1, minMax.minimum, minMax.maximum);
			cargoTransform.localPosition = new Vector3(cargoTransform.localPosition.x, yLevel, cargoTransform.localPosition.z);
		}
		else
		{
			if (isLoading && modelController.trainCar.IsFull())
			{
				cargoTransform.gameObject.SetActive(true);
			}
			else if (!isLoading && modelController.trainCar.IsEmpty())
			{
				cargoTransform.gameObject.SetActive(false);
			}
		}
	}
}

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

		var trainCar =  __instance.trainCar;
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

/// <summary>
/// visual cargo unloading (bulk cargo)
/// </summary>
[HarmonyPatch(typeof(CargoModelController))]
[HarmonyPatch(nameof(CargoModelController.OnCargoUnloaded))]
public class CargoModelController_OnCargoUnloaded_Patch
{
	private static bool Prefix(CargoModelController __instance)
	{
		var cargoType = __instance.trainCar.logicCar.LastUnloadedCargoType;
		if(!BulkMachine.IsCargoTypeSupported(cargoType)) return true;
		
		var trainCar =  __instance.trainCar;
		
		if (trainCar.IsEmpty())
		{
			if (trainCar.IsCargoLoadedUnloadedByMachine)
			{
				CMCPatchesShared.PlayCarFullEmptySound("empty");
			}

			DestroyCargoModel(__instance);
		}
		else
		{
			CMCPatchesShared.UpdateCargoLevel(__instance, cargoType, false);
		}

		return false;
	}

	private static void DestroyCargoModel(CargoModelController __instance)
	{
		__instance.currentCargoModelIndex = null;
		
		if(__instance.currentCargoModel == null) return;
		
		__instance.DestroyCurrentCargoModel();
		__instance.trainColliders.SetupCargo(null);
	}
}