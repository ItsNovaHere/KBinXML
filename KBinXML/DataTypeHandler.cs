namespace KBinXML {

	public struct DataTypeHandler {

		public DataTypeHandlers.ToString Method { get; }
		public DataTypeHandlerAttribute Attribute { get; }

		public DataTypeHandler(DataTypeHandlers.ToString method, DataTypeHandlerAttribute attribute) {
			Method = method;
			Attribute = attribute;
		}
	}

}