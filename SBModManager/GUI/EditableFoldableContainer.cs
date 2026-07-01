using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using SBModManager.Attributes;

namespace SBModManager.GUI {

	/// <summary>
	/// This is a compound node which relies on a specific hierarchy.
	/// </summary>
	public partial class EditableFoldableContainer : Control {

		[Import, AllowNull]
		public FoldableContainer Container { get; }

		[Import, AllowNull]
		public LineEdit EditableName { get; }

		[Import, AllowNull]
		public VBoxContainer Children { get; }

		public string DisplayedName {
			get => EditableName.Text;
			set {
				EditableName.Text = value ?? string.Empty;
				_lastDisplayedName = value ?? string.Empty;
			}
		}
		private string _lastDisplayedName = "";

		public override void _Ready() {
			ImportAttribute.ImportAll(this);

			EditableName.GuiInput += OnEditableNameInput;
			EditableName.TextSubmitted += OnEditableNameSubmitted;
			EditableName.EditingToggled += OnEditingToggled;
			Container.GuiInput += OnContainerInput;
		}

		private void OnContainerInput(InputEvent @event) => OnEditableNameInput(@event);

		private void OnEditingToggled(bool toggledOn) {
			if (!toggledOn) {
				OnEditableNameSubmitted(EditableName.Text);
			}
		}

		private void OnEditableNameSubmitted(string newText) {
			EditableName.Editable = false;
			EditableName.MouseDefaultCursorShape = CursorShape.Arrow;
			_lastDisplayedName = newText;
			EditableName.Text = newText;
			EditableName.SetProcessInput(false);
		}

		private void OnEditableNameInput(InputEvent @event) {
			if (@event is InputEventMouseButton mouseButton) {
				if (mouseButton.ButtonIndex == MouseButton.Right) {
					if (!EditableName.Editable) {
						EditableName.Editable = true;
						EditableName.MouseDefaultCursorShape = CursorShape.Ibeam;
						EditableName.SetProcessInput(true);
						EditableName.Edit(true);
						EditableName.AcceptEvent();
						return;
					}
				}
			}
		}
	}
}
