namespace better_loading;

public class CraneInfo
{
	public readonly Utilities.MinMax base_minmax; //todo implement crane constraints or remove this
	public static readonly Utilities.MinMax cab_minmax = new(-16, 19.5f);
	public readonly bool PlaceContainersAtLongSideOfCrane;
	public readonly string Path;

	private CraneInfo(Utilities.MinMax base_minmax_, 
		bool placeContainersAtLongSideOfCrane,
		string path)
	{
		base_minmax = base_minmax_;
		PlaceContainersAtLongSideOfCrane = placeContainersAtLongSideOfCrane;
		Path = path;
	}

	private static readonly CraneInfo GF = new(
		new Utilities.MinMax(-85.5f, 71),
		true,
		"Far__x12_z10_LFS/Far_SilosCranes/Portal_Crane"
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