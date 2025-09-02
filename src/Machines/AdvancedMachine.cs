using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using TMPro;
using UnityEngine;

namespace better_loading;

public abstract class AdvancedMachine: MonoBehaviour
{
	public static readonly List<AdvancedMachine> AllAdvancedMachines = new();
	
	public WarehouseMachineController MachineController;
	
	protected Coroutine loadUnloadCoro;
	
	protected CargoType[] cargoTypes;
	protected CargoType_v2[] cargoTypesV2;
	
	//text
	protected TextMeshPro displayTitleText;
	protected TextMeshPro displayText;
	
	#region setup

	public void PreStart(
		WarehouseMachineController vanillaMachineController, 
		WarehouseMachineController clonedMachineController, 
		CargoType[] cargoTypes_)
	{
		MachineController = vanillaMachineController;
		AllAdvancedMachines.Add(this);
		
		cargoTypes = cargoTypes_;
		cargoTypesV2 = cargoTypes_.Select(v1 => v1.ToV2()).ToArray();
		
		displayTitleText = clonedMachineController.displayTitleText;
		displayText = clonedMachineController.displayText;
		
		HideCargoFromMachine(vanillaMachineController, cargoTypes_);
	}
	
	// Hide these cargo types from the screen of the vanilla warehouse machine
	protected static void HideCargoFromMachine(WarehouseMachineController machineController, CargoType[] cargoTypesToHide)
	{
		machineController.CurrentTextPresets.Clear();
		
		var stringBuilder = new StringBuilder();
		foreach (var cargoType in machineController.supportedCargoTypes)
		{
			if(cargoTypesToHide.Contains(cargoType)) continue;
			stringBuilder.AppendLine(cargoType.ToV2().LocalizedName());
		}
		machineController.supportedCargoTypesText = stringBuilder.ToString();
		machineController.DisplayIdleText();
	}

	protected void Start_()
	{
		StartCoroutine(InitLeverHJAF());
	}
	
	// WarehouseMachineController.InitLeverHJAF
	private IEnumerator InitLeverHJAF()
	{
		HingeJointAngleFix jointFix;
		while ((jointFix = gameObject.GetComponentInChildren<HingeJointAngleFix>()) == null)
			yield return WaitFor.Seconds(0.2f);
		var amplitudeChecker = jointFix.gameObject.AddComponent<RotaryAmplitudeChecker>();
		var hingeJoint = jointFix.gameObject.GetComponent<HingeJoint>();
		var amplitude = hingeJoint.limits.max - hingeJoint.limits.min;
		amplitudeChecker.checkThreshold = amplitude * 0.2f;
		amplitudeChecker.checkPeriod = 0.1f;
		amplitudeChecker.RotaryStateChanged += OnLeverPositionChange;
	}
	
	protected void SetupTexts(string titleText)
	{
		ChangeText(gameObject.FindChildByName("TextTitle"), titleText);
		ChangeText(gameObject.FindChildByName("TextUnload"), "Stop");
		ChangeText(gameObject.FindChildByName("TextLoad"), "Start");

		DisplayIdleText();
	}
	
	private void ChangeText(GameObject textTitleObject, string text)
	{
		var tmp = textTitleObject.GetComponent<TextMeshPro>();
		tmp.SetText(text);
	}
	
	#endregion

	private void OnDestroy()
	{
		AllAdvancedMachines.Remove(this);
	}

	private void OnLeverPositionChange(int positionState)
	{
		switch (positionState)
		{
			case -1:
				StartLoadingSequence();
				break;
			case 1:
				StopLoadingSequence();
				break;
		}
	}

	protected abstract void StartLoadingSequence();
	protected abstract void StopLoadingSequence();
	
	protected virtual void DisplayIdleText()
	{
		displayTitleText.SetText($"This machine loads: {cargoTypesV2.Select(cargoType => cargoType.LocalizedName()).Join(", ")}");
		displayText.SetText("Move the handle to start");
	}

	public abstract bool IsSupportedCargoType(CargoType cargoType);
}