using System;
using System.Collections;
using UnityEngine;

namespace better_loading;

public class ShuteEffectsManager: MonoBehaviour
{
	private ShuteEffects[] shutes;
	private static LocoResourceModule tenderCoalModule;

	public static void SetTenderCoalModule(LocoResourceModule tenderCoalModule_)
	{
		tenderCoalModule = tenderCoalModule_;
	}

	public void CreateShutes(Vector3[] shutePositions)
	{
		if (shutePositions.Length == 0)
		{
			throw new ArgumentException($"{nameof(shutePositions)} cannot be empty");
		}
		
		shutes = new ShuteEffects[shutePositions.Length];
		
		for (var index = 0; index < shutePositions.Length; index++)
		{
			var shutePos = shutePositions[index];
			var shuteObject =
				Utilities.CreateGameObject(transform, shutePos, Quaternion.identity, $"shuteObject {index}", false);
			shutes[index] = shuteObject.AddComponent<ShuteEffects>();
		}
	}

	public void ScheduleInitializeEffects()
	{
		StartCoroutine(WaitForCoalModule());
	}

	private IEnumerator WaitForCoalModule()
	{
		while (tenderCoalModule == null) yield return new WaitForSeconds(1);
		InitializeEffects();
	}
	
	public void InitializeEffects()
	{
		foreach (var shute in shutes)
		{
			shute.Initialize(tenderCoalModule);
		}
	}
	
	public void StartTransferring()
	{
		foreach (var shute in shutes)
		{
			shute.StartTransferring();
		}
	}
	
	public void StopTransferring(string reason)
	{
		foreach (var shute in shutes)
		{
			shute.StopTransferring(reason);
		}
	}
}