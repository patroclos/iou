using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOU {
	public class BDict : BEnc<IReadOnlyList<KeyValuePair<BStr, BEnc>>> {
		public override BEncType Type => BEncType.BDict;

		public override IReadOnlyList<KeyValuePair<BStr, BEnc>> Value { get; }

		public BEnc? this[BStr key]
			=> Value.FirstOrDefault(
				kv => kv.Key.Value.Span.SequenceEqual(key.Value.Span)
			).Value;

		public BEnc? this[IEnumerable<BStr> keys] =>
			keys.Aggregate((BEnc?)this, (o, k) => o != null && o is BDict d ? d[k] : default);

		public BDict(IReadOnlyList<KeyValuePair<BStr, BEnc>> values) {
			Value = values;
		}

		public override void Encode(BinaryWriter writer) {
			writer.Write('d');
			foreach (var pair in Value) {
				pair.Key.Encode(writer);
				pair.Value.Encode(writer);
			}

			writer.Write('e');
		}

		public static bool TryParse(ReadOnlySpan<byte> buffer, out BDict? value, out int consumed) {
			value = null;
			consumed = 0;

			if (buffer.IsEmpty)
				return false;

			if (buffer[0] != 'd')
				return false;

			consumed++;

			var pairs = new List<KeyValuePair<BStr, BEnc>>();
			while (true) {
				if (!BStr.TryParse(buffer.Slice(consumed), out var str, out var bcK) || str == null)
					break;
				consumed += bcK;
				if (!BEnc.TryParseExpr(buffer.Slice(consumed), out var val, out var bcV) || val == null)
					return false;
				consumed += bcV;
				pairs.Add(new KeyValuePair<BStr, BEnc>(str, val));
			}

			if (buffer.Length == consumed || buffer[consumed] != 'e')
				return false;
			consumed++;
			value = new BDict(pairs);
			return true;
		}

		public override void WriteTo(IndentedTextWriter writer) {
			writer.WriteLine($"Dict({Value.Count} items)");
			writer.Indent++;
			foreach (var kv in Value) {
				writer.WriteLine("-----------------");
				writer.Indent++;
				writer.WriteLine("[K]");
				writer.Indent++;
				kv.Key.WriteTo(writer);
				writer.Indent--;
				writer.WriteLine("[V]");
				writer.Indent++;
				kv.Value.WriteTo(writer);
				writer.Indent--;
				writer.Indent--;
			}

			writer.Indent--;
			writer.WriteLine($"// End Dict\n");
		}
	}
}
