using UnityModManagerNet;
using UnityEngine;

namespace better_loading;

public class Settings: UnityModManager.ModSettings
{
	public bool EnableDebugLog = false;
	public bool EnableDebugBoxes = false;
	
	public float LoadSpeedMultipler = CONVENIENT_MULTIPLIER;
	public LoadSpeedPreset MyLoadSpeedPreset = LoadSpeedPreset.Convenient;

	private const float CONVENIENT_MULTIPLIER = 4.5f;
	private const float REALISTIC_MULTIPLIER = 1f;

	public enum LoadSpeedPreset
	{
		Convenient,
		Realistic,
		// Custom
	} 
	
	public void Draw(UnityModManager.ModEntry _)
	{
		GUILayout.Label("Loading speed");
		if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Convenient, $"Convenient ({CONVENIENT_MULTIPLIER:N1}x)"))
		{
			MyLoadSpeedPreset = LoadSpeedPreset.Convenient;
			LoadSpeedMultipler = CONVENIENT_MULTIPLIER;
		}
		if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Realistic, $"Realistic ({REALISTIC_MULTIPLIER:N0}x)"))
		{
			MyLoadSpeedPreset = LoadSpeedPreset.Realistic;
			LoadSpeedMultipler = REALISTIC_MULTIPLIER;
		}
		// if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Custom, "Custom"))
		// {
		// 	MyLoadSpeedPreset = LoadSpeedPreset.Custom;
		// 	CustomMultiplierText = GUILayout.TextField(CustomMultiplierText);
		// 	LoadSpeedMultipler = float.Parse(CustomMultiplierText);
		// }
		
		GUILayout.Space(20f);
		
		EnableDebugLog = GUILayout.Toggle(EnableDebugLog, "Enable debug logging");
		EnableDebugBoxes = GUILayout.Toggle(EnableDebugBoxes, "Enable debug boxes");
	}

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}