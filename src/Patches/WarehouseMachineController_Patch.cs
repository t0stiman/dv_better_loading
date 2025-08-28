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
		var stationID = StationController.allStations.First(station => station.warehouseMachineControllers.Contains(__instance)).stationInfo.YardID;

		if(!IndustryBuildingInfo.TryGetInfo(stationID, out var buildingInfo))
		{
			Main.Debug($"Skipping station {stationID}");
			return;
		}
		
		CreateBulkMachine(__instance, buildingInfo);
		ChangeSupportedText(__instance);
	}

	// create the bulkMachine component
	private static void CreateBulkMachine(WarehouseMachineController machineController, IndustryBuildingInfo industryBuildingInfo)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(ct => BulkMachine.IsSupportedBulkType(ct)).ToArray();
		if(cargoTypes.Length == 0) return;

		var model = machineController.transform.FindChildByName("WarehouseMachine model");
		
		var copy = Object.Instantiate(
			machineController.gameObject,
			machineController.transform.position + model.forward * 2,
			machineController.transform.rotation,
			machineController.transform.parent
		);
		
		copy.name = machineController.gameObject.name.Replace("(Clone)", "").Replace("Warehouse", "Bulk");
		
		var bulkMachine = copy.AddComponent<BulkMachine>();
		var clonedMachineController = copy.GetComponent<WarehouseMachineController>();
		bulkMachine.PreStart(machineController, clonedMachineController, cargoTypes, industryBuildingInfo);
		
		Object.Destroy(clonedMachineController);
	}
	
	// Hide the bulk cargo from the screen
	private static void ChangeSupportedText(WarehouseMachineController machineController)
	{
		machineController.CurrentTextPresets.Clear();
		
		var stringBuilder = new StringBuilder();
		foreach (var cargoType in machineController.supportedCargoTypes)
		{
			if(BulkMachine.IsSupportedBulkType(cargoType)) continue;
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


