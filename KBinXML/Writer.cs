using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace KBinXML {

	public class Writer : IDisposable {

		private const byte Signature = 0xA0;
		
		private readonly XDocument _document;
		private readonly Encoding _encoding;
		private readonly Compression _compression;
		private MemoryStream _nodeStream;
		private DataStream _dataStream;

		static Writer() {
			System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		
		public Writer(XDocument document, Compression compression) {
			_document = document;
			_encoding = GetEncoding(_document.Declaration.Encoding);
			_compression = compression;
		}

		public void WriteTo(Stream stream) {
			if (stream.CanWrite) {
				stream.WriteByte(Signature);
				stream.WriteByte((byte) _compression);
				stream.WriteByte((byte) _encoding);
				stream.WriteByte((byte) ~_encoding);
			}

			_nodeStream = new MemoryStream();
			_dataStream = new DataStream();

			WriteNode(_document.Root);
			
			_nodeStream.WriteByte(0xFF);
			
			stream.WriteUInt32((uint) _nodeStream.Length, Endianness.BigEndian);
			
			_nodeStream.WriteTo(stream);

			stream.WriteUInt32((uint) _dataStream.Length, Endianness.BigEndian);
			
			_dataStream.WriteTo(stream);
		}

		private void WriteNode(XElement node) {
			WriteNodeType(node, out var nodeType, out var isArray);
			WriteName(node);

			switch (nodeType) {
				case NodeType.String:
					_dataStream.WriteString(node.Value, _encoding);
					break;
				case NodeType.Binary:
					var data = new byte[node.Value.Length / 2];
					for (var i = 0; i < data.Length; i++) {
						data[i] = byte.Parse(node.Value[(i * 2)..(i * 2 + 2)], NumberStyles.HexNumber);
					}
					
					_dataStream.WriteUInt32((uint) data.Length, Endianness.BigEndian);
					_dataStream.Write(data, 0, data.Length);
					_dataStream.Realign();
					break;
				
				default:
					if (DataTypeHandlers.FromStringMap.TryGetValue(nodeType, out var fromString)) {
						if (isArray) {
							var arrayLength = uint.Parse(node.Attribute("__count")?.Value!);
							_dataStream.WriteUInt32((uint) (arrayLength * fromString.Attribute.Size * fromString.Attribute.Count), Endianness.BigEndian);
						}
						
						fromString.Method(_dataStream, node.Value);
				
						_dataStream.Realign();
					}
					break;
			}

			foreach (var attribute in node.Attributes()) {
				if(attribute.Name.LocalName.StartsWith("__")) continue;

				_nodeStream.WriteNodeType(NodeType.Attribute);
				_dataStream.WriteString(attribute.Value, _encoding);
				
				WriteName(attribute);
			}
			
			foreach (var child in node.Elements()) {
				WriteNode(child);
			}

			_nodeStream.WriteByte(0xFE);
		}

		private void WriteNodeType(XElement node, out NodeType nodeType, out bool isArray) {
			isArray = false;
			
			var typeAttribute = node.Attribute("__type");
			if (typeAttribute != null) {
				nodeType = Util.FromTypeName(typeAttribute.Value);
				var rawNodeType = (byte) nodeType; 
					
				var countAttribute = node.Attribute("__count");
				if (countAttribute != null) {
					rawNodeType = (byte) (rawNodeType ^ 0b1000000); 
					isArray = true;
				}
					
				_nodeStream.WriteByte(rawNodeType);
			} else {
				_nodeStream.WriteNodeType(NodeType.NodeStart);

				nodeType = NodeType.NodeStart;
			}
		}
		
		private void WriteName(XObject node) {
			var name = node switch {
				XElement element => element.Name.LocalName,
				XAttribute attribute => attribute.Name.LocalName,
				_ => throw new ArgumentOutOfRangeException(nameof(node), $"node is type {node.GetType().Name}, expected XElement or XAttribute")
			};
			
			if (_compression == Compression.Compressed) {
				Sixbit.Pack(_nodeStream, name);
			} else {
				_nodeStream.WriteString(name, _encoding);
			}
		}
		
		public void Dispose() {
			
		}

		private static Encoding GetEncoding(string encoding) {
			return encoding.ToUpper() switch {
				"ASCII" => Encoding.ASCII,
				"EUC-JP" => Encoding.EUCJP,
				"ISO-8859-1" => Encoding.ISO88591,
				"SHIFT_JIS" => Encoding.ShiftJIS,
				"UTF-8" => Encoding.UTF8,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
		
	}

}