using UnityModManagerNet;
using UnityEngine;

namespace better_loading;

public class Settings: UnityModManager.ModSettings
{
	public bool EnableDebugLog = false;
	
	public void Draw(UnityModManager.ModEntry modEntry)
	{
		EnableDebugLog = GUILayout.Toggle(EnableDebugLog, "Enable debug logging");
	}

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}