using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace KBinXML {
	
	internal static class DataTypeHandlers {

		public new delegate string ToString(DataStream stream, int count);
		public delegate void FromString(DataStream stream, string value);
		
		public static readonly Dictionary<NodeType, ToStringHandler> ToStringMap = new();
		public static readonly Dictionary<NodeType, FromStringHandler> FromStringMap = new();

		static DataTypeHandlers() {
			var properties = typeof(DataTypeHandlers).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);
			foreach (var property in properties) {
				if (property.GetValue(null) is ToStringInternal toString) {
					foreach (var attribute in property.GetCustomAttributes<DataTypeHandlerAttribute>()) {
						ToStringMap.Add(attribute.Type, new ToStringHandler((stream, count) => toString(stream, count * attribute.Count), attribute));
					}
				}

				if (property.GetValue(null) is FromStringInternal fromString) {
					foreach (var attribute in property.GetCustomAttributes<DataTypeHandlerAttribute>()) {
						FromStringMap.Add(attribute.Type, new FromStringHandler((stream, value) => fromString(stream, value), attribute));
					}
				}
			}
		}

		#region ToString

		[DataTypeHandler(NodeType.U8, 1, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X2, 2, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X3, 3, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X4, 4, sizeof(byte))]
		[DataTypeHandler(NodeType.VU8, 16, sizeof(byte))]
		private static ToStringInternal ReadU8 => (stream, count) => string.Join(" ", stream.ReadUInt8(count));

		[DataTypeHandler(NodeType.S8, 1, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X2, 2, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X3, 3, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X4, 4, sizeof(sbyte))]
		[DataTypeHandler(NodeType.VS8, 16, sizeof(sbyte))]
		private static ToStringInternal ReadS8 => (stream, count) => string.Join(" ", stream.ReadSInt8(count));

		[DataTypeHandler(NodeType.U16, 1, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X2, 2, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X3, 3, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X4, 4, sizeof(ushort))]
		[DataTypeHandler(NodeType.VU16, 8, sizeof(ushort))]
		private static ToStringInternal ReadU16 => (stream, count) => string.Join(" ", stream.ReadUInt16(count, Endianness.BigEndian));
		
		[DataTypeHandler(NodeType.S16, 1, sizeof(short))]
		[DataTypeHandler(NodeType.S16X2, 2, sizeof(short))]
		[DataTypeHandler(NodeType.S16X3, 3, sizeof(short))]
		[DataTypeHandler(NodeType.S16X4, 4, sizeof(short))]
		[DataTypeHandler(NodeType.VS16, 8, sizeof(short))]
		private static ToStringInternal ReadS16 => (stream, count) => string.Join(" ", stream.ReadSInt16(count, Endianness.BigEndian));

		[DataTypeHandler(NodeType.S32, 1, sizeof(int))]
		[DataTypeHandler(NodeType.S32X2, 2, sizeof(int))]
		[DataTypeHandler(NodeType.S32X3, 3, sizeof(int))]
		[DataTypeHandler(NodeType.S32X4, 4, sizeof(int))]
		private static ToStringInternal ReadS32 => (stream, count) => string.Join(" ", stream.ReadSInt32(count, Endianness.BigEndian));

		[DataTypeHandler(NodeType.U32, 1, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X2, 2, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X3, 3, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X4, 4, sizeof(uint))]
		private static ToStringInternal ReadU32 => (stream, count) => string.Join(" ", stream.ReadUInt32(count, Endianness.BigEndian));

		[DataTypeHandler(NodeType.S64, 1, sizeof(long))]
		[DataTypeHandler(NodeType.S64X2, 2, sizeof(long))]
		[DataTypeHandler(NodeType.S64X3, 3, sizeof(long))]
		[DataTypeHandler(NodeType.S64X4, 4, sizeof(long))]
		private static ToStringInternal ReadS64 => (stream, count) => string.Join(" ", stream.ReadSInt64(count, Endianness.BigEndian));
		
		[DataTypeHandler(NodeType.U64, 1, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X2, 2, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X3, 3, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X4, 4, sizeof(ulong))]
		private static ToStringInternal ReadU64 => (stream, count) => string.Join(" ", stream.ReadUInt64(count, Endianness.BigEndian));

		[DataTypeHandler(NodeType.Single, 1, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX2, 2, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX3, 3, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX4, 4, sizeof(float))]
		private static ToStringInternal ReadSingle => (stream, count) => string.Join(" ", stream.ReadSingle(count, Endianness.BigEndian).Select(x => x.ToString("F5"))); // ugly way to format floats

		[DataTypeHandler(NodeType.Double, 1, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX2, 2, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX3, 3, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX4, 4, sizeof(double))]
		private static ToStringInternal ReadDouble => (stream, count) => string.Join(" ", stream.ReadDouble(count, Endianness.BigEndian).Select(x => x.ToString("F5")));

		[DataTypeHandler(NodeType.Bool, 1, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX2, 2, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX3, 3, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX4, 4, sizeof(bool))]
		[DataTypeHandler(NodeType.VB, 16, sizeof(bool))]
		private static ToStringInternal ReadBoolean => (stream, count) => string.Join(" ", stream.ReadUInt8(count));
		
		[DataTypeHandler(NodeType.IP4, 1, sizeof(byte) * 4)]
		private static ToStringInternal ReadIP4 => (stream, count) => string.Join(" ", stream.ReadIP4(count));

		[DataTypeHandler(NodeType.Time, 1, sizeof(byte) * 4)]
		private static ToStringInternal ReadTime => (stream, count) => string.Join(" ", stream.ReadUInt32(count, Endianness.BigEndian));
		
		#endregion

		#region FromString

		[DataTypeHandler(NodeType.U8, 1, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X2, 2, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X3, 3, sizeof(byte))]
		[DataTypeHandler(NodeType.U8X4, 4, sizeof(byte))]
		[DataTypeHandler(NodeType.VU8, 16, sizeof(byte))]
		private static FromStringInternal WriteU8 => (stream, value) => stream.WriteUInt8(value.Split(" ").Select(byte.Parse).ToArray());
		
		[DataTypeHandler(NodeType.S8, 1, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X2, 2, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X3, 3, sizeof(sbyte))]
		[DataTypeHandler(NodeType.S8X4, 4, sizeof(sbyte))]
		[DataTypeHandler(NodeType.VS8, 16, sizeof(sbyte))]
		private static FromStringInternal WriteS8 => (stream, value) => stream.WriteSInt8(value.Split(" ").Select(sbyte.Parse).ToArray());

		[DataTypeHandler(NodeType.U16, 1, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X2, 2, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X3, 3, sizeof(ushort))]
		[DataTypeHandler(NodeType.U16X4, 4, sizeof(ushort))]
		[DataTypeHandler(NodeType.VU16, 8, sizeof(ushort))]
		private static FromStringInternal WriteU16 => (stream, value) => stream.WriteUInt16(value.Split(" ").Select(ushort.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.S16, 1, sizeof(short))]
		[DataTypeHandler(NodeType.S16X2, 2, sizeof(short))]
		[DataTypeHandler(NodeType.S16X3, 3, sizeof(short))]
		[DataTypeHandler(NodeType.S16X4, 4, sizeof(short))]
		[DataTypeHandler(NodeType.VS16, 8, sizeof(short))]
		private static FromStringInternal WriteS16 => (stream, value) => stream.WriteSInt16(value.Split(" ").Select(short.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.U32, 1, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X2, 2, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X3, 3, sizeof(uint))]
		[DataTypeHandler(NodeType.U32X4, 4, sizeof(uint))]
		private static FromStringInternal WriteU32 => (stream, value) => stream.WriteUInt32(value.Split(" ").Select(uint.Parse).ToArray(), Endianness.BigEndian);
		
		[DataTypeHandler(NodeType.S32, 1, sizeof(int))]
		[DataTypeHandler(NodeType.S32X2, 2, sizeof(int))]
		[DataTypeHandler(NodeType.S32X3, 3, sizeof(int))]
		[DataTypeHandler(NodeType.S32X4, 4, sizeof(int))]
		private static FromStringInternal WriteS32 => (stream, value) => stream.WriteSInt32(value.Split(" ").Select(int.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.U64, 1, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X2, 2, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X3, 3, sizeof(ulong))]
		[DataTypeHandler(NodeType.U64X4, 4, sizeof(ulong))]
		private static FromStringInternal WriteU64 => (stream, value) => stream.WriteUInt64(value.Split(" ").Select(ulong.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.S64, 1, sizeof(long))]
		[DataTypeHandler(NodeType.S64X2, 2, sizeof(long))]
		[DataTypeHandler(NodeType.S64X3, 3, sizeof(long))]
		[DataTypeHandler(NodeType.S64X4, 4, sizeof(long))]
		private static FromStringInternal WriteS64 => (stream, value) => stream.WriteSInt64(value.Split(" ").Select(long.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.Single, 1, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX2, 2, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX3, 3, sizeof(float))]
		[DataTypeHandler(NodeType.SingleX4, 4, sizeof(float))]
		private static FromStringInternal WriteSingle => (stream, value) => stream.WriteSingle(value.Split(" ").Select(float.Parse).ToArray(), Endianness.BigEndian);
		
		[DataTypeHandler(NodeType.Double, 1, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX2, 2, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX3, 3, sizeof(double))]
		[DataTypeHandler(NodeType.DoubleX4, 4, sizeof(double))]
		private static FromStringInternal WriteDouble => (stream, value) => stream.WriteDouble(value.Split(" ").Select(double.Parse).ToArray(), Endianness.BigEndian);

		[DataTypeHandler(NodeType.Bool, 1, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX2, 2, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX3, 3, sizeof(bool))]
		[DataTypeHandler(NodeType.BoolX4, 4, sizeof(bool))]
		[DataTypeHandler(NodeType.VB, 16, sizeof(bool))]
		private static FromStringInternal WriteBool => (stream, value) => stream.WriteUInt8(value.Split(" ").Select(byte.Parse).ToArray());
		
		[DataTypeHandler(NodeType.IP4, 1, sizeof(byte) * 4)]
		private static FromStringInternal WriteIP4 => (stream, value) => stream.Write(value.Split(" ").SelectMany(x => x.Split(".").Select(byte.Parse)).ToArray());

		#endregion
		
		private delegate string ToStringInternal(DataStream stream, int size);
		private delegate void FromStringInternal(DataStream stream, string value);

	}

}