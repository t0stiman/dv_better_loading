using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DV;
using DV.ThingTypes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

public static class Extensions
{
	public static bool CanLoad(this TrainCar trainCar, CargoType_v2 cargoType)
	{
		return Globals.G.Types.CarTypeToLoadableCargo[trainCar.carLivery.parentType].Contains(cargoType);
	}
	
	/// <summary>
	/// Create a LayerMask that matches only the layer provided.
	/// </summary>
	public static LayerMask LayerMaskFromInt(int layer)
	{
		return 1 << layer;
	}

	public static Transform[] FindAllByName(this Scene scene, string name)
	{
		return scene.GetRootGameObjects()
			.SelectMany(rootObject => rootObject.transform.FindChildrenByName(name))
			.ToArray();
	}
	
	public static Transform[] FindChildrenByName(this Transform transform, string name)
	{
		return transform.GetComponentsInChildren<Transform>(true)
			.Where(anotherTransform => anotherTransform.name == name)
			.ToArray();
	}
	
	public static Transform FindChildByName(this Transform transform, string name)
	{
		return transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(anotherTransform => anotherTransform.name == name);
	}
	
	public static GameObject FindChildByName(this GameObject gameObject, string name)
	{
		return gameObject.transform.FindChildByName(name).gameObject;
	}

	private static IReadOnlyList<CargoType> supportedBulkTypes { get; } = new List<CargoType>
	{
		CargoType.Coal,
		// CargoType.IronOre
		//graan
	};
	
	public static bool IsSupportedBulkType(this CargoType cargoType)
	{
		return supportedBulkTypes.Contains(cargoType);
	}
}