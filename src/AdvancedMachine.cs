using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using TMPro;
using UnityEngine;

namespace better_loading;

public abstract class AdvancedMachine: MonoBehaviour
{
	protected static readonly List<AdvancedMachine> AllAdvancedMachines = new();
	public static bool TryGetAdvancedMachine(WarehouseMachine aMachine,
		out AdvancedMachine advancedMachine)
	{
		// using the == operator on WarehouseMachine does not work, so we compare the track IDs
		advancedMachine = AllAdvancedMachines.FirstOrDefault(AM =>
			AM.VanillaMachineController.warehouseMachine.WarehouseTrack.ID == aMachine.WarehouseTrack.ID);
		return advancedMachine is not null;
	}
	
	public static readonly List<WarehouseMachineController> AllClonedMachineControllers = new();
	
	protected WarehouseMachineController VanillaMachineController;
	protected WarehouseMachineController clonedMachineController;
	
	protected Coroutine loadUnloadCoroutine;
	protected CargoType[] cargoTypes;
	
	#region setup

	public void PreStart(WarehouseMachineController vanillaMachineController,
		WarehouseMachineController clonedMachineController_,
		CargoType[] cargoTypes_)
	{
		VanillaMachineController = vanillaMachineController;
		AllAdvancedMachines.Add(this);

		clonedMachineController = clonedMachineController_;
		AllClonedMachineControllers.Add(clonedMachineController_);
		
		cargoTypes = cargoTypes_;
		
		FilterCargoOnScreen(vanillaMachineController, cargoTypes_, true);
		FilterCargoOnScreen(clonedMachineController_, cargoTypes_, false);
	}
	
	// hide == true -> Hide these cargo types from the screen of the warehouse machine
	// hide == false -> Show only these cargo types on the machine, hide the rest
	protected static void FilterCargoOnScreen(WarehouseMachineController machineController, CargoType[] cargoTypes, bool hide)
	{
		machineController.CurrentTextPresets.Clear();
		
		var stringBuilder = new StringBuilder();
		foreach (var cargoType in machineController.supportedCargoTypes)
		{
			if(hide && cargoTypes.Contains(cargoType)) continue;
			if(!hide && !cargoTypes.Contains(cargoType)) continue;
			
			stringBuilder.AppendLine(cargoType.ToV2().LocalizedName());
		}
		machineController.supportedCargoTypesText = stringBuilder.ToString();
		machineController.DisplayIdleText();
	}

	private void OnEnable()
	{
		OnEnable_();
	}
	
	//make it overrideable
	protected virtual void OnEnable_()
	{
		StartCoroutine(InitializeLeverCallback());
		StartCoroutine(TrainInRangeCheck());
	}
	
	private void OnDisable()
	{
		StopAllCoroutines();
		loadUnloadCoroutine = null;
	}
	
	// subscribe our callback function to RotaryStateChanged and unsubscribe the vanilla
	protected IEnumerator InitializeLeverCallback()
	{
		while (!clonedMachineController || !clonedMachineController.initialized)
		{
			yield return WaitFor.Seconds(0.5f);
		}

		var amplitudeChecker = clonedMachineController.GetComponentInChildren<RotaryAmplitudeChecker>();
		if (!amplitudeChecker)
		{
			Main.Error("amplitudeChecker is null");
			yield break;
		}
		amplitudeChecker.RotaryStateChanged += OnLeverPositionChange;
		amplitudeChecker.RotaryStateChanged -= clonedMachineController.OnLeverPositionChange;
	}
	
	protected void ChangeText(GameObject textTitleObject, string text)
	{
		var tmp = textTitleObject.GetComponent<TextMeshPro>();
		tmp.SetText(text);
	}
	
	#endregion

	private void OnDestroy()
	{
		AllAdvancedMachines.Remove(this);
	}

	protected abstract void OnLeverPositionChange(int positionState);

	public abstract bool IsSupportedCargoType(CargoType cargoType);
	
	protected IEnumerable<WarehouseTask> GetReadyTasks()
	{
		return VanillaMachineController.warehouseMachine.currentTasks.Where(task =>
			IsSupportedCargoType(task.cargoType) &&
			task.readyForMachine &&
			task.warehouseTaskType != WarehouseTaskType.None &&
			VanillaMachineController.warehouseMachine.CarsPresentOnWarehouseTrack(task.cars)
		);
	}
	
	protected bool AnyTrainToLoadPresentOnTrack()
	{
		return GetReadyTasks().Any(task => task.warehouseTaskType == WarehouseTaskType.Loading);
	}

	protected bool AnyTrainToUnloadPresentOnTrack()
	{
		return GetReadyTasks().Any(task => task.warehouseTaskType == WarehouseTaskType.Unloading);
	}
	
	//pretty much the same as WarehouseMachineController.TrainInRangeCheck
	private IEnumerator TrainInRangeCheck()
	{
		while (!clonedMachineController || !clonedMachineController.initialized)
		{
			yield return WaitFor.Seconds(WarehouseMachineController.TRAIN_IN_RANGE_CHECK_PERIOD);
		}
		
		bool aTrainWasPresent = false;
		clonedMachineController.ClearTrainInRangeText();
		
		while (true)
		{
			yield return WaitFor.Seconds(WarehouseMachineController.TRAIN_IN_RANGE_CHECK_PERIOD);
			var loadPresentOnTrack = AnyTrainToLoadPresentOnTrack();
			var unloadPresentOnTrack = AnyTrainToUnloadPresentOnTrack();
	
			var trainIsPresent = loadPresentOnTrack | unloadPresentOnTrack && loadUnloadCoroutine == null;
			if(trainIsPresent == aTrainWasPresent) continue;
	
			if (aTrainWasPresent)
			{
				clonedMachineController.ClearTrainInRangeText();
			}
			else
			{
				clonedMachineController.SetScreen(WarehouseMachineController.TextPreset.TrainInRange,
					extra: loadPresentOnTrack ? "whm/load_brackets" : "whm/unload_brackets");
			}
	
			aTrainWasPresent = trainIsPresent;
		}
	}
	
	protected void SetDisplayTitleText(string str)
	{
		clonedMachineController.displayTitleText.SetText(str);
	}
	
	protected void SetDisplayDescriptionText(string str)
	{
		clonedMachineController.displayText.SetText(str);
	}

	protected void SetScreen(
		WarehouseMachineController.TextPreset preset,
		bool isLoading = false,
		string jobId = null,
		Car car = null,
		CargoType_v2 cargoType = null,
		string extra = null)
	{
		clonedMachineController.SetScreen(preset, isLoading, jobId, car, cargoType, extra);
	}
}