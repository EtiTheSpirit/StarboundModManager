using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SBModManager.IO;
using SBModManager.ModInstances;

namespace SBModManager.Other {

	/// <summary>
	/// Contains some special cases for use in SBMM
	/// </summary>
	public static class CompatibilityDetector {

		/// <summary>
		/// Returns messages for specific mods being installed.
		/// </summary>
		/// <param name="modID">The ID ("name" field) of the mod being tested.</param>
		/// <param name="specialCase">If a special note exists, this is the note.</param>
		/// <returns></returns>
		public static bool TryGetSpecialCaseFor(string modID, [NotNullWhen(true)] out string? specialCase) {
			// TO FUTURE ANYONE:
			// I'm actually really iffy about adding this feature.
			// SB modders are notoriously passive-aggressive and toxic to each other over incompatibilities. Adding them here seems like
			// a great way to use an otherwise harmless mod manager as a weapon for petty arguments over who's doing what wrong.

			// Basically, if you think you have something to PR into here: No, you don't.

			if (modID == "Futara's Dragon Pixel Full Bright Shader") {
				specialCase = $"OpenStarbound comes with [color=#E094FE]{modID}[/color] as a built-in feature. You can still have this installed, but it won't do anything.";
				return true;
			}
			specialCase = null;
			return false;
		}

		/// <summary>
		/// Asynchronously enumerates over the entire modpack and finds out which mods have conflicting tile IDs, fluid IDs, and/or matmod IDs.
		/// </summary>
		/// <param name="modpack">The modpack to check.</param>
		/// <returns></returns>
		public static IDCompatibilityResult GetTileAndFluidIncompatibilities(Modpack modpack) {
			ArgumentNullException.ThrowIfNull(modpack);

			IDCompatibilityResult result = new IDCompatibilityResult();
			Parallel.ForEach(modpack.ModSources.Keys, delegate (ModSource source) {
				foreach (ModArchive archive in source.Mods) {
					lock (result) {
						result.Add(archive);
					}
				}
			});

			return result;
		}

		private static byte? GetAsByte(GDDictionary dictionary, string key) {
			if (dictionary.TryGetValue(key, out Variant valueVar)) {
				if (valueVar.VariantType == Variant.Type.Int || valueVar.VariantType == Variant.Type.Float) {
					long value64 = (long)valueVar;
					if (value64 >= 0 && value64 <= byte.MaxValue) {
						return (byte)value64;
					}
				}
			}
			return null;
		}

		private static ushort? GetAsUInt16(GDDictionary dictionary, string key) {
			if (dictionary.TryGetValue(key, out Variant valueVar)) {
				if (valueVar.VariantType == Variant.Type.Int || valueVar.VariantType == Variant.Type.Float) {
					long value64 = (long)valueVar;
					if (value64 >= 0 && value64 <= ushort.MaxValue) {
						return (ushort)value64;
					}
				}
			}
			return null;
		}

		public class IDCompatibilityResult {

			/// <summary>
			/// True if any conflicts are present.
			/// </summary>
			public bool HasConflicts => LiquidConflicts.Count > 0 || MaterialConflicts.Count > 0 || MatModConflicts.Count > 0;

			/// <summary>
			/// An array of every mod ID that is involved in a conflict in some way. Stores <see cref="ModMetadata.ModID"/>.
			/// </summary>
			public HashSet<ModArchive> ModsInvolved { get; } = [];

			/// <summary>
			/// A dictionary of every conflict. Keys are the material's ID, values are a list of the mod ID that registers the liquid and said material's name.
			/// </summary>
			public Dictionary<ushort, Dictionary<ModArchive, string>> MaterialConflicts { get; } = [];

			/// <summary>
			/// A dictionary of every conflict. Keys are the liquid's ID, values are a list of the mod ID that registers the liquid and said liquid's name.
			/// </summary>
			public Dictionary<byte, Dictionary<ModArchive, string>> LiquidConflicts { get; } = [];

			/// <summary>
			/// A dictionary of every conflict. Keys are the matmod's ID, values are a list of the mod ID that registers the liquid and said matmod's name.
			/// </summary>
			public Dictionary<ushort, Dictionary<ModArchive, string>> MatModConflicts { get; } = [];

			/// <summary>
			/// Returns a copy of this instance where all dictionaries are clones.
			/// </summary>
			/// <returns></returns>
			public IDCompatibilityResult Duplicate() {
				IDCompatibilityResult result = new IDCompatibilityResult();
				foreach (ModArchive mod in ModsInvolved) {
					result.ModsInvolved.Add(mod);
				}
				foreach (KeyValuePair<ushort, Dictionary<ModArchive, string>> binding in MaterialConflicts) {
					result.MaterialConflicts[binding.Key] = binding.Value.ToDictionary();
				}
				foreach (KeyValuePair<byte, Dictionary<ModArchive, string>> binding in LiquidConflicts) {
					result.LiquidConflicts[binding.Key] = binding.Value.ToDictionary();
				}
				foreach (KeyValuePair<ushort, Dictionary<ModArchive, string>> binding in MatModConflicts) {
					result.MatModConflicts[binding.Key] = binding.Value.ToDictionary();
				}
				return result;
			}

			/// <summary>
			/// Returns a lookup where keys are <see cref="ModMetadata.ModID"/>, and values are pre-written strings explaining incompatibilities.
			/// This is intended to be shown in tooltips.
			/// </summary>
			/// <param name="modpack">If provided, disable mods in this pack are not considered.</param>
			/// <returns></returns>
			public Dictionary<ModArchive, List<string>> GetConflictsForModTooltips(Modpack? modpack, bool bbcode) {
				Dictionary<ModArchive, List<string>> result = [];
				CommonBuildAlternateLookup(result, EnumerateEnabledConflicts(modpack, MaterialConflicts), "Material", bbcode);
				CommonBuildAlternateLookup(result, EnumerateEnabledConflicts(modpack, LiquidConflicts), "Liquid", bbcode);
				CommonBuildAlternateLookup(result, EnumerateEnabledConflicts(modpack, MatModConflicts), "Matmod", bbcode);
				return result;
			}

			private static void CommonBuildAlternateLookup<TID>(Dictionary<ModArchive, List<string>> result, IEnumerable<(TID, IEnumerable<(ModArchive mod, string name)>)> lookup, string type, bool bbcode) {
				foreach (var incompat in lookup) {
					foreach ((ModArchive mod, string name) in incompat.Item2) {
						List<string> reports = result.GetOrAdd(mod, static key => []);
						foreach ((ModArchive otherMod, string otherName) in incompat.Item2.Where(kvp => kvp.mod != mod)) {
							// Holy mother of spaghetti.
							if (bbcode) {
								reports.Add($"{type} [color=#acf]{name}[/color] (ID [color=#afa]{incompat.Item1}[/color]) conflicts with [color=#acf]{otherName}[/color] from [color=#aff]{otherMod.Metadata.SBMMFriendlyNameNoMarkup}[/color].");
							} else {
								reports.Add($"{type} '{name}' (ID {incompat.Item1}) conflicts with '{otherName}' from '{otherMod.Metadata.SBMMFriendlyNameNoMarkup}'.");
							}
						}
					}
				}
			}

			/// <summary>
			/// Modifies this result to factor in the provided mod archive.
			/// </summary>
			/// <param name="archive"></param>
			public void Add(ModArchive mod) {
				if (ModsInvolved.Add(mod)) {
					if (mod.IsDirectory) {
						CommonAddProcedureOuter(
							mod,
							Directory.GetFiles(mod.AbsolutePath, "*.*", SearchOption.AllDirectories),
							File.ReadAllText,
							MaterialConflicts,
							LiquidConflicts,
							MatModConflicts
						);
					} else {
						using SBPackStream packFile = new SBPackStream(File.OpenRead(mod.AbsolutePath), false);
						CommonAddProcedureOuter(
							mod,
							packFile.GetFiles(),
							packFile.ReadFileAsString,
							MaterialConflicts,
							LiquidConflicts,
							MatModConflicts
						);
					}
				} else {
					throw new InvalidOperationException($"Mod {mod.Metadata.ModID} is already added to the ID compatibility list.");
				}
			}

			/// <summary>
			/// Modifies this result to no longer factor in the provided mod archive.
			/// </summary>
			/// <param name="archive"></param>
			public void Remove(ModArchive archive) {
				ModsInvolved.Remove(archive);
				foreach (KeyValuePair<ushort, Dictionary<ModArchive, string>> binding in MaterialConflicts.ToArray()) {
					binding.Value.Remove(archive);
					if (binding.Value.Count == 0) {
						MaterialConflicts.Remove(binding.Key);
					}
				}
				foreach (KeyValuePair<byte, Dictionary<ModArchive, string>> binding in LiquidConflicts.ToArray()) {
					binding.Value.Remove(archive);
					if (binding.Value.Count == 0) {
						LiquidConflicts.Remove(binding.Key);
					}
				}
				foreach (KeyValuePair<ushort, Dictionary<ModArchive, string>> binding in MatModConflicts.ToArray()) {
					binding.Value.Remove(archive);
					if (binding.Value.Count == 0) {
						MatModConflicts.Remove(binding.Key);
					}
				}
			}

			private static void CommonAddProcedureOuter(ModArchive mod, IEnumerable<string> files, Func<string, string> readFile, Dictionary<ushort, Dictionary<ModArchive, string>> materialConflicts, Dictionary<byte, Dictionary<ModArchive, string>> liquidConflicts, Dictionary<ushort, Dictionary<ModArchive, string>> matmodConflicts) {
				foreach (string path in files) {
					string extension = Path.GetExtension(path);

					if (extension == ".liquid") {
						CommonAddProcedure(
							mod,
							path,
							"liquidId",
							"name", // Liquids use "name" not "liquidName".
							readFile,
							liquidConflicts,
							GetAsByte
						);
					} else if (extension == ".matmod") {
						CommonAddProcedure(
							mod,
							path,
							"modId",
							"modName",
							readFile,
							matmodConflicts,
							GetAsUInt16
						);
					} else if (extension == ".material") {
						CommonAddProcedure(
							mod,
							path,
							"materialId",
							"materialName",
							readFile,
							materialConflicts,
							GetAsUInt16
						);
					}
				}
			}

			private static void CommonAddProcedure<TID>(ModArchive mod, string path, string idKey, string nameKey, Func<string, string> getJson, Dictionary<TID, Dictionary<ModArchive, string>> storage, Func<GDDictionary, string, TID?> getID) where TID : struct {
				Variant readJson = StarboundJsonSanitizer.ParseString(getJson(path));
				if (readJson.VariantType == Variant.Type.Dictionary) {
					GDDictionary asDictionary = (GDDictionary)readJson;
					TID? id = getID(asDictionary, idKey);
					string name = asDictionary.GetValueAsStringOrDefault(nameKey, Path.GetFileNameWithoutExtension(path));
					if (id != null) {
						Dictionary<ModArchive, string> data = storage.GetOrAdd(id.Value, static key => []);
						if (!data.ContainsValue(name)) {
							// Duplicate names are acceptable because it indicates an intentional overwrite.
							data.Add(mod, name);
						}
					}
				}
			}

			/// <summary>
			/// Writes a formatted block of text to the provided <paramref name="writer"/> detailing any conflicts.
			/// </summary>
			/// <param name="modpack">If not null, this modpack is used to figure out if a mod is enabled or not.</param>
			/// <param name="writer">The destination for the formatted string.</param>
			public void WriteReviewToString(Modpack? modpack, TextWriter writer) {
				if (HasConflicts) {
					IndentedTextWriter indented = new IndentedTextWriter(writer);
					indented.WriteLine("ID conflicts were detected!");
					{
						indented.Indent++;
						indented.WriteLine("Material Conflicts:");
						{
							indented.Indent++;
							if (MaterialConflicts.Count > 0) {
								foreach (var binding in EnumerateEnabledConflicts(modpack, MaterialConflicts)) {
									indented.WriteLine($"Material ID {binding.Item1} is used by...");
									indented.Indent++;
									foreach (var (modID, name) in binding.Item2) {
										indented.WriteLine($"...[{modID}] as {name}");
									}
									indented.Indent--;
								}
							} else {
								indented.WriteLine("No material conflicts occurred.");
							}
							indented.Indent--;
						}

						indented.WriteLine("Liquid Conflicts:");
						{
							indented.Indent++;
							if (LiquidConflicts.Count > 0) {
								foreach (var binding in EnumerateEnabledConflicts(modpack, LiquidConflicts)) {
									indented.WriteLine($"Liquid ID {binding.Item1} is used by...");
									indented.Indent++;
									foreach (var (modID, name) in binding.Item2) {
										indented.WriteLine($"...[{modID}] as {name}");
									}
									indented.Indent--;
								}
							} else {
								indented.WriteLine("No liquid conflicts occurred.");
							}
							indented.Indent--;
						}

						indented.WriteLine("MatMod (Ore) Conflicts:");
						{
							indented.Indent++;
							if (MatModConflicts.Count > 0) {
								foreach (var binding in EnumerateEnabledConflicts(modpack, MatModConflicts)) {
									indented.WriteLine($"MatMod ID {binding.Item1} is used by...");
									indented.Indent++;
									foreach (var (modID, name) in binding.Item2) {
										indented.WriteLine($"...[{modID}] as {name}");
									}
									indented.Indent--;
								}
							} else {
								indented.WriteLine("No MatMod conflicts occurred.");
							}
							indented.Indent--;
						}
						indented.Indent--;
					}
				} else {
					writer.WriteLine("No ID conflicts were detected.");
				}
			}

			/// <summary>
			/// Helper method that enumerates over the provided <paramref name="source"/>, but only factors in enabled mods.
			/// </summary>
			/// <typeparam name="TID"></typeparam>
			/// <param name="source"></param>
			/// <returns></returns>
			private static IEnumerable<(TID, IEnumerable<(ModArchive mod, string name)>)> EnumerateEnabledConflicts<TID>(Modpack? modpack, Dictionary<TID, Dictionary<ModArchive, string>> source) where TID : struct {
				foreach (KeyValuePair<TID, Dictionary<ModArchive, string>> container in source) {
					yield return (container.Key, EnumerateEnabledConflicts(modpack, container.Value));
				}
			}

			private static IEnumerable<(ModArchive mod, string name)> EnumerateEnabledConflicts(Modpack? modpack, Dictionary<ModArchive, string> conflictHavers) {
				foreach (KeyValuePair<ModArchive, string> conflictHaver in conflictHavers) {
					if (modpack == null) {
						yield return (conflictHaver.Key, conflictHaver.Value);
					} else if (modpack.ModSources.TryGetValue(conflictHaver.Key.Owner, out bool isEnabled)) {
						if (isEnabled) {
							yield return (conflictHaver.Key, conflictHaver.Value);
						}
					}
				}
			}

		}

	}
}
