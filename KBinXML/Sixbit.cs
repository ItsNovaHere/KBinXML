using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBinXML {

	public static class Sixbit {

		private const string PackMap = "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private static readonly Dictionary<char, byte> UnpackMap = new();

		static Sixbit() {
			for (var i = 0; i < PackMap.Length; i++) {
				UnpackMap.Add(PackMap[i], (byte) i);
			}
		}
		
		public static string Unpack(Stream stream) {
			var sixbitLength = stream.ReadUInt8();
			var realLength = (int) Math.Ceiling(sixbitLength * 6d / 8d);
			var buffer = stream.Read(realLength);
			var result = "";

			for (var i = 0; i < sixbitLength; i++) {
				byte current = 0;
				for (var j = 0; j < 6; j++) {
					var k = i * 6 + j;
					current = (byte) (current | (buffer[k / 8] >> (7 - k % 8) & 1) << (5 - k % 6));
				}

				result += PackMap[current];
			}
			
			return result;
		}

		public static string Unpack(byte[] input) {
			var sixbitLength = input[0];
			var realLength = (int) Math.Ceiling(sixbitLength * 6d / 8d);
			var buffer = input[1..(realLength + 1)];
			var result = "";

			for (var i = 0; i < sixbitLength; i++) {
				byte current = 0;
				for (var j = 0; j < 6; j++) {
					var k = i * 6 + j;
					current = (byte) (current | (buffer[k / 8] >> (7 - k % 8) & 1) << (5 - k % 6));
				}

				result += PackMap[current];
			}
			
			return result;
		}
		
		public static byte[] Pack(string input) {
			var length = (byte) input.Length;
			var realLength = (int) Math.Ceiling(length * 6d / 8d);
			var bytes = new byte[realLength + 1];
			bytes[0] = length;

			var i = 0;
			foreach (var b in input.Select(c => UnpackMap[c])) {
				for (var j = 0; j < 6; j++) {
					bytes[1 + i / 8] = (byte) (bytes[1 + i / 8] | (b >> (5 - i % 6) & 1) << (7 - i % 8));
					i += 1;
				}
			}
			return bytes;
		}

	}

}