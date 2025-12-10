namespace better_loading;

public class CraneInfo
{
	public readonly float ContainerAreaOffset;
	public readonly string Path;

	private CraneInfo(
		float containerAreaOffset,
		string path
		// string yard = "",
		// string track = ""
		)
	{
		ContainerAreaOffset = containerAreaOffset;
		Path = path;
	}

	// Get a CraneInfo for the station with ID stationID
	public static bool TryGetInfo(string stationID, out CraneInfo craneInfo)
	{
		switch (stationID)
		{
			case "CW":
				craneInfo = new CraneInfo(
					0,
					"Far__x1_z5_LFS/Far_SilosCranes/Portal_Crane"
				);
				return true;
			
			case "FF":
				craneInfo = new CraneInfo(
					-18.5f,
					"Far__x9_z13_LFS/Far_SilosCranes/Portal_Crane"
				);
				return true;
			
			case "GF":
				craneInfo = new CraneInfo(
					-18.5f,
					"Far__x12_z10_LFS/Far_SilosCranes/Portal_Crane"
				);
				return true;
			
			//todo HB has 3 cranes
			case "HB":
				craneInfo = new CraneInfo(
					-17,
					"Far__x12_z2_LFS/Far_Cargo Bay/SilosCranes (1)/Portal_Crane (6)"
				);
				return true;
			
			case "MF":
				craneInfo = new CraneInfo(
					-18.5f,
					"Far__x2_z10_LFS/Far_Station_MachineFactory/SilosCranes/Portal_Crane"
				);
				return true;

			default:
				craneInfo = null;
				return false;
		}
	}
}