using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace LSR;

public static class Main
{
	public static UnityModManager.ModEntry? ModEntry { get; private set; }
	public static bool enabled { get; private set; } = true;
	private static string LoadingScreensPath => Path.Combine(ModEntry?.Path ?? "", "LoadingScreens");
	private static List<Texture2D> customLoadingScreens = new List<Texture2D>();
	private static System.Random random = new System.Random();

	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		ModEntry = modEntry;
		Harmony? harmony = null;

		try
		{
			// Create LoadingScreens folder if it doesn't exist
			Directory.CreateDirectory(LoadingScreensPath);

			// Load custom loading screens
			LoadCustomLoadingScreens();

			// Patch RandomScreenPicker_OnEnable
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			modEntry.OnToggle = OnToggle;
			modEntry.OnUnload = Unload;

			modEntry.Logger.Log("Loading Screen Replacer loaded successfully!");

			if (customLoadingScreens.Count > 0)
			{
				modEntry.Logger.Log($"Loaded {customLoadingScreens.Count} custom loading screen(s) from: {LoadingScreensPath}");
			}
			else
			{
				modEntry.Logger.Log($"No custom loading screens found in: {LoadingScreensPath}");
				modEntry.Logger.Log("The game will use default loading screens.");
				modEntry.Logger.Log("To add custom loading screens, place PNG, JPG, or JPEG files in the LoadingScreens folder.");
			}
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll(modEntry.Info.Id);
			return false;
		}

		return true;
	}

	private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
	{
		enabled = value;
		Patches.RandomScreenPicker_OnEnable_Patch.SetEnabled(enabled);
		return true;
	}

	private static bool Unload(UnityModManager.ModEntry modEntry)
	{
		try
		{
			ModEntry = modEntry;
			Harmony? harmony = null;
			harmony = new Harmony(modEntry.Info.Id);
			harmony.UnpatchAll(modEntry.Info.Id);
			return true;
		}
		catch (Exception ex)
		{
			ModEntry?.Logger.LogException($"Failed to unload {modEntry.Info.DisplayName}:", ex);
			return false;
		}
	}

	private static void LoadCustomLoadingScreens()
	{
		ModEntry?.Logger.Log($"Looking for custom loading screens in: {LoadingScreensPath}");

		if (!Directory.Exists(LoadingScreensPath))
		{
			ModEntry?.Logger.Log($"LoadingScreens directory does not exist. Creating it...");
			return;
		}

		// Get all image files (PNG, JPG, JPEG)
		var imageFiles = new List<string>();
		imageFiles.AddRange(Directory.GetFiles(LoadingScreensPath, "*.png", SearchOption.TopDirectoryOnly));
		imageFiles.AddRange(Directory.GetFiles(LoadingScreensPath, "*.jpg", SearchOption.TopDirectoryOnly));
		imageFiles.AddRange(Directory.GetFiles(LoadingScreensPath, "*.jpeg", SearchOption.TopDirectoryOnly));

		ModEntry?.Logger.Log($"Found {imageFiles.Count} image file(s)");

		foreach (var file in imageFiles)
		{
			try
			{
				var fileData = File.ReadAllBytes(file);
				var texture = new Texture2D(2, 2);

				if (texture.LoadImage(fileData))
				{
					texture.name = Path.GetFileNameWithoutExtension(file);
					customLoadingScreens.Add(texture);
				}
				else
				{
					ModEntry?.Logger.Error($"  - Failed to load image: {file}");
					UnityEngine.Object.Destroy(texture);
				}
			}
			catch (Exception ex)
			{
				ModEntry?.Logger.LogException($"  - Error loading {file}:", ex);
			}
		}
	}

	public static Texture2D? GetRandomLoadingScreen()
	{
		return customLoadingScreens.Count == 0 ? null : customLoadingScreens[random.Next(customLoadingScreens.Count)];
	}
}
