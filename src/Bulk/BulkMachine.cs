using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace better_loading;

public abstract class BulkMachine: AdvancedMachine
{
	//kg/s
	protected static readonly Dictionary<CargoType, float> loadUnloadSpeed = new()
	{
		{ CargoType.Coal, 56000 / 45f },
		{ CargoType.IronOre, 62000 / 20f }
		//todo
		//wheat
		//ballast?
		//sunflower
		//flour
		//corn
	};
	
	public static bool IsCargoTypeSupported(CargoType cargoType)
	{
		return loadUnloadSpeed.Keys.Contains(cargoType);
	}
	
	// polymorphism
	public override bool IsSupportedCargoType(CargoType cargoType)
	{
		return IsCargoTypeSupported(cargoType);
	}

	// returns true if the carType can load any bulk cargo type
	public static bool IsCarTypeSupported(TrainCarType_v2 carType)
	{
		return loadUnloadSpeed.Keys.Any(cargoType => cargoType.ToV2().IsLoadableOnCarType(carType));;
	}
	
	protected bool initialized = false;
	protected bool coroutineIsRunning = false;
	public BulkLoaderInfo LoaderInfo;
	
	protected bool timeWasFlowing;
	
	protected GameObject overlapBoxObject;

	#region setup
	
	protected override void OnEnable_()
	{
		StartCoroutine(InitializeLeverCallback());
		// no TrainInRangeCheck
	}
	
	protected void Start()
	{
		SetupTexts();
	}
	
	protected abstract void Initialize();

	protected abstract void SetupTexts();
	protected void SetupTexts2(string maybeUn)
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), $"Bulk cargo\n{maybeUn}loader");
		ChangeText(gameObject.FindChildByName("TextUnload"), "Stop");
		ChangeText(gameObject.FindChildByName("TextLoad"), "Start");
	}
	
	#endregion
	
	protected override void OnLeverPositionChange(int positionState)
	{
		switch (positionState)
		{
			case -1:
				EnableMachine();
				break;
			case 1:
				DisableMachine();
				break;
		}
	}

	protected void EnableMachine()
	{
		if (coroutineIsRunning) return;
		
		Initialize();
		loadUnloadCoroutine = StartCoroutine(LoadingUnloading());
	}

	protected abstract void DisableMachine();

	protected abstract void SetEnabledText();
	
	#region update

	protected abstract IEnumerator LoadingUnloading();

	protected bool TryGetTask(TrainCar aCar, out WarehouseTask task)
	{
		if (aCar.logicCar == null)
		{
			task = null;
			return false;
		}
		
		foreach (var aTask in VanillaMachineController.warehouseMachine.currentTasks)
		{
			if(!aTask.cars.Contains(aCar.logicCar)) continue;
			
			task = aTask;
			return true;
		}

		task = null;
		return false;
	}

	#endregion
}