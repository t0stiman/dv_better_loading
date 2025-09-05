using DV.Logic.Job;
using UnityEngine;

namespace better_loading;

public class ShippingContainer
{
	//the visible game object of the container
	public readonly GameObject gameObject;
	//the car the container will be loaded onto
	public readonly Car car;
	//the index of the cargo model prefab in CargoType_v2.TrainCargoToCargoPrefabs
	public readonly byte cargoModelIndex;
	
	public readonly Vector3 roofOffset;
	public readonly Vector3 bottomOffset;

	public ShippingContainer(GameObject gameObject_, Car Car, byte CargoModelIndex)
	{
		gameObject = gameObject_;
		car = Car;
		cargoModelIndex = CargoModelIndex;

		var collider = gameObject_.GetComponentInChildren<Collider>(false);

		{
			var highUp = gameObject_.transform.position + Vector3.up * 99f;
			var topOfContainer = collider.ClosestPoint(highUp);
			roofOffset = topOfContainer - gameObject_.transform.position;
		}

		{
			var lowDown = gameObject_.transform.position - Vector3.up * 99f;
			var bottomOfContainer = collider.ClosestPoint(lowDown);
			bottomOffset = bottomOfContainer - gameObject_.transform.position;
		}
	}
}