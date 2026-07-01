using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.GUI {
	public sealed partial class TooltipControl : Control {

		public override GodotObject? _MakeCustomTooltip(string forText) => TooltipCommon.MakeCustomTooltip(forText);
	}
}
