using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBinXML {

	public static class Util {

		public static void Assert(bool condition) {
			if (!condition) {
				throw new Exception("Assertion failed.");
			}
		}

		public static void Realign(this Stream stream, int size = 4) {
			while (stream.Position % size > 0) {
				stream.Position++;
			}
		}
		
		public static byte[] Read(this Stream stream, int length) {
			if (stream.CanRead) {
				var buffer = new byte[length];

				stream.Read(buffer, 0, length);

				return buffer;
			}

			return new byte[0];
		}

		public static string ReadString(this Stream stream, int length, Encoding encoding) {
			if (stream.CanRead) {
				var buffer = new byte[length];

				stream.Read(buffer, 0, length);

				return encoding switch {
					Encoding.ASCII => System.Text.Encoding.GetEncoding(20127).GetString(buffer),
					Encoding.EUCJP => System.Text.Encoding.GetEncoding(51932).GetString(buffer),
					Encoding.UTF8 => System.Text.Encoding.GetEncoding(65001).GetString(buffer),
					Encoding.ISO88591 => System.Text.Encoding.GetEncoding(28591).GetString(buffer),
					Encoding.ShiftJIS => System.Text.Encoding.GetEncoding(932).GetString(buffer),
					_ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
				};
			}
			
			return "";
		}

		public static string ReadString(this Stream stream, Encoding encoding) {
			var ret = stream.ReadString((int) stream.ReadUInt32(Endianness.BigEndian), encoding);
			
			stream.Realign();

			return ret.Trim('\0');
		}
		
		public static byte ReadUInt8(this Stream stream) {
			if (stream.CanRead) {
				return (byte) stream.ReadByte();
			}

			return 0;
		}

		public static uint ReadUInt32(this Stream stream, Endianness endianness) {
			if (stream.CanRead) {
				return stream.ReadUInt32(1, endianness)[0];
			}

			return 0;
		}
		
		public static uint[] ReadUInt32(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(uint), endianness, BitConverter.ToUInt32);
		}

		public static int[] ReadSInt32(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(int), endianness, BitConverter.ToInt32);
		}

		public static ulong[] ReadUInt64(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(ulong), endianness, BitConverter.ToUInt64);
		}
		
		public static long[] ReadSInt64(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(long), endianness, BitConverter.ToInt64);
		}

		public static float[] ReadSingle(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(float), endianness, BitConverter.ToSingle);
		}
		
		public static double[] ReadDouble(this Stream stream, int count, Endianness endianness) {
			return stream.ReadArray(count, sizeof(double), endianness, BitConverter.ToDouble);
		}

		public static string[] ReadIP4(this Stream stream, int count) {
			if (stream.CanRead) {
				var ips = new string[count];
				var buffer = new byte[4 * count];
				stream.Read(buffer, 0, buffer.Length);

				for (var i = 0; i < count; i++) {
					ips[i] = string.Join(".", buffer[(i * 4)..(i * 4 + 4)]);
				}

				return ips;
			}

			return Array.Empty<string>();
		}

		private static T[] ReadArray<T>(this Stream stream, int count, int size, Endianness endianness, Func<byte[], int, T> converter) {
			if (stream.CanRead) {
				var buffer = new byte[size * count];
				var returnData = new T[count];
				
				stream.Read(buffer, 0, buffer.Length);

				for (var i = 0; i < count; i++) {
					var temp = buffer[(i * size)..(i * size + size)];
					
					if (endianness == Endianness.BigEndian) {
						Array.Reverse(temp);
					}

					returnData[i] = converter(temp, 0);
				}
				
				return returnData;
			}

			return Array.Empty<T>();
		}
		
		public static string ToTypeName(this NodeType nodeType) {
			return nodeType switch {
				NodeType.S8 => "s8",
				NodeType.S8X2 => "2s8",
				NodeType.S8X3 => "3s8",
				NodeType.S8X4 => "4s8",
				NodeType.VS8 => "vs8",

				NodeType.U8 => "u8",
				NodeType.U8X2 => "2u8",
				NodeType.U8X3 => "3u8",
				NodeType.U8X4 => "4u8",
				NodeType.VU8 => "vu8",
				
				NodeType.S16 => "s16",
				NodeType.S16X2 => "2s16",
				NodeType.S16X3 => "3s16",
				NodeType.S16X4 => "4s16",
				NodeType.VS16 => "vs16",
				
				NodeType.U16 => "u16",
				NodeType.U16X2 => "2u16",
				NodeType.U16X3 => "3u16",
				NodeType.U16X4 => "4u16",
				NodeType.VU16 => "vu16",
				
				NodeType.S32 => "s32",
				NodeType.S32X2 => "2s32",
				NodeType.S32X3 => "3s32",
				NodeType.S32X4 => "4s32",
				
				NodeType.U32 => "u32",
				NodeType.U32X2 => "2u32",
				NodeType.U32X3 => "3u32",
				NodeType.U32X4 => "4u32",
				
				NodeType.S64 => "s64",
				NodeType.S64X2 => "2s64",
				NodeType.S64X3 => "3s64",
				NodeType.S64X4 => "4s64",
				
				NodeType.U64 => "u64",
				NodeType.U64X2 => "2u64",
				NodeType.U64X3 => "3u64",
				NodeType.U64X4 => "4u64",
				
				NodeType.Single => "float",
				NodeType.SingleX2 => "2f",
				NodeType.SingleX3 => "3f",
				NodeType.SingleX4 => "4f",
				
				NodeType.Double => "double",
				NodeType.DoubleX2 => "2d",
				NodeType.DoubleX3 => "3d",
				NodeType.DoubleX4 => "4d",
				
				NodeType.Bool => "bool",
				NodeType.BoolX2 => "2b",
				NodeType.BoolX3 => "3b",
				NodeType.BoolX4 => "4b",
				NodeType.VB => "vb",
				
				NodeType.IP4 => "ip4",
				NodeType.Binary => "binary",
				NodeType.String => "string",

				_ => nodeType.ToString(),
			};
		}

	}

	public enum Endianness {

		BigEndian,
		LittleEndian

	}
	
}