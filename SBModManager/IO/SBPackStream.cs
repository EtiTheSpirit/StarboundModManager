using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using SBModManager.ModInstances;
using SBModManager.Other;

namespace SBModManager.IO {

	/// <summary>
	/// Reads Starbound .pak files.
	/// </summary>
	public sealed class SBPackStream : IDisposable {

		/// <summary>
		/// Binds file paths to their offset in the file.
		/// </summary>
		private readonly Dictionary<string, (long offset, long size)> _filesByName = [];

		/// <summary>
		/// The location in the stream passed into the constructor where reading began. This isn't likely to really be used,
		/// but it future-proofs reading pak files directly from another buffer inline.
		/// </summary>
		private readonly long _startedReadingAt = 0;

		/// <summary>
		/// The data of the pack file. This uses unmanaged memory because pak files can be greater than .NET's maximum array length.
		/// </summary>
		private readonly Stream _pakFile;

		/// <summary>
		/// If <see langword="true"/>, disposing of this reader will not dispose of the underlying stream.
		/// </summary>
		private readonly bool _keepOpen;

		/// <summary>
		/// The mod's metadata.
		/// </summary>
		public ModMetadata Metadata { get; }

		/// <summary>
		/// Reads the entirety of a .pak file and stores its contents in memory.
		/// </summary>
		/// <param name="pak"></param>
		public SBPackStream(Stream pak, bool keepOpen = false) {
			ArgumentNullException.ThrowIfNull(pak);
			if (!pak.CanRead) throw new NotSupportedException("The stream must be readable.");
			if (!pak.CanSeek) throw new NotSupportedException("The stream must be seekable.");
			_pakFile = pak;
			_keepOpen = keepOpen;

			_startedReadingAt = pak.Position;
			using BinaryReader reader = new BinaryReader(pak, Encoding.UTF8, true);
			const ulong SBASSET6 = 3923872721875845715UL; // The sequence of characters "SBASSET6" as a little endian integer.
			if (reader.ReadUInt64() != SBASSET6) {
				throw new InvalidDataException("Not a valid Starbound .pak file.");
			}

			long filePtr = BinaryPrimitives.ReverseEndianness(reader.ReadInt64());
			pak.Seek(_startedReadingAt + filePtr, SeekOrigin.Begin);

			const ulong INDE = 1162104393; // The sequence of characters "INDE" as a little endian integer.
			const byte X = 88; // The letter "X" as a little endian integer.
			uint inde = reader.ReadUInt32();
			byte x = reader.ReadByte();
			if (inde != INDE || x != X) {
				throw new InvalidDataException($"Not a valid Starbound .pak file; expected INDEX block at position {_startedReadingAt + filePtr:X16} in underlying stream.");
			}

			GDDictionary metadata = SBPackedJsonReader.ReadJson(pak);
			long indexSize = SBStreamReader.Read7BitEncodedInt64BE(reader);
			while (indexSize-- > 0) {
				string fileName = SBStreamReader.ReadDynLengthString(reader);
				long fileLocation = BinaryPrimitives.ReverseEndianness(reader.ReadInt64());
				long fileSize = BinaryPrimitives.ReverseEndianness(reader.ReadInt64());
				_filesByName[fileName] = (fileLocation, fileSize);

				long orgPos = pak.Position;
				if (fileName == "/_previewimage") {
					byte[] buffer = new byte[fileSize];
					pak.Seek(_startedReadingAt + fileLocation, SeekOrigin.Begin);
					pak.ReadExactly(buffer);
					pak.Seek(orgPos, SeekOrigin.Begin);

					Image image = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8);
					Error loadError = image.LoadPngFromBuffer(buffer);
					if (loadError != Error.Ok) {
						loadError = image.LoadJpgFromBuffer(buffer);
					}
					if (loadError != Error.Ok) {
						loadError = image.LoadGifFirstFrameFromBuffer(buffer);
					}
					if (loadError == Error.Ok) {
						metadata["preview_image"] = ImageTexture.CreateFromImage(image);
					}
				}
			}
			Metadata = new ModMetadata("<no name>", metadata, 0);
		}

		/// <summary>
		/// Reads a file at the provided path, which generally starts with a <c>/</c> character.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">No such file was present.</exception>
		public byte[] ReadFile(string path) {
			(long offset, long size) = _filesByName[path]; // KeyNotFound can propagate.
			byte[] buffer = new byte[size];
			long oldPosition = _pakFile.Position;
			_pakFile.Seek(_startedReadingAt + offset, SeekOrigin.Begin);
			_pakFile.ReadExactly(buffer);
			_pakFile.Seek(oldPosition, SeekOrigin.Begin);
			return buffer;
		}

		/// <summary>
		/// Wraps <see cref="ReadFile"/> around <see cref="Encoding.GetString(byte[])"/> from <see cref="Encoding.UTF8"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string ReadFileAsString(string path) => Encoding.UTF8.GetString(ReadFile(path));

		/// <summary>
		/// The name of every file in this .pak
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetFiles() => _filesByName.Keys;

		public void Dispose() {
			_filesByName.Clear();
			if (!_keepOpen) {
				_pakFile.Dispose();
			}
		}

		/// <summary>
		/// A segment of the stream which a .pak file was read from.
		/// </summary>
		private sealed class StreamSegment : Stream {

			/// <summary>
			/// The underlying <see cref="Stream"/> to access.
			/// </summary>
			public Stream BaseStream { get; }

			/// <summary>
			/// The position in the <see cref="BaseStream"/> where this section begins.
			/// </summary>
			public long Start { get; }
			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => false;
			public override long Length { get; }

			/// <summary>
			/// The position of this segment. May return a negative value, or a value greater than <see cref="Length"/>.
			/// </summary>
			public override long Position {
				get => BaseStream.Position - Start;
				set => BaseStream.Position = value + Start;
			}

			public StreamSegment(Stream baseStream, long start, long length) {
				BaseStream = baseStream;
				Start = start;
				Length = length;
				if ((Start + Length) > BaseStream.Length) {
					throw new InvalidDataException("Segment reaches beyond the end of the stream.");
				}
			}

			public override int ReadByte() {
				if (Position >= Length || Position < 0) return -1;
				return base.ReadByte();
			}

			public override int Read(byte[] buffer, int offset, int count) {
				long available = long.Min(Start + Length - BaseStream.Position, buffer.Length - offset);
				available = long.Min(available, Length);
				if (available <= 0) return 0;
				count = (int)long.Min(count, available);
				if (count <= 0) return 0;

				for (int i = 0; i < count; i++) {
					buffer[i + offset] = (byte)BaseStream.ReadByte();
				}
				return count;
			}

			public override long Seek(long offset, SeekOrigin origin) {
				if (origin == SeekOrigin.Begin) {
					if (offset < 0 || offset >= Length) throw new IOException("Attempt to seek beyond the bounds of the stream.");
					return offset;
				} else if (origin == SeekOrigin.Current) {
					long newPosInBase = BaseStream.Position + offset;
					return Seek(newPosInBase - Start, SeekOrigin.Begin);
				} else if (origin == SeekOrigin.End) {
					if (offset < 0 || offset >= Length) throw new IOException("Attempt to seek beyond the bounds of the stream.");
					return Seek(Length - offset, SeekOrigin.Begin);
				} else {
					throw new ArgumentOutOfRangeException(nameof(offset));
				}
			}

			public override void SetLength(long value) {
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count) {
				throw new NotSupportedException();
			}

			public override void Flush() { }
		}
	}
}
