#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static KBinXML.Util;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace KBinXML {

	public class Reader : IDisposable, IEnumerable<Node> {

		static Reader() {
			System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		
		private const byte Signature = 0xA0;
		private readonly Compression _compression;
		private readonly DataStream _dataStream;
		private readonly Encoding _encoding;

		private readonly MemoryStream _nodeStream;

		public Reader(Stream stream) {
			var signature = stream.ReadUInt8();
			Assert(signature == Signature);

			_compression = (Compression) stream.ReadUInt8();
			var encodingByte = stream.ReadUInt8();
			var encodingCheck = stream.ReadUInt8();
			_encoding = (Encoding) encodingByte;
			Assert(encodingByte == (byte) ~encodingCheck);

			Console.WriteLine($"Compression: {_compression:G}, Encoding: {_encoding:G}.");

			var nodeLength = stream.ReadUInt32(Endianness.BigEndian);

			stream.Seek(nodeLength, SeekOrigin.Current);

			var dataLength = stream.ReadUInt32(Endianness.BigEndian);

			var nodeEnd = 8 + nodeLength;
			var dataStart = nodeEnd + 4;

			stream.Seek(8, SeekOrigin.Begin);
			_nodeStream = new MemoryStream(stream.Read((int) (nodeEnd - 8)));

			stream.Seek(dataStart, SeekOrigin.Begin);
			_dataStream = new DataStream(stream.Read((int) (stream.Length - dataStart)));
		}

		private (NodeType, bool) ReadNodeType() {
			var nodeTypeByte = _nodeStream.ReadUInt8();
			var isArray = (nodeTypeByte & 64) == 64;
			var nodeType = (byte) (nodeTypeByte & ~64);

			return ((NodeType) nodeType, isArray);
		}

		public Node? ReadNode() {
			if (_nodeStream.Position >= _nodeStream.Length) return null;

			var (nodeType, isArray) = ReadNodeType();

			var name = nodeType switch {
				NodeType.NodeEnd => "",
				NodeType.FileEnd => "",
				_ => _compression switch {
					Compression.Compressed => Sixbit.Unpack(_nodeStream),
					Compression.Uncompressed => _nodeStream.ReadString(_nodeStream.ReadUInt8(), _encoding),
					_ => throw new ArgumentOutOfRangeException()
				}
			};

			return new Node(name, nodeType, isArray);
		}

		public XDocument GetDocument() {
			XElement? node = null;

			var document = new XDocument(new XDeclaration("1.0", GetEncodingString(), "no"));
			
			foreach (var rawNode in this) {
				Console.WriteLine($"Parsing {rawNode.ToString()}");
				
				switch (rawNode.Type) {
					case NodeType.FileEnd when node == null:
						throw new NullReferenceException("Attempt to add a node that doesn't exist.");
					case NodeType.NodeEnd when node == null:
						throw new NullReferenceException("Attempt to end node that hasn't started.");
					case NodeType.Attribute when node == null:
						throw new NullReferenceException("Attempt to add an attribute to a node that hasn't started.");

					case NodeType.NodeStart: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						break;
					}

					case NodeType.FileEnd: {
						document.Add(node);
						return document;
					}

					case NodeType.NodeEnd: {
						node = node.Parent ?? node;
						break;
					}

					case NodeType.Attribute: {
						node.Add(new XAttribute(rawNode.Name, _dataStream.ReadString(_encoding)));
						break;
					}

					case NodeType.String: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						node.Add(new XAttribute("__type", rawNode.TypeName));

						
						node.Add(_dataStream.ReadString(_encoding));
						
						break;
					}

					case NodeType.Binary: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						node.Add(new XAttribute("__type", rawNode.TypeName));


						var length = _dataStream.ReadUInt32(Endianness.BigEndian);
						var data = _dataStream.Read((int) length);
						_dataStream.Realign();
						
						node.Add(string.Join("", data.Select(x => x.ToString("X2"))));
						
						break;
					}

					default: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						node.Add(new XAttribute("__type", rawNode.TypeName));

						if (DataTypeHandlers.ToStringMap.TryGetValue(rawNode.Type, out var toString)) {
							if (rawNode.IsArray) {
								var arraySize = _dataStream.ReadUInt32(Endianness.BigEndian);
								var count = arraySize / (toString.Attribute.Size * toString.Attribute.Count);
								var data = toString.Method(_dataStream, (int) count);
								
								node.Add(string.Join(" ", data));
								node.Add(new XAttribute("__count", count));
							} else {
								node.Add(toString.Method(_dataStream, 1));
							}
							
							_dataStream.Realign();
						} else {
							throw new Exception($"Data type {rawNode.Type} handler not found.");
						}

						break;
					}
				}
			}
			
			return document;
		}

		private string GetEncodingString() {
			return _encoding switch {
				Encoding.ASCII => "ASCII",
				Encoding.EUCJP => "EUC-JP",
				Encoding.ISO88591 => "ISO-8859-1",
				Encoding.ShiftJIS => "Shift-JIS",
				Encoding.UTF8 => "UTF-8",
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
			
			_nodeStream.Dispose();
			_dataStream.Dispose();
		}

		public IEnumerator<Node> GetEnumerator() {
			return new NodeEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	public enum Compression : byte {

		Compressed = 0x42,
		Uncompressed = 0x45

	}

	public enum Encoding : byte {

		ASCII = 0x20,
		ISO88591 = 0x40,
		EUCJP = 0x60,
		ShiftJIS = 0x80,
		UTF8 = 0xA0

	}

	public enum NodeType : byte {

		NodeStart = 1,
		S8 = 2,
		U8 = 3,
		S16 = 4,
		U16 = 5,
		S32 = 6,
		U32 = 7,
		S64 = 8,
		U64 = 9,
		Binary = 10,
		String = 11,
		IP4 = 12,
		Time = 13,
		Single = 14,
		Double = 15,
		S8X2 = 16,
		U8X2 = 17,
		S16X2 = 18,
		U16X2 = 19,
		S32X2 = 20,
		U32X2 = 21,
		S64X2 = 22,
		U64X2 = 23,
		SingleX2 = 24,
		DoubleX2 = 25,
		S8X3 = 26,
		U8X3 = 27,
		S16X3 = 28,
		U16X3 = 29,
		S32X3 = 30,
		U32X3 = 31,
		S64X3 = 32,
		U64X3 = 33,
		SingleX3 = 34,
		DoubleX3 = 35,
		S8X4 = 36,
		U8X4 = 37,
		S16X4 = 38,
		U16X4 = 39,
		S32X4 = 40,
		U32X4 = 41,
		S64X4 = 42,
		U64X4 = 43,
		SingleX4 = 44,
		DoubleX4 = 45,
		Attribute = 46,
		VS8 = 48,
		VU8 = 49,
		VS16 = 50,
		VU16 = 51,
		Bool = 52,
		BoolX2 = 53,
		BoolX3 = 54,
		BoolX4 = 55,
		VB = 56,

		NodeEnd = 190,
		FileEnd = 191

	}

}