using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

using SBModManager.Attributes;

namespace SBModManager.Menus.Windows {

	/// <summary>
	/// The base class for a preset <see cref="ConfirmationDialog"/> which allows prompting in the form of a <see cref="Task{TResult}"/>.
	/// </summary>
	public partial class TaskConfirmationDialog : ConfirmationDialog {

		[Import, AllowNull]
		public RichTextLabel RichTextLabel { get; }

		private TaskCompletionSource<bool>? _tcs;
		private string? _pendingText;
		private string? _pendingConfirmText;
		private string? _pendingTitle;

		public override void _Ready() {
			ImportAttribute.ImportAll(this);
			if (_pendingText != null) {
				RichTextLabel.Text = _pendingText;
			}
			if (_pendingConfirmText != null) {
				GetOkButton().Text = _pendingConfirmText;
			}
			if (_pendingTitle != null) {
				Title = _pendingTitle;
			}
			Show();
		}

		/// <summary>
		/// Shows this popup, and then waits until an option is selected, returning <see langword="true"/>
		/// if accept was clicked, and <see langword="false"/> if cancel was clicked or the dialog was exited.
		/// </summary>
		/// <param name="customText">Fully custom text to display. Can have bbcode.</param>
		/// <returns></returns>
		public Task<bool> ShowAndGetResultCustomAsync(string customText, string title, string confirmText) {
			if (_tcs != null) throw new InvalidOperationException("Make a new dialog, don't reuse an old one.");
			_tcs = new TaskCompletionSource<bool>();
			_pendingText = customText;
			_pendingConfirmText = confirmText;
			_pendingTitle = title;
			if (IsNodeReady()) {
				Title = title;
				GetOkButton().Text = confirmText;
				RichTextLabel.Text = _pendingText;
				Show();
			}
			Confirmed += delegate {
				_tcs.SetResult(true);
				QueueFree();
			};
			Canceled += delegate {
				_tcs.SetResult(false);
				QueueFree();
			};
			CloseRequested += delegate {
				_tcs.TrySetResult(false); // TrySet because confirm/cancel take precedence.
				QueueFree();
			};
			return _tcs.Task;
		}

	}
}
