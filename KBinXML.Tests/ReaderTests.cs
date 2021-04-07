using System.IO;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace KBinXML.Tests {

	public class ReaderTests {

		private readonly ITestOutputHelper _testOutputHelper;

		public ReaderTests(ITestOutputHelper testOutputHelper) {
			_testOutputHelper = testOutputHelper;
		}

		[Fact]
		public void ProperDecode() {
			using var file = File.OpenRead("test.kbin");
			
			using var reader = new Reader(file);
			var document = reader.GetDocument();
			
			Assert.True(XNode.DeepEquals(XDocument.Load("test.xml"), document));
		}
	}

}