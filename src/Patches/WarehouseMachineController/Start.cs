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
		//avoid recursion
		if (AdvancedMachine.AllClonedMachineControllers.Contains(__instance))
		{
			__instance.warehouseMachine = null;
			return;
		}

		var stationID = StationController.allStations
			.First(station => station.warehouseMachineControllers.Contains(__instance)).stationInfo.YardID;

		if (BulkLoaderInfo.TryGetInfo(stationID, out var loaderInfo))
		{
			CreateBulkMachine(__instance, loaderInfo);
		}
		if (CraneInfo.TryGetInfo(stationID, out var craneInfo))
		{
			CreateContainerMachine(__instance, craneInfo);
		}
	}

	private static void CreateBulkMachine(
		WarehouseMachineController machineController,
		BulkLoaderInfo loaderInfo
	)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(BulkMachine.IsCargoTypeSupported).ToArray();
		if (cargoTypes.Length == 0) return;

		var model = machineController.transform.FindChildByName("WarehouseMachine model");

		var copy = Object.Instantiate(
			machineController.gameObject,
			machineController.transform.position + model.forward * -2,
			machineController.transform.rotation,
			machineController.transform.parent
		);

		copy.name = machineController.gameObject.name.Replace("(Clone)", "").Replace("Warehouse", "Bulk");

		BulkMachine bulkMachine = loaderInfo.isLoader ? copy.AddComponent<BulkLoader>() : copy.AddComponent<BulkUnloader>();
		var clonedMachineController = copy.GetComponent<WarehouseMachineController>();
		if (!clonedMachineController)
		{
			Main.Error("Unable to get clonedMachineController");
		}

		bulkMachine.LoaderInfo = loaderInfo;
		bulkMachine.PreStart(machineController, clonedMachineController, cargoTypes);
	}

	private static void CreateContainerMachine(WarehouseMachineController machineController, CraneInfo craneInfo)
	{
		var cargoTypes = machineController.supportedCargoTypes.Where(ContainerMachine.IsInShippingContainer).ToArray();
		if (cargoTypes.Length == 0) return;

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
		if (!clonedMachineController)
		{
			Main.Error("Unable to get clonedMachineController");
		}
		containerMachine.PreStart(machineController, clonedMachineController, cargoTypes, craneInfo);
	}
}
