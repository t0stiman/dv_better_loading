using UnityEngine;

namespace better_loading;

public static class Utilities
{
	public static float Map(float input, float in_min, float in_max, float out_min, float out_max)
	{
		return (input - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}
	
	public record struct MinMax(float minimum, float maximum)
	{
		public readonly float minimum = minimum;
		public readonly float maximum = maximum;
	}

	public static GameObject CreateDebugCube(Transform parent, string name = "debug cube")
	{
		return CreateDebugCube(parent, parent.position, parent.rotation, name);
	}
	
	public static GameObject CreateDebugCube(Transform parent, Vector3 position, Quaternion rotation, string name = "debug cube")
	{
		var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.name = name;
		cube.transform.SetParent(parent);
		cube.transform.position = position;
		cube.transform.rotation = rotation;
		cube.SetActive(Main.MySettings.EnableDebugBoxes);
		return cube;
	}
	
	public static GameObject CreateGameObject(Transform parent, Vector3 position, Quaternion rotation, string name, bool instantiateInWorldSpace = true)
	{
		var obj = new GameObject(name);
		obj.transform.SetParent(parent);
		if (instantiateInWorldSpace)
		{
			obj.transform.position = position;
			obj.transform.rotation = rotation;
		}
		else
		{
			obj.transform.localPosition = position;
			obj.transform.localRotation = rotation;
		}

		return obj;
	}
}