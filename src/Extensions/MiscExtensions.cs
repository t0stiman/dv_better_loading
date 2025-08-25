using System.Collections.Generic;
using DV;
using DV.ThingTypes;
using UnityEngine;

namespace better_loading;

public static class MiscExtensions
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
	
	/// <summary>
	/// Create a LayerMask that matches only the layer provided.
	/// </summary>
	public static LayerMask LayerMaskFromInt(int layer)
	{
		return 1 << layer;
	}
	
	public static GameObject FindChildByName(this GameObject gameObject, string name)
	{
		return gameObject.transform.FindChildByName(name).gameObject;
	}
	
	public static string Join(this IEnumerable<string> strings, string separator)
	{
		return string.Join(separator, strings);
	}
}