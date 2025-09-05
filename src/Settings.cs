using System;
using UnityModManagerNet;
using UnityEngine;

namespace better_loading;

public class Settings: UnityModManager.ModSettings
{
	public bool EnableDebugLog = false;
	public bool EnableVerboseDebugLog = false;
	public bool EnableDebugBoxes = false;
	
	private const float REALISTIC_MULTIPLIER = 1f;
	
	//bulk
	private const float BULK_CONVENIENT_MULTIPLIER = 4f;
	private const float BULK_FAST_MULTIPLIER = 8f;
	
	public float BulkLoadSpeedMultiplier => BulkLoadSpeedPreset switch
	{
		LoadSpeedPreset.Realistic => REALISTIC_MULTIPLIER,
		LoadSpeedPreset.Convenient => BULK_CONVENIENT_MULTIPLIER,
		LoadSpeedPreset.Fast => BULK_FAST_MULTIPLIER,
		_ => throw new ArgumentOutOfRangeException()
	};
	public LoadSpeedPreset BulkLoadSpeedPreset = LoadSpeedPreset.Convenient;

	//containers
	private const float CONTAINER_REALISTIC_SPEED = 1.5f;
	private const float CONTAINER_CONVENIENT_SPEED = 3f;
	private const float CONTAINER_FAST_SPEED = 12f;

	public float ContainerLoadSpeed => ContainerLoadSpeedPreset switch
	{
		LoadSpeedPreset.Realistic => CONTAINER_REALISTIC_SPEED,
		LoadSpeedPreset.Convenient => CONTAINER_CONVENIENT_SPEED,
		LoadSpeedPreset.Fast => CONTAINER_FAST_SPEED,
		_ => throw new ArgumentOutOfRangeException()
	};
	public LoadSpeedPreset ContainerLoadSpeedPreset = LoadSpeedPreset.Convenient;

	public enum LoadSpeedPreset
	{
		Realistic = 0,
		Convenient = 1,
		Fast = 2
	} 
	
	public void Draw(UnityModManager.ModEntry _)
	{
		GUILayout.Label("Bulk cargo loading speed (iron ore, coal, grains, etc)");
		
		if (GUILayout.Toggle(BulkLoadSpeedPreset == LoadSpeedPreset.Realistic, $"Realistic ({REALISTIC_MULTIPLIER:N0}x)"))
		{
			BulkLoadSpeedPreset = LoadSpeedPreset.Realistic;
		}
		if (GUILayout.Toggle(BulkLoadSpeedPreset == LoadSpeedPreset.Convenient, $"Convenient ({BULK_CONVENIENT_MULTIPLIER:N1}x)"))
		{
			BulkLoadSpeedPreset = LoadSpeedPreset.Convenient;
		}
		if (GUILayout.Toggle(BulkLoadSpeedPreset == LoadSpeedPreset.Fast, $"Fast ({BULK_FAST_MULTIPLIER:N1}x)"))
		{
			BulkLoadSpeedPreset = LoadSpeedPreset.Fast;
		}
		
		GUILayout.Space(20f);
		
		GUILayout.Label("Shipping containers loading speed");
		
		if (GUILayout.Toggle(ContainerLoadSpeedPreset == LoadSpeedPreset.Realistic, $"Realistic ({REALISTIC_MULTIPLIER:N0} m/s)"))
		{
			ContainerLoadSpeedPreset = LoadSpeedPreset.Realistic;
		}
		if (GUILayout.Toggle(ContainerLoadSpeedPreset == LoadSpeedPreset.Convenient, $"Convenient ({CONTAINER_CONVENIENT_SPEED:N0} m/s)"))
		{
			ContainerLoadSpeedPreset = LoadSpeedPreset.Convenient;
		}
		if (GUILayout.Toggle(ContainerLoadSpeedPreset == LoadSpeedPreset.Fast, $"Fast ({CONTAINER_FAST_SPEED:N0} m/s)"))
		{
			ContainerLoadSpeedPreset = LoadSpeedPreset.Fast;
		}
		
		GUILayout.Space(20f);
		
		EnableDebugLog = GUILayout.Toggle(EnableDebugLog, "Enable debug logging");
		if (EnableDebugLog)
		{
			EnableVerboseDebugLog = GUILayout.Toggle(EnableVerboseDebugLog, "Enable verbose debug logging");
		}
		EnableDebugBoxes = GUILayout.Toggle(EnableDebugBoxes, "Enable debug boxes");
	}

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}
}