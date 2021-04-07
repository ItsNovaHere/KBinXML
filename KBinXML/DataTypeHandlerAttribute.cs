using System;

namespace KBinXML {

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	internal class DataTypeHandlerAttribute : Attribute {

		public NodeType Type { get; }
		public int Count { get; }
		public int Size { get; }
		
		public DataTypeHandlerAttribute(NodeType type, int count, int size) {
			Type = type;
			Count = count;
			Size = size;
		}

	}

}