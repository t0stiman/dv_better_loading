using HarmonyLib;

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