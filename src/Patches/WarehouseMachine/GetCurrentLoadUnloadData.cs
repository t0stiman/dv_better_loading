using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// Prevent (un)loading bulk cargo with the default machine
/// </summary>
[HarmonyPatch(typeof(WarehouseMachine))]
[HarmonyPatch(nameof(WarehouseMachine.GetCurrentLoadUnloadData))]
public class WarehouseMachine_GetCurrentLoadUnloadData_Patch
{
	private static void Postfix(WarehouseMachine __instance, ref List<WarehouseMachine.WarehouseLoadUnloadDataPerJob> __result)
	{
		if (!AdvancedMachine.TryGetAdvancedMachine(__instance, out var advancedMachine)) return;
		
		// remove bulk cargo jobs
		__result = __result
			.Where(dataPerJob => !dataPerJob.tasksAvailableToProcess.Any(task => advancedMachine.IsSupportedCargoType(task.cargoType)))
			.ToList();
	}
}