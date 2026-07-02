using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.GUI {

	/// <summary>
	/// Explicitly designed for the <see cref="RichTextLabel"/> in use inside of Godot's tooltip popup.
	/// </summary>
	public partial class MovingRichTextLabel : RichTextLabel {

		public static MovingRichTextLabel? MostRecentTooltip {
			get {
				if (!IsInstanceValid(field)) return null;
				return field;
			}
			private set;
		}

		private int _line = 0;

		public override void _EnterTree() {
			MostRecentTooltip = this;
		}

		public override void _Process(double delta) {
			if (GetParent() is Popup popup) {
				Viewport viewport = GetViewport();
				if (viewport == null) return;
				Window window = viewport.GetWindow();
				if (window == null) return;

				Vector2 mousePos = viewport.GetMousePosition();
				popup.Position = (Vector2I)(window.Position + mousePos + new Vector2(12, 12));
			}
		}

		public void ScrollUp() {
			_line = int.Clamp(_line - 1, 0, GetLineCount());
			ScrollToLine(_line);
		}

		public void ScrollDown() {
			_line = int.Clamp(_line + 1, 0, GetLineCount());
			ScrollToLine(_line);
		}

	}
}
