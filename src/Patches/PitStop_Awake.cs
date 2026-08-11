using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// Copy the AudioClip reference
/// </summary>
[HarmonyPatch(typeof(PitStop))]
[HarmonyPatch(nameof(PitStop.Awake))]
public class PitStop_Awake_Patch
{
	private static void Postfix(PitStop __instance)
	{
		if(CMCPatchesShared.ChingSound) return;
		CMCPatchesShared.ChingSound = __instance.chingSound;
	}
}