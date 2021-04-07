using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBinXML {

	public class DataStream : MemoryStream {

		internal long Position1 { get; set; }
		internal long Position2 { get; set; }

		internal DataStream(byte[] data) : base(data) {
				
		}
		
		internal byte[] GetAligned(int size) {
			if (Position1 % 4 == 0) {
				Position1 = Position;
			}

			if (Position2 % 4 == 0) {
				Position2 = Position;
			}

			var pos = Position;
			byte[] data;
			switch (size) {
				case 1:
					Seek(Position1++, SeekOrigin.Begin);
					data = new[] { (byte) ReadByte() };
					break;
				case 2:
					Seek(Position2, SeekOrigin.Begin);
					data = this.Read(2);
					Position2 += 2;
					break;
				default:
					data = this.Read(size);
					this.Realign();
					return data;
			}

			var trailing = Math.Max(Position1, Position2);
			if (pos < trailing) {
				Seek(trailing, SeekOrigin.Begin);
				this.Realign();
			} else {
				Seek(pos, SeekOrigin.Begin);
			}

			return data;
		}
		
		public byte[] ReadUInt8(int length) {
			if (CanRead) {
				return GetAligned(length * sizeof(byte));
			}

			return Array.Empty<byte>();
		}

		public sbyte[] ReadSInt8(int length) {
			if (CanRead) {
				return GetAligned(length * sizeof(sbyte)).Select(x => (sbyte) x).ToArray();
			}

			return Array.Empty<sbyte>();
		}

		public ushort[] ReadUInt16(int length, Endianness endianness) {
			if (CanRead) {
				var data = GetAligned(length * sizeof(ushort));
				var returnData = new ushort[length];
				
				for (var i = 0; i < length; i++) {
					var temp = data[(i * 2)..(i * 2 + 2)];

					if (endianness == Endianness.BigEndian) {
						Array.Reverse(temp);
					}

					returnData[i] = BitConverter.ToUInt16(temp, 0);
				}

				return returnData;
			}

			return Array.Empty<ushort>();
		}
		
		public short[] ReadSInt16(int length, Endianness endianness) {
			if (CanRead) {
				var data = GetAligned(length * sizeof(short));
				var returnData = new short[length];
				
				for (var i = 0; i < length; i++) {
					var temp = data[(i * 2)..(i * 2 + 2)];

					if (endianness == Endianness.BigEndian) {
						Array.Reverse(temp);
					}

					returnData[i] = BitConverter.ToInt16(temp, 0);
				}

				return returnData;
			}

			return Array.Empty<short>();
		}

	}

}