using DV.Logic.Job;
using UnityEngine;

namespace better_loading;

public record struct ShippingContainer(GameObject gameObject, Car car, byte cargoModelIndex)
{
	//the visible game object of the container
	public readonly GameObject gameObject = gameObject;
	//the car the container will be loaded onto
	public readonly Car car = car;
	//the index of the cargo model prefab in CargoType_v2.TrainCargoToCargoPrefabs
	public readonly byte cargoModelIndex = cargoModelIndex;
}