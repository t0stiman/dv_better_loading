using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

// removes some objects that are in the way
public static class ObjectDeleter
{
	private record struct ObjectPath
	{
		public readonly string fullPath;
		
		public readonly string scene;
		public readonly string rootObject;
		public readonly string subPath;

		public ObjectPath(string fullPath_)
		{
			fullPath = fullPath_;

			var slashIndex = fullPath_.IndexOf('/');
			rootObject = fullPath.Substring(0, slashIndex);
			subPath = fullPath.Substring(slashIndex+1);
			
			scene = rootObject.Replace("_LFS", "");
		}

		public static implicit operator ObjectPath(string fullPath)
		{
			return new ObjectPath(fullPath);
		}
	}
	
	//copy-pasted this from runtime unity editor
	private static readonly ObjectPath[] toDelete =
	{
		//GF
		"Far__x12_z10_LFS/Far_Containers/Container_Stack_import_clothing_variant 7",
		"Far__x12_z10_LFS/Far_Vehicles/TrailerMedium90sTarp_01_Blue",
		"Far__x12_z10_LFS/Far_Vehicles/TrailerMedium90sTarp_01_Blue (1)",
		"Far__x12_z10_LFS/Far_Containers/Container_Stack_export_misc_variant 8",
		"Far__x12_z10_LFS/Far_Containers/Container_Stack_export_misc_variant 2 (1)",
		"Far__x12_z10_LFS/Far_Props/TrafficCone (4)",
		"Far__x12_z10_LFS/Far_Containers/Container_Stack_export_misc_variant 2"
	};
	
	public static void OnSceneLoaded(Scene scene, LoadSceneMode _)
	{
		foreach (var td in toDelete.Where(td => td.scene == scene.name))
		{
			foreach (var selectedRootObject in scene.GetRootGameObjects().Where(rootObject => rootObject.name == td.rootObject))
			{
				var found = td.subPath == "" ? selectedRootObject.transform : selectedRootObject.transform.Find(td.subPath);
				if (found)
				{
					Main.Debug($"{nameof(ObjectDeleter)} '{scene.name}' '{found.gameObject.GetPath()}'");
					Object.Destroy(found.gameObject);
				}
				else
				{
					Main.Error($"{nameof(ObjectDeleter)} Could not find '{td.fullPath}'");
				}
			}
		}
	}
}