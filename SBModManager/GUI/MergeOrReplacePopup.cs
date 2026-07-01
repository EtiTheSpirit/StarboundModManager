using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using SBModManager.Attributes;

namespace SBModManager.GUI {
	public partial class MergeOrReplacePopup : Popup {

		[Import, AllowNull]
		public Button Cancel { get; }

		[Import, AllowNull]
		public Button Merge { get; }

		[Import, AllowNull]
		public Button Replace { get; }

		/// <summary>
		/// This event fires when the Merge option is selected.
		/// It is unset automatically.
		/// </summary>
		public Action? Merged { get; set; }

		/// <summary>
		/// This event fires when the Replace option is selected.
		/// It is unset automatically.
		/// </summary>
		public Action? Replaced { get; set; }

		public override void _Ready() {
			ImportAttribute.ImportAll(this);
			Cancel.Pressed += OnCancelPressed;
			Merge.Pressed += OnMergePressed;
			Replace.Pressed += OnReplacePressed;
		}

		public void ForceCancel() => OnCancelPressed();

		private void OnCancelPressed() {
			Merged = null;
			Replaced = null;
			Hide();
		}

		private void OnReplacePressed() {
			Replaced?.Invoke();
			Merged = null;
			Replaced = null;
			Hide();
		}

		private void OnMergePressed() {
			Merged?.Invoke();
			Merged = null;
			Replaced = null;
			Hide();
		}
	}
}
