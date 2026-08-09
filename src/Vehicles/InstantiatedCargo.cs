using UnityEngine;

namespace better_loading;

public class InstantiatedCargo
{
	public readonly GameObject gameObject;
	public readonly byte cargoModelIndex;
	public readonly TrainCar trainCar;

	public InstantiatedCargo(GameObject gameObject, byte cargoModelIndex, TrainCar trainCar)
	{
		this.gameObject = gameObject;
		this.cargoModelIndex = cargoModelIndex;
		this.trainCar = trainCar;
	}
}