using System;
using System.Text;

namespace IOU
{
	public static partial class Utils
	{
		public static string FormatBytesize(long bytes, bool longFormat = true)
		{
			var table = new[]{
				new{brief="B", prefix=""},
				new{brief="KB", prefix="Kilo"},
				new{brief="MB", prefix="Mega"},
				new{brief="GB", prefix="Giga"},
				new{brief="TB", prefix="Terra"},
				new{brief="PB", prefix="Peta"},
			};

			double ToRound(float x) => Math.Round(x * 100) / 100;

			var sign = bytes < 0 ? "-" : "";
			bytes = Math.Abs(bytes);

			var order = (int)Math.Min(Math.Floor(Math.Log2(bytes) / 10), table.Length - 1);

			var rounded = ToRound(bytes / (float)Math.Pow(2, 10 * order));
			var suffix = longFormat
				? $"{table[order].prefix}byte"
				: table[order].brief;

			return $"{sign}{rounded}{suffix}";
		}

		public static string HexDump(byte[] bytes, int bytesPerLine = 32)
		{
			if (bytes == null)
				throw new ArgumentOutOfRangeException(nameof(bytes));

			var bytesLength = bytes.Length;

			char[] hexChars = "0123456789ABCDEF".ToCharArray();

			// 8 characters for the address + 3 spaces
			var firstHexColumn = 8 + 3;

			var firstCharColumn = firstHexColumn + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;

			var lineLength = firstCharColumn + bytesPerLine + Environment.NewLine.Length;

			char[] line = (new string(' ', firstCharColumn + bytesPerLine) + Environment.NewLine).ToCharArray();
			var expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
			StringBuilder result = new StringBuilder(expectedLines * lineLength);

			for (var i = 0; i < bytesLength; i += bytesPerLine)
			{
				line[0] = hexChars[(i >> 28) & 0xF];
				line[1] = hexChars[(i >> 24) & 0xF];
				line[2] = hexChars[(i >> 20) & 0xF];
				line[3] = hexChars[(i >> 16) & 0xF];
				line[4] = hexChars[(i >> 12) & 0xF];
				line[5] = hexChars[(i >> 8) & 0xF];
				line[6] = hexChars[(i >> 8) & 0xF];
				line[7] = hexChars[(i) & 0xF];

				var hexColumn = firstHexColumn;
				var charColumn = firstCharColumn;

				for (var j = 0; j < bytesPerLine; j++)
				{
					if (j > 0 && (j & 7) == 0)
						hexColumn++;

					if (i + j >= bytesLength)
					{
						line[hexColumn] = ' ';
						line[hexColumn + 1] = ' ';
						line[charColumn] = ' ';
					}
					else
					{
						var b = bytes[i + j];
						line[hexColumn] = hexChars[(b >> 4) & 0xF];
						line[hexColumn + 1] = hexChars[b & 0xF];
						line[charColumn] = asciiSymbol(b);
					}

					hexColumn += 3;
					charColumn++;
				}

				result.Append(line);
			}

			return result.ToString();
		}

		static char asciiSymbol(byte val)
		{
			if (val < 32) return '.'; // non primtable ascii
			if (val < 127) return (char)val;
			// Handle the hole in Latin-1
			if (val == 127) return '.';
			if (val < 0x90) return "€.‚ƒ„…†‡ˆ‰Š‹Œ.Ž."[val & 0xF];
			if (val < 0xA0) return ".‘’“”•–—˜™š›œ.žŸ"[val & 0xF];
			if (val == 0xAD) return '.'; // Soft hyphen: this symbol is zero-width even in monospace fonts
			return (char)val; // Normal Latin-1
		}
	}
}
