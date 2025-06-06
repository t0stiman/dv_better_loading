using DV;
using DV.ThingTypes;
using UnityEngine;

namespace better_loading;

public static class Extensions
{
	public static bool CanLoad(this TrainCar trainCar, CargoType_v2 cargoType)
	{
		return Globals.G.Types.CarTypeToLoadableCargo[trainCar.carLivery.parentType].Contains(cargoType);
	}
}