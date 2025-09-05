using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace better_loading;

public class ContainerArea: MonoBehaviour
{
	private readonly List<ShippingContainer> containers = new();
	private Quaternion containersRotation;

	private void Start()
	{
		containersRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
	}

	public void SpawnContainers(List<Car> cars, CargoType cargoType)
	{
		foreach (var car in cars)
		{
			var slot = containers.Count; //todo
			var position = transform.position + slot * 14f * transform.right;
			var cargo = CreateContainerModel(car.TrainCar(), cargoType, position, containersRotation, out var cargoModelIndex);
			
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