using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.ActivateExternally))]
public class WarehouseMachineController_ActivateExternally_Patch 
{
	private static bool Prefix(WarehouseMachineController __instance)
	{
		return !AdvancedMachine.AllClonedMachineControllers.Contains(__instance);

		//todo implement? what does ActivateExternally do?
	}
}