using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

using SBModManager.Attributes;

namespace SBModManager.Menus.Windows {

	/// <summary>
	/// This dialog opens when the user is about to delete a modpack, to prompt them to confirm.
	/// </summary>
	public partial class ConfirmDeleteDialog : TaskConfirmationDialog {

		private const string FORMAT = @"Are you sure you want to delete [b]{0}[/b]? [color=#f77]All characters and worlds in this modpack will be [b]permanently deleted[/b]. You cannot undo this action![/color]";

		/// <summary>
		/// Shows this popup, and then waits until an option is selected, returning <see langword="true"/>
		/// if accept was clicked, and <see langword="false"/> if cancel was clicked or the dialog was exited.
		/// </summary>
		/// <param name="modpackNameToDelete"></param>
		/// <returns></returns>
		public Task<bool> ShowAndGetResultAsync(string modpackNameToDelete) {
			return ShowAndGetResultCustomAsync(string.Format(FORMAT, modpackNameToDelete));
		}
	}
}
