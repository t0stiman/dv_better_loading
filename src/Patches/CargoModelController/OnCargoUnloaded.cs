using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// visual cargo unloading (bulk cargo)
/// </summary>
[HarmonyPatch(typeof(CargoModelController))]
[HarmonyPatch(nameof(CargoModelController.OnCargoUnloaded))]
public class CargoModelController_OnCargoUnloaded_Patch
{
	private static bool Prefix(CargoModelController __instance)
	{
		var cargoType = __instance.trainCar.logicCar.LastUnloadedCargoType;
		if(!BulkMachine.IsCargoTypeSupported(cargoType)) return true;
		
		var trainCar = __instance.trainCar;
		
		if (trainCar.IsEmpty())
		{
			if (trainCar.IsCargoLoadedUnloadedByMachine)
			{
				CMCPatchesShared.PlayCarFullEmptySound("empty");
			}

			DestroyCargoModel(__instance);
		}
		else
		{
			CMCPatchesShared.UpdateCargoLevel(__instance, cargoType, false);
		}

		return false;
	}

	private static void DestroyCargoModel(CargoModelController __instance)
	{
		__instance.currentCargoModelIndex = null;
		
		if(__instance.currentCargoModel == null) return;
		
		__instance.DestroyCurrentCargoModel();
		__instance.trainColliders.SetupCargo(null);
	}
}