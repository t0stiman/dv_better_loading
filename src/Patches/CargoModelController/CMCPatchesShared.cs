using System.Collections.Generic;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
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
	
	/// <summary>
	/// Only these car-cargo combinations will have a visibly rising cargo level. With others the cargo will appear when the car is full, just like the base game
	/// </summary>
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