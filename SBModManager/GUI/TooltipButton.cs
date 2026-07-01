using System;
using System.Collections.Generic;
using System.Text;

using SBModManager.GUI;

namespace SBModManager {
	public sealed partial class TooltipButton : Button {

		public override GodotObject? _MakeCustomTooltip(string forText) => TooltipCommon.MakeCustomTooltip(forText);
	}
}
