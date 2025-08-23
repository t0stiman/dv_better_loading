using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.Start))]
public class WarehouseMachineController_Awake_Patch 
{
	private static void Postfix(WarehouseMachineController __instance)
	{
		var cargoType = __instance.supportedCargoTypes.FirstOrDefault(ct => ct.IsSupportedBulkType());
		if(cargoType == default) return;

		var model = __instance.transform.FindChildByName("WarehouseMachine model");
		
		var copy = Object.Instantiate(
			__instance.gameObject,
			__instance.transform.position + model.forward * -2,
			__instance.transform.rotation,
			__instance.transform.parent
		);
		
		copy.name = __instance.gameObject.name.Replace("(Clone)", "").Replace("Warehouse", "Bulk");
		
		var bulkLoader = copy.AddComponent<BulkLoader>();
		var clonedMachineController = copy.GetComponent<WarehouseMachineController>();
		bulkLoader.PreStart(__instance, clonedMachineController, cargoType);
		
		Object.Destroy(clonedMachineController);
	}
}
