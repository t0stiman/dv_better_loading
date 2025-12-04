using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

public static class ObjectWaiter
{
	private static List<ObjectPath> notYetFoundObjects = new();
	private static Dictionary<ObjectPath, GameObject> foundObjects = new();

	public static void FindPlease(ObjectPath aPath)
	{
		var maybeObject = GameObject.Find(aPath.fullPath);
		if (maybeObject)
		{
			Main.Debug($"[{nameof(ObjectWaiter)}] found {aPath.fullPath} instantly");
			foundObjects.Add(aPath, maybeObject);
			return;
		}
		
		notYetFoundObjects.Add(aPath);
	}

	public static bool IsFound(ObjectPath aPath, out GameObject foundObject)
	{
		return foundObjects.TryGetValue(aPath, out foundObject);
	}

	public static void Deregister(ObjectPath aPath)
	{
		foundObjects.Remove(aPath);
	}
	
	public static void OnSceneLoaded(Scene scene, LoadSceneMode _)
	{
		foreach (var path2 in notYetFoundObjects.Where(path => path.scene == scene.name))
		{
			if(ObjectFinder.FindInScene(scene, path2, out var found))
			{
				Main.Debug($"{nameof(ObjectWaiter)} found in OnSceneLoaded '{scene.name}' '{found.GetPath()}'");
				foundObjects.Add(path2, found);
			}
			else
			{
				Main.Error($"{nameof(ObjectWaiter)} Could not find '{path2.fullPath}'");
			}
		}

		foreach (var path in foundObjects.Keys)
		{
			notYetFoundObjects.Remove(path);
		}
	}
}