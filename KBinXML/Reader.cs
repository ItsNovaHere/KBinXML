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

		private const byte Signature = 0xA0;

		private readonly Compression _compression;
		private readonly Encoding _encoding;
		internal readonly DataStream DataStream;
		internal readonly MemoryStream NodeStream;

		static Reader() {
			System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public Reader(byte[] data) : this(new MemoryStream(data), true) { }
		
		public Reader(Stream stream, bool closeStream = false) {
			var signature = stream.ReadUInt8();
			Assert(signature == Signature);

			_compression = (Compression) stream.ReadUInt8();
			var encodingByte = stream.ReadUInt8();
			var encodingCheck = stream.ReadUInt8();
			_encoding = (Encoding) encodingByte;
			Assert(encodingByte == (byte) ~encodingCheck);

			var nodeLength = stream.ReadUInt32(Endianness.BigEndian);

			stream.Seek(nodeLength, SeekOrigin.Current);

			var dataLength = stream.ReadUInt32(Endianness.BigEndian);

			var nodeEnd = nodeLength;
			var dataStart = nodeEnd + 12;

			stream.Seek(8, SeekOrigin.Begin);
			NodeStream = new MemoryStream(stream.Read((int) nodeEnd));

			stream.Seek(dataStart, SeekOrigin.Begin);
			DataStream = new DataStream(stream.Read((int) (stream.Length - dataStart)));
			
			if(closeStream) stream.Dispose();
		}

		public void Dispose() {
			GC.SuppressFinalize(this);

			NodeStream.Dispose();
			DataStream.Dispose();
		}

		public IEnumerator<Node> GetEnumerator() {
			return new NodeEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		private (NodeType, bool) ReadNodeType() {
			var nodeTypeByte = NodeStream.ReadUInt8();
			var isArray = (nodeTypeByte & 64) == 64;
			var nodeType = (byte) (nodeTypeByte & ~64);

			return ((NodeType) nodeType, isArray);
		}

		public Node? ReadNode() {
			if (NodeStream.Position >= NodeStream.Length) return null;

			var (nodeType, isArray) = ReadNodeType();

			var name = nodeType switch {
				NodeType.NodeEnd => "",
				NodeType.FileEnd => "",
				_ => _compression switch {
					Compression.Compressed => Sixbit.Unpack(NodeStream),
					Compression.Uncompressed => NodeStream.ReadString(NodeStream.ReadUInt8(), _encoding),
					_ => throw new ArgumentOutOfRangeException()
				}
			};

			return new Node(name, nodeType, isArray);
		}

		public XDocument GetDocument() {
			XElement? node = null;

			var document = new XDocument(new XDeclaration("1.0", GetEncodingString(_encoding), "no"));

			foreach (var rawNode in this) {
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
						break;
					}

					case NodeType.NodeEnd: {
						node = node.Parent ?? node;
						break;
					}

					case NodeType.Attribute: {
						node.Add(new XAttribute(rawNode.Name, DataStream.ReadString(_encoding)));
						break;
					}

					case NodeType.String: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						node.Add(new XAttribute("__type", rawNode.TypeName));


						node.Add(DataStream.ReadString(_encoding));

						break;
					}

					case NodeType.Binary: {
						var newNode = new XElement(rawNode.Name);
						node?.Add(newNode);
						node = newNode;
						node.Add(new XAttribute("__type", rawNode.TypeName));


						var length = DataStream.ReadUInt32(Endianness.BigEndian);
						var data = DataStream.Read((int) length);
						DataStream.Realign();

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
								var arraySize = DataStream.ReadUInt32(Endianness.BigEndian);
								var count = arraySize / (toString.Attribute.Size * toString.Attribute.Count);
								var data = toString.Method(DataStream, (int) count);

								node.Add(string.Join(" ", data));
								node.Add(new XAttribute("__count", count));
							} else {
								node.Add(toString.Method(DataStream, 1));
							}

							DataStream.Realign();
						} else {
							throw new Exception($"Data type {rawNode.Type} handler not found.");
						}

						break;
					}
				}
			}

			return document;
		}

		private static string GetEncodingString(Encoding encoding) {
			return encoding switch {
				Encoding.ASCII => "ASCII",
				Encoding.EUCJP => "EUC-JP",
				Encoding.ISO88591 => "ISO-8859-1",
				Encoding.ShiftJIS => "Shift_JIS",
				Encoding.UTF8 => "UTF-8",
				_ => throw new ArgumentOutOfRangeException()
			};
		}

	}

}