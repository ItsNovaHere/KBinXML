namespace KBinXML {

	internal readonly struct FromStringHandler {

		public DataTypeHandlers.FromString Method { get; }
		public DataTypeHandlerAttribute Attribute { get; }

		public FromStringHandler(DataTypeHandlers.FromString method, DataTypeHandlerAttribute attribute) {
			Method = method;
			Attribute = attribute;
		}
	}

}