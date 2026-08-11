using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.OnDestroy))]
public class WarehouseMachineController_OnDestroy_Patch 
{
	private static void Prefix(WarehouseMachineController __instance)
	{
		AdvancedMachine.AllClonedMachineControllers.Remove(__instance);
	}
}