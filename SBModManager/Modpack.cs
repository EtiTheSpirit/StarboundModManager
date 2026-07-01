using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using SBModManager.SteamInterop;

namespace SBModManager {

	/// <summary>
	/// Represents an entire modpack.
	/// </summary>
	public sealed class Modpack {

		/// <summary>
		/// The name of this modpack, in a user-friendly format. Cannot be null.
		/// </summary>
		public required string Name {
			get;
			set => field = value ?? throw new ArgumentNullException(nameof(Name));
		} = "No Name";

		/// <summary>
		/// The person or entity who created this modpack. Cannot be null.
		/// </summary>
		public required string Creator {
			get;
			set => field = value ?? throw new ArgumentNullException(nameof(Creator));
		} = "";

		/// <summary>
		/// The user-defined description of this modpack. Cannot be null.
		/// </summary>
		public string Description {
			get;
			set => field = value ?? throw new ArgumentNullException(nameof(Description));
		} = "";

		/// <summary>
		/// The GUID of this modpack. This does not change.
		/// </summary>
		public Guid ID { get; }

		/// <summary>
		/// The IDs of all workshop mods that are part of this pack. They are loaded from the workshop cache, not
		/// from the actual Steam Workshop. The boolean value indicates if the mod is enabled or not.
		/// </summary>
		public Dictionary<ulong, bool> WorkshopModIDs { get; } = [];

		/// <summary>
		/// The names of all shared mods. Shared mods go in a folder and have a specific name, just like workshop mods,
		/// but with a name instead of an ID.
		/// </summary>
		public Dictionary<string, bool> SharedModNames { get; } = [];

		private Modpack() { }

		/// <summary>
		/// Attempts to create a modpack with the provided name.
		/// </summary>
		/// <param name="starbound">The location of Starbound.exe</param>
		/// <param name="name"></param>
		/// <param name="modpack"></param>
		/// <returns></returns>
		public static Error CreateModpack(FileInfo starbound, string name, out Modpack? modpack) {
			modpack = null;
			return Error.PrinterOnFire;
		}

		/// <summary>
		/// Returns a <see cref="Texture2D"/> representing the icon of this modpack.
		/// </summary>
		/// <returns></returns>
		public Texture2D GetIcon() {
			string directory = Directories.GetPackDirectory(ID);
			string icon = Path.Combine(directory, "icon.png");
			try {
				Image? result = Image.LoadFromFile(icon);
				if (result != null) {
					return ImageTexture.CreateFromImage(result);
				}
			} catch { }
			return Core.GetStarboundIcon();
		}

		/// <summary>
		/// Sets the modpack icon based on an image file.
		/// </summary>
		/// <param name="imageFile"></param>
		public Texture2D? TrySetIcon(string imageFile) {
			string directory = Directories.GetPackDirectory(ID);
			string icon = Path.Combine(directory, "icon.png");
			try {
				Image? result = Image.LoadFromFile(imageFile);
				if (result != null) {
					result.Resize(256, 256, Image.Interpolation.Lanczos);
					result.SavePng(icon);
					return ImageTexture.CreateFromImage(result);
				}
			} catch { }
			return null;
		}

		/// <summary>
		/// Returns the JSON string which represents the contents of sbinit.config. This may need to do some downloading ahead of time
		/// for things like Workshop mods.
		/// </summary>
		/// <returns></returns>
		public async Task<string> PrepareForLaunchAsync(CancellationToken cancellationToken) {
			GDArray assetDirectories = [];
			assetDirectories.Add(Directories.GetExtraAssetsDirectory(ID));
			assetDirectories.Add(Directories.GetExtraModsDirectory(ID));

			// This might prevent a lot of downloading.
			SteamTools.CopyAllCurrentSubscriptionsToCache(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			await SteamTools.DownloadWorkshopModsAsync(WorkshopModIDs.Where(static kvp => kvp.Value).Select(static kvp => kvp.Key).ToArray(), true, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			string workshopCache = Directories.GetLocalWorkshopCacheDirectory();
			foreach (KeyValuePair<ulong, bool> kvp in WorkshopModIDs) {
				if (!kvp.Value) continue;
				cancellationToken.ThrowIfCancellationRequested();
				assetDirectories.Add(Path2.Combine(workshopCache, kvp.Key.ToString()));
			}

			GDDictionary sbInit = [	];
			sbInit["assetDirectories"] = assetDirectories;
			sbInit["logDirectory"] = Directories.GetLogDirectory(ID);
			sbInit["storageDirectory"] = Directories.GetStorageDirectory(ID);
			sbInit["includeUGC"] = false;

			return Json.Stringify(sbInit, "\t", false, false);
		}

	}
}
