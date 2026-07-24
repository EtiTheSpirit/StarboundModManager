using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SBModManager.IO {

	/// <summary>
	/// Tools to read stuff from Stream the way Starbound writes it.
	/// </summary>
	public static class SBStreamReader {

		/// <summary>
		/// Similar to <see cref="BinaryReader.Read7BitEncodedInt64"/> but with the opposite byte order. Used by Starbound for
		/// variable-length integers.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException">The </exception>
		internal static long Read7BitEncodedInt64BE(BinaryReader data) {
			// Almost identical to Read7BitEncodedInt64 but this has a reversed byte order.
			long x = 0;
			for (int i = 0; i < 10; ++i) {
				byte oct = data.ReadByte();
				x = (x << 7) | (long)(oct & 127);
				if ((oct & 128) == 0) {
					return x;
				}
			}
			throw new InvalidDataException();
		}

		/// <summary>
		/// Reads a length-prefixed string.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException">The length of the string is too long.</exception>
		public static string ReadDynLengthString(BinaryReader data) {
			long length = Read7BitEncodedInt64BE(data);
			if (length > int.MaxValue) {
				throw new InvalidDataException("String is too long. Is this data corrupted?");
			}
			Span<byte> utf8 = length < 512 ? stackalloc byte[(int)length] : new byte[length];
			data.ReadExactly(utf8);
			return Encoding.UTF8.GetString(utf8);
		}
	}
}
