using HarmonyLib;

namespace better_loading.Patches;

[HarmonyPatch(typeof(TrainCar))]
[HarmonyPatch(nameof(TrainCar.Awake))]
public class TrainCar_Awake_Patch
{
	private static void Postfix(TrainCar __instance)
	{
		__instance.gameObject.AddComponent<TrainCarV2Debug>();
	}
}