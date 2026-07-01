using System;
using System.Collections.Generic;
using System.Text;

namespace SBModManager.GUI {
	public sealed partial class TooltipImageButton : TextureButton {

		public override GodotObject? _MakeCustomTooltip(string forText) => TooltipCommon.MakeCustomTooltip(forText);
	}
}
