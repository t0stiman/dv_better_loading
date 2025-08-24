using UnityModManagerNet;
using UnityEngine;

namespace better_loading;

public class Settings: UnityModManager.ModSettings
{
	public bool EnableDebugLog = false;
	public bool EnableDebugBox = false;
	
	//how fast the cargo is loaded, in kg/s
	public int LoadSpeed = CONVENIENT_SPEED;
	public LoadSpeedPreset MyLoadSpeedPreset = LoadSpeedPreset.Convenient;
	
	//presets
	private const int CONVENIENT_SPEED = 5000;
	private const int REALISTIC_SPEED = 56000/74;

	public enum LoadSpeedPreset
	{
		Convenient,
		Realistic,
		Custom
	} 
	
	public void Draw(UnityModManager.ModEntry modEntry)
	{
		GUILayout.Label("Loading speed, in kg/s");
		if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Convenient, $"Convenient ({CONVENIENT_SPEED})"))
		{
			MyLoadSpeedPreset = LoadSpeedPreset.Convenient;
			LoadSpeed = CONVENIENT_SPEED;
		}
		if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Realistic, $"Realistic ({REALISTIC_SPEED})"))
		{
			MyLoadSpeedPreset = LoadSpeedPreset.Realistic;
			LoadSpeed = REALISTIC_SPEED;
		}
		if (GUILayout.Toggle(MyLoadSpeedPreset == LoadSpeedPreset.Custom, "Custom"))
		{
			MyLoadSpeedPreset = LoadSpeedPreset.Custom;
			LoadSpeed = int.Parse(GUILayout.TextField(LoadSpeed.ToString()));
		}
		
		GUILayout.Space(20f);
		
		EnableDebugLog = GUILayout.Toggle(EnableDebugLog, "Enable debug logging");
		EnableDebugBox = GUILayout.Toggle(EnableDebugBox, "Enable debug box");
	}

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}