using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SBModManager.IO {

	/// <summary>
	/// Reads packed JSON from a stream.
	/// </summary>
	public static class SBPackedJsonReader {

		/// <summary>
		/// Reads binary/packed JSON from the provided <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <returns></returns>
		public static GDDictionary ReadJson(Stream stream) {
			using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
			return ReadNextJsonObject(reader);
		}

		/// <summary>
		/// Reads a binary/packed JSON object from the provided <see cref="BinaryReader"/>.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static GDDictionary ReadNextJsonObject(BinaryReader data) {
			long length = SBStreamReader.Read7BitEncodedInt64BE(data);
			GDDictionary resultDict = [];
			for (long i = 0; i < length; i++) {
				resultDict[SBStreamReader.ReadDynLengthString(data)] = ReadNextBinJsonValue(data);
			}
			return resultDict;
		}

		private static Variant ReadNextBinJsonValue(BinaryReader data) {
			byte type = data.ReadByte();
			if (type > 0) type--;
			switch (type) {
				case 1:
					return data.ReadDouble();
				case 2:
					return data.ReadBoolean();
				case 3:
					return SBStreamReader.Read7BitEncodedInt64BE(data);
				case 4:
					return SBStreamReader.ReadDynLengthString(data); // Same code.
				case 5:
					long length = SBStreamReader.Read7BitEncodedInt64BE(data);
					GDArray resultArr = [];
					for (long i = 0; i < length; i++) {
						resultArr.Add(ReadNextBinJsonValue(data));
					}
					return resultArr;
				case 6:
					return ReadNextJsonObject(data);
				default:
					return default;
			}

		}

	}
}
