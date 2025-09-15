using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace better_loading.Patches;

[HarmonyPatch(typeof(TrainCar))]
[HarmonyPatch(nameof(TrainCar.Awake))]
public class TrainCar_Awake_Patch
{
	// positions of the unloading shutes on the bottom of the train car
	private static readonly Dictionary<string, Vector3[]> TrainCarIDToShutePositions = new()
	{
		{ "Hopper", new[]
			{
				new Vector3(0, 0.3f, -3.7f),
				new Vector3(0, 0.3f, -1.24f),
				new Vector3(0, 0.3f, 1.24f),
				new Vector3(0, 0.3f, 3.7f),
			}
		}
	};
	
	private static void Postfix(TrainCar __instance)
	{
		__instance.gameObject.AddComponent<TrainCarV2Debug>();
		SetupBulkUnloadEffects(__instance);
	}

	private static void SetupBulkUnloadEffects(TrainCar trainCar)
	{
		if(!BulkMachine.IsCarTypeSupported(trainCar.carLivery.parentType)) return;
		
		var unloadEffects = trainCar.gameObject.AddComponent<ShuteEffectsManager>();

		if (!TrainCarIDToShutePositions.TryGetValue(trainCar.carLivery.parentType.id, out Vector3[] positions))
		{
			positions = new[] { Vector3.zero };
		}
		
		unloadEffects.CreateShutes(positions);
		unloadEffects.ScheduleInitializeEffects();
	}
}