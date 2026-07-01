using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SBModManager {

	/// <summary>
	/// The program's settings.
	/// </summary>
	public static class ProgramSettings {

		/// <summary>
		/// The location of SteamCMD, or <see langword="null"/> if it is not installed.
		/// </summary>
		public static FileInfo? SteamCMD { get; set; }

		public static void Load() {
			try {
				string cfg = Directories.GetAppConfigFile();
				Variant json = Json.ParseString(File.ReadAllText(cfg));
				if (json.Obj is GDDictionary dictionary) {
					string steamCMDLocation = (string)dictionary["steamcmd_location"];
					if (!string.IsNullOrWhiteSpace(steamCMDLocation)) {
						SteamCMD = new FileInfo(steamCMDLocation);
					}
				}
			} catch { }
		}

		public static void Save() {
			try {
				string cfg = Directories.GetAppConfigFile();
				File.WriteAllText(
					cfg, 
					Json.Stringify(
						new GDDictionary {
							{ "steamcmd_location", SteamCMD?.FullName ?? string.Empty }
						},
						"\t", false, false
					)
				);
			} catch { }
		}



	}
}
