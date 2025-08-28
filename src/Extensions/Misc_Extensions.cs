using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;

namespace better_loading;

public static class Misc_Extensions
{
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
	
	public static bool IsFull(this Car car)
	{
		return car.LoadedCargoAmount >= car.capacity;
	}
}