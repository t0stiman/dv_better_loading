using System.Linq;
using System.Text;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.Start))]
public class WarehouseMachineController_Start_Patch 
{
	private static void Postfix(WarehouseMachineController __instance)
	{
		CreateBulkMachine(__instance);
		ChangeSupportedText(__instance);
	}

	// create the bulkMachine component
	private static void CreateBulkMachine(WarehouseMachineController machineController)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(ct => ct.IsSupportedBulkType()).ToArray();
		if(cargoTypes.Length == 0) return;

		var model = machineController.transform.FindChildByName("WarehouseMachine model");
		
		var copy = Object.Instantiate(
			machineController.gameObject,
			machineController.transform.position + model.forward * -2,
			machineController.transform.rotation,
			machineController.transform.parent
		);
		
		copy.name = machineController.gameObject.name.Replace("(Clone)", "").Replace("Warehouse", "Bulk");
		
		var bulkMachine = copy.AddComponent<BulkMachine>();
		var clonedMachineController = copy.GetComponent<WarehouseMachineController>();
		bulkMachine.PreStart(machineController, clonedMachineController, cargoTypes);
		
		Object.Destroy(clonedMachineController);
	}
	
	// Hide the bulk cargo from the screen
	private static void ChangeSupportedText(WarehouseMachineController machineController)
	{
		var stringBuilder = new StringBuilder();
		foreach (var cargoType in machineController.supportedCargoTypes)
		{
			if(cargoType.IsSupportedBulkType()) continue;
			stringBuilder.AppendLine(cargoType.ToV2().LocalizedName());
		}
		machineController.supportedCargoTypesText = stringBuilder.ToString();
		machineController.DisplayIdleText();
	}
}

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.OnDestroy))]
public class WarehouseMachineController_OnDestroy_Patch
{
	private static void Prefix(WarehouseMachineController __instance)
	{
		BulkMachine.AllWarehouseMachinesWithBulk.Remove(__instance.warehouseMachine);
	}
}


