using DV;
using UnityEngine;

namespace better_loading;

public class ShuteEffects: MonoBehaviour
{
	private LayeredAudio audioSource;
	private const ResourceFlowMode flowMode = ResourceFlowMode.Air;
	private float flowVolume;
	private float curVolumeVelocity;
	private ParticleSystem[] raycastFlowingEffects = {};

	private bool cargoIsFlowing = false;
	private GameObject debugBox;
	private bool initialized = false;

	public void Initialize(LocoResourceModule tenderCoalModule_)
	{
		InitializeVisuals(tenderCoalModule_);
		InitializeAudio(tenderCoalModule_);
		
		// box
		debugBox = Utilities.CreateDebugCube(transform, $"{nameof(ShuteEffects)} debug cube");

		initialized = true;
		Main.Debug(gameObject.GetPath());
	}
	
	private void InitializeVisuals(LocoResourceModule tenderCoalModule_)
	{
		var effectsObject = Instantiate(
			tenderCoalModule_.raycastFlowingEffects[0].transform.parent,
			transform
		);

		effectsObject.name = nameof(effectsObject);
		effectsObject.rotation = Quaternion.identity;
		raycastFlowingEffects = effectsObject.GetComponentsInChildren<ParticleSystem>();

		if (raycastFlowingEffects.Length == 0)
		{
			Main.Error("no effects");
		}
	}
	
	private void InitializeAudio(LocoResourceModule tenderCoalModule_)
	{
		var original = tenderCoalModule_.audioSourcesPerFlow[(int)flowMode];
		var audioObject = Instantiate(original.gameObject, transform);
		audioObject.name = $"{nameof(ShuteEffects)} LoadingSound";
		audioSource = audioObject.GetComponentInChildren<LayeredAudio>();
		if (!audioSource)
		{
			Main.Error("no audio");
		}
	}

	private void OnDisable()
	{
		StopTransferring(nameof(OnDisable));
	}

	private void Update()
	{
		if(!initialized) return;
		
		debugBox.SetActive(Main.MySettings.EnableDebugBoxes);
		
		//game paused?
		if (!TimeUtil.IsFlowing) return;
		
		DoSound();
	}
	
	protected void DoSound()
	{
		if(!audioSource) return;

		if (cargoIsFlowing)
		{
			if (flowVolume < 1.0)
			{
				//increase volume
				flowVolume = Mathf.SmoothDamp(flowVolume, 1f, ref curVolumeVelocity, LocoResourceModule.FLOW_RATE_PERCENTAGE);
			}
		}
		else if (flowVolume > 0.0)
		{
			//decrease volume
			flowVolume = Mathf.SmoothDamp(flowVolume, 0f, ref curVolumeVelocity, LocoResourceModule.FLOW_RATE_PERCENTAGE);
		}
		
		audioSource.Set(flowVolume);
	}

	public void StartTransferring()
	{
		if(cargoIsFlowing) return;
		cargoIsFlowing = true;
		
		foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		{
			raycastFlowingEffect.Play();
		}
	}
	
	public void StopTransferring(string reason)
	{
		if(!cargoIsFlowing) return;
		
		cargoIsFlowing = false;
		Main.Debug($"{nameof(ShuteEffects)}.{nameof(StopTransferring)} {reason}");
		
		foreach (ParticleSystem raycastFlowingEffect in raycastFlowingEffects)
		{
			raycastFlowingEffect.Stop();
		}
	}
}