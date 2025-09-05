using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

public static class Transform_Extensions
{
	public static Transform[] FindAllByName(this Scene scene, string name)
	{
		return scene.GetRootGameObjects()
			.SelectMany(rootObject => rootObject.transform.FindChildrenByName(name))
			.ToArray();
	}
	
	public static Transform[] FindChildrenByName(this Transform transform, string name)
	{
		return transform.GetComponentsInChildren<Transform>(true)
			.Where(anotherTransform => anotherTransform.name == name)
			.ToArray();
	}
	
	public static Transform FindChildByName(this Transform transform, string name)
	{
		return transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(anotherTransform => anotherTransform.name == name);
	}
	
	public static Transform GetChildByName(this Transform transform, string name)
	{
		for (var i = 0; i < transform.childCount; i++)
		{
			var child = transform.GetChild(i);
			if(child.name != name) continue;
			return child;
		}
		
		return null;
	}
}