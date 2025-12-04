using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.Awake))]
public class WarehouseMachineController_Awake_Patch
{
	private static bool Prefix(WarehouseMachineController __instance)
	{
		// AdvancedMachine.AllClonedMachineControllers won't work here yet
		var isClone = __instance.gameObject.name.Contains("(Clone)");
		if (isClone)
		{
			Main.Debug($"{nameof(WarehouseMachineController_Awake_Patch)} skipping");
		}
		return !isClone;
	}
}

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.OnEnable))]
public class WarehouseMachineController_OnEnable_Patch
{
	private static bool Prefix(WarehouseMachineController __instance)
	{
		// AdvancedMachine.AllClonedMachineControllers won't work first time
		var isClone = AdvancedMachine.AllClonedMachineControllers.Contains(__instance) ||
		              __instance.gameObject.name.Contains("(Clone)");
		if (!isClone) return true; 
		
		Main.Debug($"{nameof(WarehouseMachineController_OnEnable_Patch)} yes");

		if (!__instance.initialized)
		{
			__instance.StartCoroutine(__instance.InitLeverHJAF());
		}

		// don't start TrainInRangeCheck, that's in AdvancedMachine.OnEnable
		__instance.DisplayIdleText();
		
		return false;
	}
}

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

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.OnDestroy))]
public class WarehouseMachineController_OnDestroy_Patch 
{
	private static void Prefix(WarehouseMachineController __instance)
	{
		AdvancedMachine.AllClonedMachineControllers.Remove(__instance);
	}
}

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.ActivateExternally))]
public class WarehouseMachineController_ActivateExternally_Patch 
{
	private static bool Prefix(WarehouseMachineController __instance)
	{
		return !AdvancedMachine.AllClonedMachineControllers.Contains(__instance);

		//todo implement? what does it do?
	}
}
