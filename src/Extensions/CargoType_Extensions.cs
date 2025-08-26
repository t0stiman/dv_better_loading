using System.Collections.Generic;
using System.Linq;
using DV.Localization;
using DV.ThingTypes;

namespace better_loading;

public static class CargoType_Extensions
{
	private static IReadOnlyList<CargoType> supportedBulkTypes { get; } = new List<CargoType>
	{
		CargoType.Coal,
		CargoType.IronOre,
		CargoType.Wheat
		//ballast
		//sunflower
		//flour
		//corn
	};
	
	public static bool IsSupportedBulkType(this CargoType cargoType)
	{
		return supportedBulkTypes.Contains(cargoType);
	}
	
	public static string LocalizedName(this CargoType_v2 cargoType)
	{
		return LocalizationAPI.L(cargoType.localizationKeyFull);
	}
}