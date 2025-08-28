using UnityEngine;

namespace better_loading;

public class IndustryBuildingInfo
{
	public readonly string name;
	public readonly Vector3 shutePosition;

	private IndustryBuildingInfo(string _name, Vector3 _shutePosition)
	{
		name = _name;
		shutePosition = _shutePosition;
	}
	
	private static readonly IndustryBuildingInfo coal = new ("Industry_Coal", new Vector3(-85.976f, 8.554f, 1.999f));
	private static readonly IndustryBuildingInfo iron = new ("Industry_Iron", new Vector3(-20.5f, 7.5f, -6.02f));

	public static bool TryGetInfo(string stationID, out IndustryBuildingInfo info)
	{
		switch (stationID)
		{
			case "CME":
			case "CMS":
				info = coal;
				return true;
			case "IME":
			case "IMW":
				info = iron;
				return true;
			
			default:
				info = null;
				return false;
		}
	}
}