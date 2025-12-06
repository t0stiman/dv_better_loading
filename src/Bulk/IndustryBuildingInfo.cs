using UnityEngine;

namespace better_loading;

public record struct IndustryBuildingInfo(string name, Vector3 shuteLocalPosition)
{
	// name of the GameObject
	public readonly string name = name;
	
	// the position of the loading chute relative to the parent object
	public readonly Vector3 shuteLocalPosition = shuteLocalPosition;
}