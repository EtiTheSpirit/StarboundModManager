using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using SBModManager.Attributes;
using SBModManager.SteamInterop;

namespace SBModManager.GUI {
	public partial class ViewModListPanel : MarginContainer {

		/// <summary>
		/// The modpack that is currently being edited.
		/// </summary>
		public Modpack? EditingModpack { get; private set; }

		/// <summary>
		/// The list of mods to display.
		/// </summary>
		[Import, AllowNull]
		public VBoxContainer ModsList { get; }

		/// <summary>
		/// The button to import mods from the Steam Workshop.
		/// </summary>
		[Import, AllowNull]
		public Button ImportFromWorkshopButton { get; }

		/// <summary>
		/// The button to import mods from a list.
		/// </summary>
		[Import, AllowNull]
		public Button ImportFromListButton { get; }

		/// <summary>
		/// The button to import a mod from the catalog.
		/// </summary>
		[Import, AllowNull]
		public Button ImportFromCatalogButton { get; }

		/// <summary>
		/// The button to import a mod from a downloaded file or directory.
		/// </summary>
		[Import, AllowNull]
		public Button ImportFromFileButton { get; }

		/// <summary>
		/// The file dialog to find mod lists.
		/// </summary>
		[Import, AllowNull]
		public FileDialog FindModListDialog { get; }

		/// <summary>
		/// The popup for importing mods that decides if they should be merged or replaced.
		/// </summary>
		[Import, AllowNull]
		public MergeOrReplacePopup ImportOrOverwrite { get; }

		public override void _Ready() {
			ImportAttribute.ImportAll(this);
			ImportFromWorkshopButton.Pressed += OnImportFromWorkshopPressed;
			ImportFromListButton.Pressed += OnImportFromListPressed;
			FindModListDialog.FileSelected += OnModlistFileSelected;
		}

		private void OnModlistFileSelected(string path) {
			if (ImportOrOverwrite.Visible) {
				OS.Alert("Not importing this list; a prompt to merge or\nreplace a different list is still open.\nFinish it first, or cancel it.");
				return;
			}

			ImportOrOverwrite.Show();
			ImportOrOverwrite.Merged = delegate {

			};
			ImportOrOverwrite.Replaced = delegate {

			};
		}

		private void OnImportFromListPressed() {
			if (ImportOrOverwrite.Visible) return;
		}

		private void OnImportFromWorkshopPressed() {
			if (ImportOrOverwrite.Visible) {
				OS.Alert("Not importing workshop subscriptions; a prompt to\nmerge or replace a different list is still open.\nFinish it first, or cancel it.");
				return;
			}
			ImportOrOverwrite.Show();
			ImportOrOverwrite.Merged = delegate {
				if (EditingModpack == null) return;
				ulong[] subscriptions = SteamTools.CopyAllCurrentSubscriptionsToCache(default);
				foreach (ulong subscription in subscriptions) {
					EditingModpack.WorkshopModIDs[subscription] = true;
				}
			};
			ImportOrOverwrite.Replaced = delegate {
				if (EditingModpack == null) return;
				EditingModpack.WorkshopModIDs.Clear();

				ulong[] subscriptions = SteamTools.CopyAllCurrentSubscriptionsToCache(default);
				foreach (ulong subscription in subscriptions) {
					EditingModpack.WorkshopModIDs[subscription] = true;
				}
			};
		}

		/// <summary>
		/// Sets <see cref="EditingModpack"/>, and updates every element in this menu to display its customization.
		/// </summary>
		/// <param name="modpack"></param>
		internal void SetModpack(Modpack modpack) {
			ArgumentNullException.ThrowIfNull(modpack);
			EditingModpack = modpack;
			FindModListDialog.Hide();
			ImportOrOverwrite.ForceCancel();
		}
	}
}
