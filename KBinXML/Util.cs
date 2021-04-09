using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace KBinXML {

	internal static class Util {

		private static readonly Dictionary<NodeType, string> ToTypeNameMap = new();
		private static readonly Dictionary<string, NodeType> FromTypeNameMap = new();
		
		static Util() {
			foreach (NodeType value in Enum.GetValues(typeof(NodeType))) {
				var attribute = typeof(NodeType).GetMember(value.ToString())[0].GetCustomAttribute<NamesAttribute>();
				if(attribute == null) continue;
				
				ToTypeNameMap.Add(value, attribute.Name);
				
				foreach (var name in attribute.Names) {
					FromTypeNameMap.Add(name, value);
				}
			}
		}
		
		public static void Assert(bool condition) {
			if (!condition) {
				throw new Exception("Assertion failed.");
			}
		}

		#region Stream

		public static void WriteNodeType(this Stream stream, NodeType nodeType) {
			stream.WriteByte((byte) nodeType);
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

		public static void WriteString(this Stream stream, string value, Encoding encoding) {
			if (stream.CanWrite) {
				value += "\0";
				
				var data = encoding switch {
					Encoding.ASCII => System.Text.Encoding.GetEncoding(20127).GetBytes(value),
					Encoding.ISO88591 => System.Text.Encoding.GetEncoding(28591).GetBytes(value),
					Encoding.EUCJP => System.Text.Encoding.GetEncoding(51932).GetBytes(value),
					Encoding.ShiftJIS => System.Text.Encoding.GetEncoding(932).GetBytes(value),
					Encoding.UTF8 => System.Text.Encoding.GetEncoding(65001).GetBytes(value),
					_ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
				};

				
				stream.WriteUInt32((uint) data.Length, Endianness.BigEndian);
				stream.Write(data, 0, data.Length);
				stream.Realign();
			}
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

		public static void WriteUInt32(this Stream stream, uint value, Endianness endianness) {
			if (stream.CanWrite) {
				var data = BitConverter.GetBytes(value);

				if (endianness == Endianness.BigEndian) {
					Array.Reverse(data);
				}
				
				stream.Write(data);
			}
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
		
		#endregion
		
		public static string ToTypeName(this NodeType nodeType) {
			return ToTypeNameMap.TryGetValue(nodeType, out var name) ? name : nodeType.ToString();
		}

		public static NodeType FromTypeName(string nodeType) {
			return FromTypeNameMap[nodeType];
		}
		
	}

	public enum Endianness {

		BigEndian,
		LittleEndian

	}
	
}