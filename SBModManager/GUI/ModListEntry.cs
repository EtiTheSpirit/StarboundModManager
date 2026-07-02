using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

using SBModManager.Attributes;
using SBModManager.ModInstances;
using SBModManager.SteamInterop;

namespace SBModManager.GUI {

	/// <summary>
	/// Represents an entry in the mod list. Not to be confused with <see cref="ModPackEntry"/>, which is on the main screen.
	/// </summary>
	public partial class ModListEntry : ColorRect {

		[Import, AllowNull]
		public CheckButton EnableMod { get; }

		[Import, AllowNull]
		public TextureRect ModIcon { get; }

		[Import, AllowNull]
		public RichTextLabel ModNameAndAuthor { get; }

		[Import, AllowNull]
		public RichTextLabel ModVersionAndSize { get; }

		/// <summary>
		/// The mod that this represents.
		/// </summary>
		[AllowNull]
		public ModArchive Mod { get; private set; }

		/// <summary>
		/// The modpack that holds this mod.
		/// </summary>
		[AllowNull]
		public Modpack Pack { get; private set;  }

		public override void _Ready() {
			ImportAttribute.ImportAll(this);
			if (Pack != null && Mod != null) AssignModRoutine(Pack, Mod);

			EnableMod.Toggled += OnEnableModToggled;
		}

		private void OnEnableModToggled(bool toggledOn) {
			Pack.ModSources[Mod.Owner] = toggledOn;
			Modulate = new Color(1, 1, 1, toggledOn ? 1 : 0.5f);
		}

		public void AssignMod(Modpack pack, ModArchive mod) {
			Pack = pack;
			Mod = mod;
			if (!IsNodeReady()) return;
			AssignModRoutine(pack, mod);
		}

		private void AssignModRoutine(Modpack pack, ModArchive mod) {
			Pack = pack;
			Mod = mod;
			EnableMod.Disabled = !mod.IsExclusive;
			EnableMod.SetPressedNoSignal(mod.Owner.IsEnabledIn(pack));
			ModIcon.Texture = mod.Metadata.PreviewImage;
			
			if (EnableMod.Disabled) {
				EnableMod.TooltipText = "You can't disable this mod because it's part of a mod group.\n\nModders typically group their own mods together like this when the mods must be together by design.";
			} else {
				EnableMod.TooltipText = string.Empty;
			}

			string friendlyName = mod.Metadata.FriendlyName ?? string.Empty;
			string author = mod.Metadata.Author ?? string.Empty;
			string version = mod.Metadata.Version ?? string.Empty;

			if (!string.IsNullOrWhiteSpace(author)) {
				ModNameAndAuthor.Clear();
				ModNameAndAuthor.PushFontSize(16);
				ModNameAndAuthor.PushContext();
				ModNameAndAuthor.AppendText(FormatTools.ShittyStarboundMarkupToBBCode(friendlyName.Replace("\n", null).Replace("\r", null)));
				ModNameAndAuthor.PopContext();
				ModNameAndAuthor.Pop();
				ModNameAndAuthor.PushFontSize(10);
				ModNameAndAuthor.AppendText("\nby ");
				ModNameAndAuthor.PushColor(Colors.MediumSeaGreen);
				ModNameAndAuthor.PushContext();
				ModNameAndAuthor.AppendText(FormatTools.ShittyStarboundMarkupToBBCode(author.Replace("\n", null).Replace("\r", null)));
				ModNameAndAuthor.PopContext();
				ModNameAndAuthor.Pop();
				ModNameAndAuthor.AppendText(" - Hover for more information.");
				ModNameAndAuthor.Pop();
			} else {
				ModNameAndAuthor.Clear();
				ModNameAndAuthor.PushContext();
				ModNameAndAuthor.AppendText(FormatTools.ShittyStarboundMarkupToBBCode(friendlyName.Replace("\n", null).Replace("\r", null)));
				ModNameAndAuthor.PopContext();
				ModNameAndAuthor.PushFontSize(10);
				ModNameAndAuthor.AppendText("\nHover for more information.");
				ModNameAndAuthor.Pop();
			}
			if (!string.IsNullOrWhiteSpace(version)) {
				ModVersionAndSize.Clear();
				ModVersionAndSize.AppendText("Version ");
				ModVersionAndSize.PushColor(Colors.MediumSeaGreen);
				ModNameAndAuthor.PushContext();
				ModVersionAndSize.AppendText(FormatTools.ShittyStarboundMarkupToBBCode(version.Replace("\n", null).Replace("\r", null)));
				ModNameAndAuthor.PopContext();
				ModVersionAndSize.Pop();
				ModVersionAndSize.AppendText("\nSize: ");
				ModVersionAndSize.PushColor(Colors.Gray);
				ModVersionAndSize.AppendText(FormatTools.ToLargestSIUnitByteSize((ulong)mod.FileSizeBytes));
				ModVersionAndSize.Pop();
			} else {
				ModVersionAndSize.Clear();
				ModVersionAndSize.PushItalics();
				ModVersionAndSize.AppendText("No version information.");
				ModVersionAndSize.Pop();
				ModVersionAndSize.AppendText("\nSize: ");
				ModVersionAndSize.PushColor(Colors.Gray);
				ModVersionAndSize.AppendText(FormatTools.ToLargestSIUnitByteSize((ulong)mod.FileSizeBytes));
				ModVersionAndSize.Pop();
			}

			ModNameAndAuthor.TooltipText = "[font_size=10][color=#aaa][i]Use Page Up and Page Down to scroll...[/i][/color]\n\n[/font_size]";
			if (mod.IsDirectory) {
				Color = Colors.Wheat;
				ModNameAndAuthor.TooltipText += "[color=wheat]Unpacked mod![/color] This mod may take longer to load.\n\n";
			}

			string? description = mod.Metadata.SBMMFixedDescription;
			if (description == null) {
				if (!string.IsNullOrWhiteSpace(mod.Metadata.Description)) {
					description = ReparseStarboundIntoBBCode(mod.Metadata.Description);
				} else {
					description = "[i]No description was provided for this mod.[/i]";
				}
				mod.Metadata.SBMMFixedDescription = description;
			}
			ModNameAndAuthor.TooltipText += description;
		}

		/// <summary>
		/// I'm so sorry for the bullshit you're about to lay your eyes upon.
		/// </summary>
		/// <param name="sbOrWorkshopDesc"></param>
		/// <returns></returns>
		private static string ReparseStarboundIntoBBCode(string sbOrWorkshopDesc) {
			// Starbound markup:
			sbOrWorkshopDesc = FormatTools.ShittyStarboundMarkupToBBCode(sbOrWorkshopDesc);

			// Because some people like to use all caps bbcode...
			sbOrWorkshopDesc = sbOrWorkshopDesc.Replace("[B]", "[b]", StringComparison.OrdinalIgnoreCase).Replace("[/B]", "[/b]", StringComparison.OrdinalIgnoreCase)
												.Replace("[I]", "[i]", StringComparison.OrdinalIgnoreCase).Replace("[/I]", "[/i]", StringComparison.OrdinalIgnoreCase)
												.Replace("[U]", "[u]", StringComparison.OrdinalIgnoreCase).Replace("[/U]", "[/u]", StringComparison.OrdinalIgnoreCase)
												// .Replace("[IMG]", "[img]", StringComparison.OrdinalIgnoreCase).Replace("[/IMG]", "[/img]", StringComparison.OrdinalIgnoreCase)
												// .Replace("[URL]", "[url]", StringComparison.OrdinalIgnoreCase).Replace("[/URL]", "[/url]", StringComparison.OrdinalIgnoreCase)
												.Replace("[STRIKE]", "[s]", StringComparison.OrdinalIgnoreCase).Replace("[/STRIKE]", "[/s]", StringComparison.OrdinalIgnoreCase)
												.Replace("[LIST]", "[ul]", StringComparison.OrdinalIgnoreCase).Replace("[/LIST]", "[/ul]", StringComparison.OrdinalIgnoreCase)
												.Replace("[OLIST]", "[ol]", StringComparison.OrdinalIgnoreCase).Replace("[/OLIST]", "[/ol]", StringComparison.OrdinalIgnoreCase)
												.Replace("[*]", null, StringComparison.OrdinalIgnoreCase) // Used in lists
												.Replace("[/HR]", null, StringComparison.OrdinalIgnoreCase) // Godot doesn't use a closing tag.
												.Replace("[LI]", null, StringComparison.OrdinalIgnoreCase).Replace("[/LI]", null, StringComparison.OrdinalIgnoreCase);

			// For URL and IMG:
			sbOrWorkshopDesc = URLBBCodeResolver().Replace(sbOrWorkshopDesc, delegate (Match match) {
				if (!match.Success) return match.Value;
				if (match.Groups[0].Success) {
					return $"[url={match.Groups[0].Value}]{match.Groups[1].Value}[/url]";
				} else {
					return $"[url]{match.Groups[1].Value}[/url]";
				}
			});
			sbOrWorkshopDesc = IMGBBCodeResolver().Replace(sbOrWorkshopDesc, delegate (Match match) {
				if (!match.Success) return match.Value;
				// IMG has its first group set to a non-capturing group.
				return $"[img]{match.Groups[0].Value}[/img]";
			});

			// Steam Workshop formatting:
			sbOrWorkshopDesc = sbOrWorkshopDesc .Replace("[h1]", "[font_size=24]", StringComparison.OrdinalIgnoreCase).Replace("[/h1]", "[/font_size]", StringComparison.OrdinalIgnoreCase)
												.Replace("[h2]", "[font_size=20]", StringComparison.OrdinalIgnoreCase).Replace("[/h2]", "[/font_size]", StringComparison.OrdinalIgnoreCase)
												.Replace("[h3]", "[font_size=16]", StringComparison.OrdinalIgnoreCase).Replace("[/h3]", "[/font_size]", StringComparison.OrdinalIgnoreCase)
												.Replace("[h4]", "[font_size=14]", StringComparison.OrdinalIgnoreCase).Replace("[/h4]", "[/font_size]", StringComparison.OrdinalIgnoreCase)
												.Replace("[h5]", "[font_size=12]", StringComparison.OrdinalIgnoreCase).Replace("[/h5]", "[/font_size]", StringComparison.OrdinalIgnoreCase)
												.Replace("[h6]", "[font_size=10]", StringComparison.OrdinalIgnoreCase).Replace("[/h6]", "[/font_size]", StringComparison.OrdinalIgnoreCase);

			// Image Fixers
			// The idea here is to create a dummy texture and then download it in the background.
			return InlineThumbnailImageHelper.ReplaceImages(sbOrWorkshopDesc);
		}

		[GeneratedRegex(@"\[url(\=[^\]]+)?\]([^\[\]]+)\[\/url\]", RegexOptions.IgnoreCase)]
		private static partial Regex URLBBCodeResolver();

		[GeneratedRegex(@"\[img(?:\=[^\]]+)?\]([^\[\]]+)\[\/img\]", RegexOptions.IgnoreCase)]
		private static partial Regex IMGBBCodeResolver();
	}
}
