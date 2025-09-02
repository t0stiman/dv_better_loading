using System.Linq;
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

		if(IndustryBuildingInfo.TryGetInfo(stationID, out var buildingInfo))
		{
			CreateBulkMachine(__instance, buildingInfo);
		}
		else if(stationID == "HB")
		{
			CreateContainerMachine(__instance);
		}
		else
		{
			Main.Debug($"Skipping station {stationID}");
		}
	}
	
	private static void CreateBulkMachine(WarehouseMachineController machineController, IndustryBuildingInfo industryBuildingInfo)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(BulkMachine.IsCargoTypeSupported).ToArray();
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
	
	private static void CreateContainerMachine(WarehouseMachineController machineController)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(ContainerMachine.IsInShippingContainer).ToArray();
		if(cargoTypes.Length == 0) return;
		
		var model = machineController.transform.FindChildByName("WarehouseMachine model");
		
		var copy = Object.Instantiate(
			machineController.gameObject,
			machineController.transform.position + model.forward * -2,
			machineController.transform.rotation,
			machineController.transform.parent
		);
		
		copy.name = machineController.gameObject.name.Replace("(Clone)", "").Replace("Warehouse", "Container");
		
		var containerMachine = copy.AddComponent<ContainerMachine>();
		var clonedMachineController = copy.GetComponent<WarehouseMachineController>();
		containerMachine.PreStart(machineController, clonedMachineController, cargoTypes);
		
		Object.Destroy(clonedMachineController);
	}
}

