namespace KBinXML {

	public struct Node {

		public string Name { get; }
		public NodeType Type { get; }
		public string TypeName { get; }
		public bool IsArray { get; }

		public Node(string name, NodeType type, bool isArray) {
			Name = name;
			Type = type;
			IsArray = isArray;
			TypeName = type.ToTypeName();
		}

		public override string ToString() {
			return $"Name: {Name}, Type: {Type:G}";
		}

	}

}