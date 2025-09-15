using UnityEngine;

namespace better_loading;

public record struct IndustryBuildingInfo(string name, Vector3 shuteLocalPosition)
{
	//todo delete this class?
	public readonly string name = name;
	public readonly Vector3 shuteLocalPosition = shuteLocalPosition;
}