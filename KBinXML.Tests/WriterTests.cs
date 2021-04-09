using System.IO;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace KBinXML.Tests {

	public class WriterTests {

		static WriterTests() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		
		[Fact]
		public void ProperEncode() {
			using var writer = new Writer(XDocument.Load("test.xml"), Compression.Compressed);
			using var memory = new MemoryStream();
			
			writer.WriteTo(memory);

			Assert.Equal(memory.ToArray(), File.ReadAllBytes("test.kbin"));
		}
	}

}