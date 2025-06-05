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

		//===========================================

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			try
			{
				myModEntry = modEntry;
				myModEntry.OnUnload = OnUnload;
				
				myHarmony = new Harmony(myModEntry.Info.Id);
				myHarmony.PatchAll(Assembly.GetExecutingAssembly());
			}
			catch (Exception ex)
			{
				myModEntry.Logger.LogException($"Failed to load {myModEntry.Info.DisplayName}:", ex);
				myHarmony?.UnpatchAll(myModEntry.Info.Id);
				return false;
			}

			Log("loaded");
			return true;
		}

		private static bool OnUnload(UnityModManager.ModEntry modEntry)
		{
			myHarmony?.UnpatchAll(myModEntry.Info.Id);
			return true;
		}

		// Logger functions
		public static void Log(string message)
		{
			myModEntry.Logger.Log(message);
		}

		public static void Warning(string message)
		{
			myModEntry.Logger.Warning(message);
		}

		public static void Error(string message)
		{
			myModEntry.Logger.Error(message);
		}
		
		public static void Debug(string message)
		{
			//todo debug setting
			myModEntry.Logger.Log(message);
		}
	}
}