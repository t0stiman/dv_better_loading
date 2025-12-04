using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace better_loading;

// removes some objects that are in the way
public static class ObjectDeleter
{
	//paths copy-pasted from runtime unity editor
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
			if(ObjectFinder.FindInScene(scene, td, out var found))
			{
				Main.Debug($"{nameof(ObjectDeleter)} '{scene.name}' '{found.GetPath()}'");
				Object.Destroy(found);
			}
			else
			{
				Main.Error($"{nameof(ObjectDeleter)} Could not find '{td.fullPath}'");
			}
		}
	}
}