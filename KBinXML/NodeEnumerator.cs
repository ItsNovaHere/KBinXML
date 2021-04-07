using System;
using System.Collections;
using System.Collections.Generic;

namespace KBinXML {

	public class NodeEnumerator : IEnumerator<Node> {

		private readonly Reader _reader;
		
		public NodeEnumerator(Reader reader) {
			_reader = reader;
		}

		public bool MoveNext() {
			var node = _reader.ReadNode();
			if (node == null) {
				return false;
			}

			Current = node.Value;
			return true;
		}

		public void Reset() { }

		public Node Current { get; private set; }

		object IEnumerator.Current => Current;

		public void Dispose() {
			GC.SuppressFinalize(this);
		}

	}

}