using System;

namespace KBinXML {
	
	[AttributeUsage(AttributeTargets.Field)]
	public class NamesAttribute : Attribute {

		public string[] Names { get; }
		public string Name => Names[0];
		
		public NamesAttribute(params string[] names) {
			Names = names;
		}

	}

}