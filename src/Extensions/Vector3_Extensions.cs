using UnityEngine;

namespace better_loading;

public static class Vector3_Extensions
{
	public static Vector3 OnlyX(this Vector3 vector)
	{
		return new Vector3(vector.x, 0, 0);
	}
	
	public static Vector3 OnlyY(this Vector3 vector)
	{
		return new Vector3(0, vector.y, 0);
	}
	
	public static Vector3 OnlyZ(this Vector3 vector)
	{
		return new Vector3(0, 0, vector.z);
	}
	
	public static Vector3 OnlyXAndZ(this Vector3 vector)
	{
		return new Vector3(vector.x, 0, vector.z);
	}

	public static Vector3 ClampMagnitude(this Vector3 vector, float maxLength)
	{
		return Vector3.ClampMagnitude(vector, maxLength);
	}

	public static Vector3 SetX(this Vector3 vector)
	{
		return new Vector3(vector.x, 0, 0);
	}
}