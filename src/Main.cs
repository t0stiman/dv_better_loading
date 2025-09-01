using System;
using System.Reflection;
using UnityModManagerNet;
using HarmonyLib;

namespace better_loading
{
	[EnableReloading]
	static class Main
	{
		private static UnityModManager.ModEntry myModEntry;
		private static Harmony myHarmony;
		public static Settings MySettings { get; private set; }

		//===========================================

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			try
			{
				myModEntry = modEntry;
				modEntry.OnUnload = OnUnload;
				
				MySettings = UnityModManager.ModSettings.Load<Settings>(modEntry);
				modEntry.OnGUI = entry => MySettings.Draw(entry);
				modEntry.OnSaveGUI = entry => MySettings.Save(entry);
				
				myHarmony = new Harmony(modEntry.Info.Id);
				myHarmony.PatchAll(Assembly.GetExecutingAssembly());
			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				myHarmony?.UnpatchAll(modEntry.Info.Id);
				return false;
			}

			Log("loaded");
			return true;
		}

		private static bool OnUnload(UnityModManager.ModEntry modEntry)
		{
			myHarmony?.UnpatchAll(modEntry.Info.Id);
			return true;
		}

		// Logger functions
		public static void Log(object message)
		{
			myModEntry.Logger.Log($"[INFO] {message}");
		}

		public static void Warning(object message)
		{
			myModEntry.Logger.Warning($"{message}");
		}

		public static void Error(object message)
		{
			myModEntry.Logger.Error($"{message}");
		}
		
		public static void Debug(object message)
		{
			if(!MySettings.EnableDebugLog) return;
			myModEntry.Logger.Log($"[DEBUG] {message}");
		}
	}
}