using DV.Logic.Job;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public static class Utilities
{
	/// <summary>
	/// https://docs.arduino.cc/language-reference/en/functions/math/map/
	/// </summary>
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
		if (!Main.MySettings.EnableDebugBoxes)
		{
			cube.GetComponent<MeshRenderer>().enabled = false;
		}
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
	
	public static GameObject InstantiateCargoPrefab(TrainCar trainCar, WarehouseTask task, Vector3 position, Quaternion rotation, Transform parent, out byte cargoModelIndex_)
	{
		var trainCarType = trainCar.carLivery.parentType;
		var cargoPrefabs = task.cargoType.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(InstantiateCargoPrefab)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {task.cargoType}");
			cargoModelIndex_ = 0;
			return null;
		}
		
		cargoModelIndex_ = (byte)Random.Range(0, cargoPrefabs.Length);
		var instantiatedCargo = Object.Instantiate(cargoPrefabs[cargoModelIndex_], position, rotation, parent);
		instantiatedCargo.name = instantiatedCargo.name.Replace("(Clone)", $" {task.Job.ID}");
		return instantiatedCargo;
	}
}