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
	
	public static bool IsFull(this Car car)
	{
		return car.LoadedCargoAmount >= car.capacity;
	}

	public static Vector3 OnlyX(this Vector3 vector)
	{
		return new Vector3(vector.x, 0, 0);
	}
	
	public static Vector3 OnlyY(this Vector3 vector)
	{
		return new Vector3(0, vector.y, 0);
	}
	
	public static Vector3 OnlyZ(this Vector3 vector)
	{
		return new Vector3(0, 0, vector.z);
	}

	public static Vector3 ClampMagnitude(this Vector3 vector, float maxLength)
	{
		return Vector3.ClampMagnitude(vector, maxLength);
	}
}