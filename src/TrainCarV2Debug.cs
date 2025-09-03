using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public class TrainCarV2Debug: MonoBehaviour
{
	private TrainCar trainCar;
	private CargoType previousLoadedCargo;
	
	public TrainCarType_v2 CarTypeV2;
	public TrainCarLivery CarLivery;
	public CargoType_v2 CargoTypeV2;
	
	private void Start()
	{
		trainCar = gameObject.GetComponent<TrainCar>();
		CarLivery = trainCar.carType.ToV2();
		CarTypeV2 = CarLivery.parentType;
		previousLoadedCargo = CargoType.None;
	}

	private void Update()
	{
		if(trainCar.LoadedCargo == previousLoadedCargo) return;
		CargoTypeV2 = trainCar.LoadedCargo.ToV2();
		previousLoadedCargo = trainCar.LoadedCargo;
	}
}