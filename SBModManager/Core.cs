using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Godot;

using SBModManager.Attributes;
using SBModManager.GUI;
using SBModManager.Menus;
using SBModManager.SteamInterop;

namespace SBModManager {
	public sealed partial class Core : Panel {

		[AllowNull, Import]
		public TextureButton NewOSBModpackButton { get; }

		[AllowNull, Import]
		public TextureButton DuplicateModpackButton { get; }

		[AllowNull, Import]
		public TextureButton ImportModpackButton { get; }

		[AllowNull, Import]
		public TextureButton EditModpackButton { get; }

		[AllowNull, Import]
		public TextureButton DeleteModpackButton { get; }

		[AllowNull, Import]
		public TextureButton ConfigButton { get; }

		[AllowNull, Import]
		public TextureButton HelpButton { get; }

		[AllowNull, Import]
		public ProgramSettingsWindow AppSettings { get; }

		[AllowNull, Import]
		public ModpackManagementWindow EditModpack { get; }

		public override void _Ready() {
			ImportAttribute.ImportAll(this);

			NewOSBModpackButton.Pressed += OnNewOSBModpackButtonPressed;
			DuplicateModpackButton.Pressed += OnDuplicateModpackButtonPressed;
			ImportModpackButton.Pressed += OnImportModpackButtonPressed;
			EditModpackButton.Pressed += OnEditModpackButtonPressed;
			DeleteModpackButton.Pressed += OnDeleteModpackButtonPressed;
			ConfigButton.Pressed += OnConfigButtonPressed;

			AppSettings.VisibilityChanged += UpdateButtonUsability;

		}

		private void UpdateButtonUsability() {
			if (!IsFullySetUp()) {
				NewOSBModpackButton.Disabled = true;
				DuplicateModpackButton.Disabled = true;
				ImportModpackButton.Disabled = true;
				EditModpackButton.Disabled = true;
				DeleteModpackButton.Disabled = true;

				// TODO: Alert icon for config.
			} else {
				NewOSBModpackButton.Disabled = false;
				DuplicateModpackButton.Disabled = false;
				ImportModpackButton.Disabled = false;
				EditModpackButton.Disabled = false;
				DeleteModpackButton.Disabled = false;

			}
		}

		private static bool IsFullySetUp() {
			if (!Directory.Exists(Directories.GetPrivateStarboundInstallDirectory())) return false;
			return true;
		}

		private void OnNewOSBModpackButtonPressed() {
			if (!IsFullySetUp()) {
				AppSettings.Show();
				return;
			}
			throw new NotImplementedException();
		}
		
		private void OnDuplicateModpackButtonPressed() {
			if (!IsFullySetUp()) {
				AppSettings.Show();
				return;
			}
			throw new NotImplementedException();
		}

		private void OnImportModpackButtonPressed() {
			if (!IsFullySetUp()) {
				AppSettings.Show();
				return;
			}
			throw new NotImplementedException();
		}

		private void OnEditModpackButtonPressed() {
			if (!IsFullySetUp()) {
				AppSettings.Show();
				return;
			}
			//EditModpack.SetModpack( );
			//EditModpack.Show();
			throw new NotImplementedException();
		}

		private void OnDeleteModpackButtonPressed() {
			if (!IsFullySetUp()) {
				AppSettings.Show();
				return;
			}
			throw new NotImplementedException();
		}

		private void OnConfigButtonPressed() {
			AppSettings.Show();
		}

		/// <summary>
		/// Returns the icon for Starbound.
		/// </summary>
		/// <returns></returns>
		public static Texture2D GetStarboundIcon() {
			return (GD.Load("res://icons/starbound.png") as Texture2D)!;
		}

	}
}