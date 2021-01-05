using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IOU
{
	public class BInt : BEnc<long>
	{
		public override BEncType Type => BEncType.BInt;
		
		public override long Value { get; }
		
		public int IntValue => (int)Value;

		public BInt(long value)
		{
			Value = value;
		}
		
		public override void Encode(BinaryWriter writer)
		{
			writer.Write(Encoding.ASCII.GetBytes($"i{Value}e"));
		}

		public static bool TryParse(ReadOnlySpan<byte> buffer, out BInt? value, out int consumed)
		{
			consumed = 0;
			value = null;
			
			if (buffer.IsEmpty)
				return false;

			if (buffer[0] != 'i')
				return false;

			consumed++;
			
			var countB = new List<byte>();
			for (; consumed < buffer.Length && char.IsDigit((char) buffer[consumed]); consumed++)
				countB.Add(buffer[consumed]);

			if (countB.Count == 0)
				return false;

			if (buffer.Length == consumed || buffer[consumed] != 'e')
				return false;

			consumed++;
			
			value = new BInt(long.Parse(Encoding.UTF8.GetString(countB.ToArray())));
			return true;
		}

		public override void WriteTo(IndentedTextWriter writer)
		{
			writer.WriteLine($"BInt({Value})");
		}
		
		public static implicit operator BInt(long val) => new BInt(val);
		public static implicit operator BInt(int val) => new BInt(val);
		public static implicit operator long(BInt bstr) => bstr.Value;
		public static implicit operator int(BInt bstr) => bstr.IntValue;
	}
}
