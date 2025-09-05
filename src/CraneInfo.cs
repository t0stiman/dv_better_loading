namespace better_loading;

public class CraneInfo
{
	public readonly Utilities.MinMax base_minmax; //todo dynamic
	public static readonly Utilities.MinMax cab_minmax = new(-16, 19.5f);
	public readonly bool PlaceContainersAtLongSideOfCrane;

	private CraneInfo(Utilities.MinMax base_minmax_, bool placeContainersAtLongSideOfCrane)
	{
		base_minmax = base_minmax_;
		PlaceContainersAtLongSideOfCrane = placeContainersAtLongSideOfCrane;
	}

	private static readonly CraneInfo GF = new(
		new Utilities.MinMax(-85.5f, 71),
		true
	);

	public static bool TryGetInfo(string stationID, out CraneInfo craneInfo)
	{
		switch (stationID)
		{
			case "GF":
				craneInfo = GF;
				return true;
			default:
				craneInfo = null;
				return false;
		}
	}
}