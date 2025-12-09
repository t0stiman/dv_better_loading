using DV.Localization;
using DV.ThingTypes;

namespace better_loading;

public static class CargoType_Extensions
{
	public static string GetLocalizedName(this CargoType_v2 cargoType)
	{
		return LocalizationAPI.L(cargoType.localizationKeyFull);
	}
}