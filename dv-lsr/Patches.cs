using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LSR;

public static class Patches
{
	[HarmonyPatch(typeof(RandomScreenPicker), "OnEnable")]
	public static class RandomScreenPicker_OnEnable_Patch
	{
		private static bool enabled;

		internal static void SetEnabled(bool isEnabled)
		{
			enabled = isEnabled;
		}

		static bool Prefix(RandomScreenPicker __instance)
		{
			if (!enabled) return true;
			// Check if we have any custom loading screens
			var customTexture = Main.GetRandomLoadingScreen();
			if (customTexture == null)
			{
				// No custom screens, let the original method run
				return true;
			}

			try
			{
				// Get the displayComponent field
				var displayField = typeof(RandomScreenPicker).GetField("displayComponent", BindingFlags.Public | BindingFlags.Instance);
				if (displayField != null)
				{
					var displayComponent = displayField.GetValue(__instance) as UnityEngine.UI.RawImage;
					if (displayComponent != null)
					{
						displayComponent.texture = customTexture;
						displayComponent.color = Color.white;
						Main.ModEntry?.Logger.Log($"Displaying custom loading screen: {customTexture.name}");
					}
				}

				// Skip the original method since we've set our custom texture
				return false;
			}
			catch (Exception ex)
			{
				Main.ModEntry?.Logger.LogException("Error displaying custom loading screen:", ex);
				// Fall back to original on error
				return true;
			}
		}
	}

}
