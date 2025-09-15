using DV;
using DV.ThingTypes;
using UnityEngine;

namespace better_loading;

public static class TrainCar_Extensions
{
	public static bool CanLoad(this TrainCar trainCar, CargoType_v2 cargoType)
	{
		return Globals.G.Types.CarTypeToLoadableCargo[trainCar.carLivery.parentType].Contains(cargoType);
	}

	public static int GetFillPercent(this TrainCar trainCar)
	{
		var capacity = trainCar.logicCar.capacity;
		var loadedCargoAmount = trainCar.logicCar.LoadedCargoAmount;
		return Mathf.RoundToInt(loadedCargoAmount / capacity * 100f);
	}

	public static bool IsFull(this TrainCar trainCar)
	{
		return trainCar.logicCar.IsFull();
	}
	
	public static bool IsEmpty(this TrainCar trainCar)
	{
		return trainCar.logicCar.IsEmpty();
	}
}