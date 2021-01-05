using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IOU
{
	public abstract class BEnc<T> : BEnc
	{
		public abstract T Value { get; }
	}

	public enum BEncType
	{
		BStr,
		BInt,
		BLst,
		BDict
	}

	public interface IBEnc
	{
		void Encode(BinaryWriter writer);
		void WriteTo(IndentedTextWriter writer);
	}

	public abstract class BEnc : IBEnc
	{
		public abstract BEncType Type { get; }

		public abstract void Encode(BinaryWriter writer);

		public abstract void WriteTo(IndentedTextWriter writer);

		public override string ToString()
		{
			var strWriter = new StringWriter(new StringBuilder());
			var writer = new IndentedTextWriter(strWriter);
			WriteTo(writer);
			writer.Close();
			return strWriter.ToString();
		}

		public byte[] ToByteArray() => BEnc.EncodeBuffer(this);

		public byte[] Hash()
		{
			using var sha = System.Security.Cryptography.SHA1.Create();
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);
			Encode(writer);
			stream.Seek(0, SeekOrigin.Begin);
			var hash = sha.ComputeHash(stream);
			return hash;
		}

		public T Value<T>() => MetaInfoSerializer.Deserialize<T>(this);

		public BEnc this[int idx] =>
			(this as BLst ?? throw new InvalidOperationException())
			.Value.ElementAt(idx);

		public BEnc? this[string key] =>
			(this as BDict ?? throw new InvalidOperationException())
			[key];

		public static byte[] EncodeBuffer(BEnc value)
		{
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);

			value.Encode(writer);
			writer.Close();

			return stream.ToArray();
		}

		public static bool TryParseExpr(ReadOnlySpan<byte> buffer, out BEnc? value, out int consumed)
		{
			if (buffer.IsEmpty)
			{
				value = default;
				consumed = default;
				return false;
			}

			if (BStr.TryParse(buffer, out var str, out consumed))
			{
				value = str;
				return true;
			}

			if (BInt.TryParse(buffer, out var i, out consumed))
			{
				value = i;
				return true;
			}

			if (BLst.TryParse(buffer, out var lst, out consumed))
			{
				value = lst;
				return true;
			}

			if (BDict.TryParse(buffer, out var dict, out consumed))
			{
				value = dict;
				return true;
			}

			value = default;
			return false;
		}
	}
}
