using UnityEngine;

namespace better_loading;

public class BulkLoaderInfo
{
	// loading or unloading?
	public readonly bool isLoader;
	public IndustryBuildingInfo building;
	
	private static readonly IndustryBuildingInfo coalLoader = new ("Industry_Coal", new Vector3(-85.976f, 8.554f, 1.999f));
	private static readonly IndustryBuildingInfo coalUnloader = new("Railway_PlatformWide_2", Vector3.zero);
	private static readonly IndustryBuildingInfo ironLoader = new ("Industry_Iron", new Vector3(-20.5f, 7.5f, -6.02f));

	private BulkLoaderInfo(bool _isLoader, IndustryBuildingInfo buildingInfo)
	{
		isLoader = _isLoader;
		building = buildingInfo;
	}
	
	public static bool TryGetInfo(string stationID, out BulkLoaderInfo info)
	{
		switch (stationID)
		{
			case "CME":
			case "CMS":
				info = new BulkLoaderInfo(true, coalLoader);
				return true;
			case "IME":
			case "IMW":
				info = new BulkLoaderInfo(true, ironLoader);
				return true;
			case "CP":
				info = new BulkLoaderInfo(false, coalUnloader);
				return true;
			
			default:
				info = null;
				return false;
		}
	}
}