namespace KBinXML {

	internal readonly struct ToStringHandler {

		public DataTypeHandlers.ToString Method { get; }
		public DataTypeHandlerAttribute Attribute { get; }

		public ToStringHandler(DataTypeHandlers.ToString method, DataTypeHandlerAttribute attribute) {
			Method = method;
			Attribute = attribute;
		}
	}

}