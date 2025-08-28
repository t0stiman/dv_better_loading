using DV.Logic.Job;
using HarmonyLib;

namespace better_loading.Patches;

// default UpdateTaskState does not support partial loading / unloading
[HarmonyPatch(typeof(WarehouseTask))]
[HarmonyPatch(nameof(WarehouseTask.UpdateTaskState))]
public class WarehouseTask_UpdateTaskState_Patch
{
	private static bool Prefix(WarehouseTask __instance, ref TaskState __result)
	{
		if (!BulkMachine.AllWarehouseMachinesWithBulk.Contains(__instance.warehouseMachine)) return true;
		
		__instance.readyForMachine = true;
		
		foreach (var car in __instance.cars)
		{
			switch (__instance.warehouseTaskType)
			{
				//loading in progress
				case WarehouseTaskType.Loading when
					!car.IsFull():
					
					__instance.SetState(TaskState.InProgress);
					__result = __instance.state;
					return false;
				
				//unloading in progress
				case WarehouseTaskType.Unloading when
					car.LoadedCargoAmount > 0.0:
					
					__instance.SetState(TaskState.InProgress);
					__result = __instance.state;
					return false;
			}
		}
		
		__instance.SetState(TaskState.Done);
		__result = __instance.state;
		return false;
	}
}