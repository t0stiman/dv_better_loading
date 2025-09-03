using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace better_loading;

public class ContainerArea
{
	private readonly List<ShippingContainer> containers = new();

	public readonly Vector3 center;
	private readonly Quaternion rotation;
	private readonly Vector3 forward;

	public ContainerArea(Vector3 center_, Quaternion rotation_, Vector3 forward_)
	{
		center = center_;
		rotation = rotation_;
		forward = forward_; //can probably be calculated from rotation be I can't be bothered
	}
	
	public void SpawnContainers(List<Car> cars, CargoType cargoType)
	{
		foreach (var car in cars)
		{
			var slot = containers.Count; //todo
			var position = center + slot * 14f * forward;
			var cargo = CreateContainerModel(car.TrainCar(), cargoType, position, rotation, out var cargoModelIndex);
			
			containers.Add(new ShippingContainer(cargo, car, cargoModelIndex));
		}
	}
	
	private static GameObject CreateContainerModel(TrainCar trainCar, CargoType cargoType, Vector3 position, Quaternion rotation, out byte cargoModelIndex)
	{
		var trainCarType = trainCar.carLivery.parentType;
		var cargoPrefabs = cargoType.ToV2().GetCargoPrefabsForCarType(trainCarType);

		if (cargoPrefabs == null || cargoPrefabs.Length == 0)
		{
			Main.Error($"{nameof(ContainerMachine)}.{nameof(CreateContainerModel)}: no cargo prefabs found for train car type {trainCarType.name}, cargo {cargoType}");
			cargoModelIndex = 0;
			return null;
		}
		
		cargoModelIndex = (byte)Random.Range(0, cargoPrefabs.Length);
		return Object.Instantiate(cargoPrefabs[cargoModelIndex], position, rotation);
	}

	public ShippingContainer GetContainerObject(Car car)
	{
		return containers.First(c => c.car.ID == car.ID);
	}

	public void Destroy(ShippingContainer aShippingContainer)
	{
		Object.Destroy(aShippingContainer.gameObject);
		containers.Remove(aShippingContainer);
	}
}