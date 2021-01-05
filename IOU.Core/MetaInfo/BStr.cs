using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IOU
{
	public sealed class BStr : BEnc<ReadOnlyMemory<byte>>
	{
		public override BEncType Type => BEncType.BStr;

		public override ReadOnlyMemory<byte> Value { get; }

		private string? _utf8;
		public string Utf8String => _utf8 ??= Encoding.UTF8.GetString(Value.Span);
		
		public BStr(byte[] value)
		{
			Value = value;
		}

		public BStr(string value)
		{
			_utf8 = value;
			Value = Encoding.UTF8.GetBytes(value);
		}

		public BStr(ReadOnlyMemory<byte> bytes)
		{
			Value = bytes;
		}
		
		public override void Encode(BinaryWriter w)
		{
			w.Write(Encoding.UTF8.GetBytes($"{Value.Length}:"));
			w.Write(Value.Span);
		}
		
		public static implicit operator BStr(string str) => new BStr(str);
		public static implicit operator string(BStr bstr) => bstr.Utf8String;
		public static implicit operator byte[](BStr bstr) => bstr.Value.ToArray();

		//public static (BStr, long)? TryParse(ReadOnlySpan<byte> buffer)
		public static bool TryParse(ReadOnlySpan<byte> buffer, out BStr? value, out int consumed)
		{
			consumed = 0;
			value = default;
			
			var countB = new List<byte>();
			for (var i = 0; i < buffer.Length && char.IsDigit((char) buffer[i]); i++)
				countB.Add(buffer[i]);

			if (countB.Count == 0)
				return false;

			if (buffer[countB.Count] != ':')
				return false;

			var len = countB.Count + 1 + int.Parse(Encoding.ASCII.GetString(countB.ToArray()));
			var buf = buffer[(countB.Count + 1)..len].ToArray();
			value = new BStr(buf);
			consumed = len;
			return true;
		}

		public override void WriteTo(IndentedTextWriter writer)
		{
			var u8 = Value.Length < 1000 ? $"'{Encoding.UTF8.GetString(Value.Span)}'" : "-";
			writer.WriteLine($"BStr({Value}, UTF8={u8})");
		}
	}
}
