using DV.ThingTypes;
using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(LocoResourceModule))]
[HarmonyPatch(nameof(LocoResourceModule.Start))]
public class LocoResourceModule_Start_Patch
{
	private static void Postfix(LocoResourceModule __instance)
	{
		if(__instance.resourceType != ResourceType.Coal) return;
		ShuteEffectsManager.SetTenderCoalModule(__instance);
	}
}