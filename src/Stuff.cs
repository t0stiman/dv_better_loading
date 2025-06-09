using UnityEngine;

namespace better_loading;

public static class Stuff
{
	public static float Map(float input, float in_min, float in_max, float out_min, float out_max)
	{
		return (input - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}

	/// <summary>
	/// Get child by name 
	/// </summary>
	public static Transform GetChild(this Transform deez, string name)
	{
		for (var i = 0; i < deez.childCount; i++)
		{
			if (deez.GetChild(i).name != name) continue;
			return deez.GetChild(i);
		}

		return null;
	}
	
	// public static T CopyComponent<T>(T original, GameObject destination) where T : Component
	// {
	// 	var type = original.GetType();
	// 	var copy = destination.AddComponent(type);
	// 	var fields = type.GetFields();
	// 	foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
	// 	return copy as T;
	// }
}