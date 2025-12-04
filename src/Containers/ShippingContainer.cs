using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class ShippingContainer
{
	//the visible game object of the container
	public readonly GameObject containerObject;
	//the car the container will be loaded onto
	public readonly Car car;
	//the index of the cargo model prefab in CargoType_v2.TrainCargoToCargoPrefabs
	public readonly byte cargoModelIndex;
	//the task this cargo belongs to
	public readonly WarehouseTask task; //todo do we need this
	
	//this is used to determine where the crane needs to grab
	public readonly Vector3 roofOffset;
	//size of the object
	public readonly Vector3 size;

	public ShippingContainer(Car car_, WarehouseTask task_, Vector3 position, Quaternion rotation, Transform parent)
	{
		car = car_;
		task = task_;
		containerObject = CreateContainerModel(car.TrainCar(), task_.cargoType, position, rotation, parent, out cargoModelIndex);
		
		size = MeasureSize(containerObject);
		
		//fix rotation
		containerObject.transform.Rotate(Vector3.up * 90f, Space.Self);
		
		//place on ground
		containerObject.transform.Translate(Vector3.up * -1.25f);
		
		{
			var highUp = containerObject.transform.position + Vector3.up * 99f;
			var collider = containerObject.GetComponentInChildren<Collider>(false);
			var topOfContainer = collider.ClosestPoint(highUp);
			roofOffset = topOfContainer - containerObject.transform.position;
		}

		ActivateColliders(containerObject);
	}

	private void ActivateColliders(GameObject gameObject)
	{
		var colliderRoot = gameObject.FindChildByName("[colliders]")?.transform;
		if(!colliderRoot) return;

		for (int i = 0; i < colliderRoot.childCount; i++)
		{
			colliderRoot.GetChild(i).gameObject.SetActive(true);
		}
	}

	private Vector3 MeasureSize(GameObject obj)
	{
		const string collisionColliderName = "[collision]";
		var collider = obj.FindChildByName(collisionColliderName)?.GetComponentInChildren<BoxCollider>(true);
		if (collider is null)
		{
			Main.Warning($"Could not find collider named {collisionColliderName} on {obj.name}");
			collider = obj.GetComponentInChildren<BoxCollider>(true);
		}
		return collider.size; //assumes the object has not been scaled
	}

	private static GameObject CreateContainerModel(TrainCar trainCar, CargoType cargoType, Vector3 position, Quaternion rotation, Transform parent, out byte cargoModelIndex_)
	{
		var trainCarType = trainCar.carLivery.parentType;
		var cargoPrefabs = cargoType.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(ContainerMachine)}.{nameof(CreateContainerModel)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {cargoType}");
			cargoModelIndex_ = 0;
			return null;
		}
		
		cargoModelIndex_ = (byte)Random.Range(0, cargoPrefabs.Length);
		return Object.Instantiate(cargoPrefabs[cargoModelIndex_], position, rotation, parent);
	}
}