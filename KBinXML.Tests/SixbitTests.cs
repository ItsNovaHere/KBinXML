using System.IO;
using Xunit;

namespace KBinXML.Tests {

	public class SixbitTests {

		[Fact]
		public void Pack() {
			const string input = "This_is_a_test_string";
			
			var packed = Sixbit.Pack(input);

			Assert.Equal(new byte[] {0x15, 0x7a, 0xdb, 0xb8, 0x96, 0xee, 0x25, 0x9a, 0x5e, 0x6a, 0xe3, 0x99, 0x78, 0xe7, 0x7b, 0xb3, 0xb0}, packed);
		}

		[Fact]
		public void Unpack_FromStream() {
			using var input = new MemoryStream(new byte[] {0x15, 0x7a, 0xdb, 0xb8, 0x97, 0x8b, 0x74, 0xeb, 0x1a, 0x65, 0xa6, 0xaa, 0x34, 0xa6, 0xa9, 0x4a, 0x0c});

			var unpacked = Sixbit.Unpack(input);
			
			Assert.Equal("This_should_decode_:3", unpacked);
		}

		[Fact]
		public void Unpack_FromArray() {
			var input = new byte[] {0x15, 0x7a, 0xdb, 0xb8, 0x97, 0x8b, 0x74, 0xeb, 0x1a, 0x65, 0xa6, 0xaa, 0x34, 0xa6, 0xa9, 0x4a, 0x0c};

			var unpacked = Sixbit.Unpack(input);

			Assert.Equal("This_should_decode_:3", unpacked);
		}

	}

}