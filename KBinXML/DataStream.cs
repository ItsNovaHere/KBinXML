using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBinXML {

	internal class DataStream : MemoryStream {

		internal long Position1 { get; set; }
		internal long Position2 { get; set; }


		internal DataStream() {
			
		}
		
		internal DataStream(byte[] data) : base(data) {
				
		}

		private byte[] GetAligned(int size) {
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

		public void WriteAligned(byte[] data) {
			if (Position1 % 4 == 0) {
				Position1 = Position;
			}

			if (Position2 % 4 == 0) {
				Position2 = Position;
			}

			var pos = Position;
			switch (data.Length) {
				case 1:
					Seek(Position1++, SeekOrigin.Begin);
					WriteByte(data[0]);
					break;
				case 2:
					Seek(Position2, SeekOrigin.Begin);
					Write(data, 0, 2);
					Position2 += 2;
					break;
				default:
					Write(data, 0, data.Length);
					this.Realign();
					return;
			}

			var trailing = Math.Max(Position1, Position2);
			if (pos < trailing) {
				Seek(trailing, SeekOrigin.Begin);
				this.Realign();
			} else {
				Seek(pos, SeekOrigin.Begin);
			}
		}
		
		public IEnumerable<byte> ReadUInt8(int length) {
			if (CanRead) {
				return GetAligned(length * sizeof(byte));
			}

			return Array.Empty<byte>();
		}

		public IEnumerable<sbyte> ReadSInt8(int length) {
			if (CanRead) {
				return GetAligned(length * sizeof(sbyte)).Select(x => (sbyte) x).ToArray();
			}

			return Array.Empty<sbyte>();
		}

		public IEnumerable<ushort> ReadUInt16(int length, Endianness endianness) {
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
		
		public IEnumerable<short> ReadSInt16(int length, Endianness endianness) {
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

		public void WriteUInt8(byte[] data) {
			WriteAligned(data);
		}

		public void WriteSInt8(sbyte[] data) {
			WriteAligned(data.Select(x => (byte)x).ToArray());
		}

		public void WriteUInt16(ushort[] data, Endianness endianness) {
			WriteArrayAligned(data, BitConverter.GetBytes, Endianness.BigEndian);

		}
		
		public void WriteSInt16(short[] data, Endianness endianness) {
			WriteArrayAligned(data, BitConverter.GetBytes, Endianness.BigEndian);
		}

		public void WriteUInt32(uint[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}

		public void WriteSInt32(int[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}
		
		public void WriteUInt64(ulong[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}

		public void WriteSInt64(long[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}
		
		public void WriteSingle(float[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}

		public void WriteDouble(double[] data, Endianness endianness) {
			WriteArray(data, BitConverter.GetBytes, Endianness.BigEndian);
		}

		private void WriteArrayAligned<T>(T[] data, Func<T, byte[]> getBytes, Endianness endianness) {
			if(endianness == Endianness.BigEndian) Array.Reverse(data);
			
			var rawData = data.SelectMany(getBytes).ToArray();
			
			if(endianness == Endianness.BigEndian) Array.Reverse(rawData);
			
			WriteAligned(rawData);
		}
		
		private void WriteArray<T>(T[] data, Func<T, byte[]> getBytes,  Endianness endianness) {
			if(endianness == Endianness.BigEndian) Array.Reverse(data); // reverse input data so later reversal maintains element orderr
			
			var rawData = data.SelectMany(getBytes).ToArray();
			
			if(endianness == Endianness.BigEndian) Array.Reverse(rawData);
			
			Write(rawData);
		}

	}

}