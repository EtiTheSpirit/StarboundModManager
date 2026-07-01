using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.GUI {
	public static class TooltipCommon {
		public static GodotObject? MakeCustomTooltip(string forText) {
			if (string.IsNullOrWhiteSpace(forText)) return null;
			PackedScene tt = (PackedScene)GD.Load("res://popups/parts/tooltip.tscn");
			MovingRichTextLabel text = tt.Instantiate<MovingRichTextLabel>();
			text.Text = forText ?? string.Empty;
			return text;
		}

	}
}
