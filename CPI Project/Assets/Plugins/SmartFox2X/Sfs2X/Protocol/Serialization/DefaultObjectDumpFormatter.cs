using System;
using System.Text;
using Sfs2X.Exceptions;
using Sfs2X.Util;

namespace Sfs2X.Protocol.Serialization
{
	public class DefaultObjectDumpFormatter
	{
		public static readonly char TOKEN_INDENT_OPEN = '{';

		public static readonly char TOKEN_INDENT_CLOSE = '}';

		public static readonly char TOKEN_DIVIDER = ';';

		public static readonly char NEW_LINE = '\n';

		public static readonly char TAB = '\t';

		public static readonly char DOT = '.';

		public static readonly int HEX_BYTES_PER_LINE = 16;

		public static int MAX_DUMP_LENGTH = 1024;

		public static string PrettyPrintDump(string rawDump)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			foreach (char c in rawDump)
			{
				if (c == TOKEN_INDENT_OPEN)
				{
					num++;
					char nEW_LINE = NEW_LINE;
					stringBuilder.Append(nEW_LINE + GetFormatTabs(num));
				}
				else if (c == TOKEN_INDENT_CLOSE)
				{
					num--;
					if (num < 0)
					{
						throw new SFSError("DumpFormatter: the indentPos is negative. TOKENS ARE NOT BALANCED!");
					}
					char nEW_LINE = NEW_LINE;
					stringBuilder.Append(nEW_LINE + GetFormatTabs(num));
				}
				else if (c == TOKEN_DIVIDER)
				{
					char nEW_LINE = NEW_LINE;
					stringBuilder.Append(nEW_LINE + GetFormatTabs(num));
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			if (num != 0)
			{
				throw new SFSError("DumpFormatter: the indentPos is not == 0. TOKENS ARE NOT BALANCED!");
			}
			return stringBuilder.ToString();
		}

		private static string GetFormatTabs(int howMany)
		{
			return StrFill(TAB, howMany);
		}

		private static string StrFill(char ch, int howMany)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < howMany; i++)
			{
				stringBuilder.Append(ch);
			}
			return stringBuilder.ToString();
		}

		public static string HexDump(ByteArray ba)
		{
			return HexDump(ba, HEX_BYTES_PER_LINE);
		}

		public static string HexDump(ByteArray ba, int bytesPerLine)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = ba.Length.ToString();
			char nEW_LINE = NEW_LINE;
			stringBuilder.Append("Binary Size: " + text + nEW_LINE);
			if (ba.Length > MAX_DUMP_LENGTH)
			{
				stringBuilder.Append("** Data larger than max dump size of " + MAX_DUMP_LENGTH + ". Data not displayed");
				return stringBuilder.ToString();
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			StringBuilder stringBuilder3 = new StringBuilder();
			int num = 0;
			int num2 = 0;
			do
			{
				byte b = ba.Bytes[num];
				string text2 = $"{b:x2}";
				if (text2.Length == 1)
				{
					text2 = "0" + text2;
				}
				stringBuilder2.Append(text2 + " ");
				char value = ((b < 33 || b > 126) ? DOT : Convert.ToChar(b));
				stringBuilder3.Append(value);
				if (++num2 == bytesPerLine)
				{
					num2 = 0;
					string text3 = stringBuilder2.ToString();
					nEW_LINE = TAB;
					string text4 = nEW_LINE.ToString();
					string text5 = stringBuilder3.ToString();
					nEW_LINE = NEW_LINE;
					stringBuilder.Append(text3 + text4 + text5 + nEW_LINE);
					stringBuilder2 = new StringBuilder();
					stringBuilder3 = new StringBuilder();
				}
			}
			while (++num < ba.Length);
			if (num2 != 0)
			{
				for (int num3 = bytesPerLine - num2; num3 > 0; num3--)
				{
					stringBuilder2.Append("   ");
					stringBuilder3.Append(" ");
				}
				string text6 = stringBuilder2.ToString();
				nEW_LINE = TAB;
				string text7 = nEW_LINE.ToString();
				string text8 = stringBuilder3.ToString();
				nEW_LINE = NEW_LINE;
				stringBuilder.Append(text6 + text7 + text8 + nEW_LINE);
			}
			return stringBuilder.ToString();
		}
	}
}
