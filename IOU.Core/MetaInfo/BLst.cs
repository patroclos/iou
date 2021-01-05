using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOU
{
	public class BLst : BEnc<IEnumerable<BEnc>>
	{
		public static readonly BLst Empty = new BLst();
		
		public override BEncType Type => BEncType.BLst;
		
		public override IEnumerable<BEnc> Value { get; }
		
		public BLst(IEnumerable<BEnc> value)
		{
			Value = value.ToArray();
		}

		public BLst()
		{
			Value = ArraySegment<BEnc>.Empty;
		}

		public BLst Add(BEnc value)
		{
			return new BLst(Value.Concat(new[]{value}));
		}
			
		public override void Encode(BinaryWriter writer)
		{
			writer.Write('l');
			foreach(var e in Value)
				e.Encode(writer);
			writer.Write('e');
		}

		public static bool TryParse(ReadOnlySpan<byte> buffer, out BLst? value, out int consumed)
		{
			value = default;
			consumed = default;
			
			if (buffer[0] != 'l')
				return false;
			
			var values = new List<BEnc>();
			consumed = 1;
			while (BEnc.TryParseExpr(buffer.Slice(consumed), out var val, out var cnt))
			{
				values.Add(val ?? throw new InvalidProgramException($"{nameof(BEnc)}.{nameof(BEnc.TryParseExpr)} returned true but the value is null"));
				consumed += cnt;
			}

			if (buffer[consumed] != 'e')
				return false;
			consumed++;
			value = new BLst(values);
			return true;
		}

		public override void WriteTo(IndentedTextWriter writer)
		{
			writer.WriteLine($"BLst({Value.Count()} items)");
			writer.Indent++;
			foreach (var val in Value)
				val.WriteTo(writer);
			writer.Indent--;
			writer.WriteLine($"// End BLst\n");
		}
	}
}
