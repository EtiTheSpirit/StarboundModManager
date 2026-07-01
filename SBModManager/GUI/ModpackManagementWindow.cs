using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using SBModManager.Attributes;
using SBModManager.Menus;

namespace SBModManager.GUI {

	/// <summary>
	/// The entire window for managing a specific modpack.
	/// </summary>
	public partial class ModpackManagementWindow : AutoClosableWindow {

		/// <summary>
		/// This button opens a prompt to export the modpack.
		/// </summary>
		[Import, AllowNull]
		public Button ExportButton { get; }

		/// <summary>
		/// This button applies all edits.
		/// </summary>
		[Import, AllowNull]
		public Button OKButton { get; }

		/// <summary>
		/// The panel where the modpack's name, author, description, and icon are set.
		/// </summary>
		[Import, AllowNull]
		public EditModpackDetailsPanel EditModpackDetails { get; }
		
		/// <summary>
		/// The panel where the list of mods is shown.
		/// </summary>
		[Import, AllowNull]
		public ViewModListPanel ViewModList { get; }

		public override void _Ready() {
			ImportAttribute.ImportAll(this);

			CloseRequested += OnCloseRequested;
		}

		private void OnCloseRequested() {
			EditModpackDetails.OnClosing();
		}

		/// <summary>
		/// Sets <see cref="EditingModpack"/>, and updates every element in this menu to display its customization.
		/// </summary>
		/// <param name="modpack"></param>
		internal void SetModpack(Modpack modpack) {
			ArgumentNullException.ThrowIfNull(modpack);
			EditModpackDetails.SetModpack(modpack);
			ViewModList.SetModpack(modpack);
		}
	}
}
