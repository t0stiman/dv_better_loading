using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.OnEnable))]
public class WarehouseMachineController_OnEnable_Patch
{
	private static bool Prefix(WarehouseMachineController __instance)
	{
		// AdvancedMachine.AllClonedMachineControllers won't work first time
		var isAdvancedMachine = AdvancedMachine.AllClonedMachineControllers.Contains(__instance) ||
		                        __instance.gameObject.name.Contains("(Clone)");
		if (!isAdvancedMachine) return true;

		if (!__instance.initialized)
		{
			__instance.StartCoroutine(__instance.InitLeverHJAF());
		}

		// don't start TrainInRangeCheck, that's in AdvancedMachine.OnEnable
		__instance.DisplayIdleText();
		
		return false;
	}
}