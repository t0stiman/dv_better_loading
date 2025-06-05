using DV.ThingTypes;
using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(WarehouseMachineController))]
[HarmonyPatch(nameof(WarehouseMachineController.StartLoadSequence))]
public class WarehouseMachineController_StartLoadSequence_Patch 
{
	private static bool Prefix(ref WarehouseMachineController __instance)
	{
		//TODO dit is niet goed
		// if(!__instance.supportedCargoTypes.Contains(CargoType.Coal)) return true;
		
		if (__instance.loadUnloadCoro != null || __instance.activateExternallyCoro != null)
		{
			return false;
		}
		
		Main.Log("hello there");
		
		CoalLoader.Instance.StartLoadingUnloading(__instance, true);

		return false;
	}
}

//TODO ActivateExternallyCoro