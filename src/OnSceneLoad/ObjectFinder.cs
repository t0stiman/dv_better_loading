using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

/// <summary>
/// Finds objects in a scene
/// </summary>
public static class ObjectFinder
{
	public static bool FindInScene(Scene scene, GameObjectPath td, out GameObject found)
	{
		foreach (var selectedRootObject in scene.GetRootGameObjects().Where(rootObject => rootObject.name == td.rootObject))
		{
			found = td.subPath == "" ? selectedRootObject : selectedRootObject.transform.Find(td.subPath).gameObject;
			if(found) { return true; }
		}
		
		found = null;
		return false;
	}
}