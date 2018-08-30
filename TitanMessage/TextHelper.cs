using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TitanMessage
{
	public static class TextHelper
	{
		// TODO: doesn't support "hacky umlauts" yet

		const char controlCodeBegin = '[', controlCodeEnd = ']', controlCodeArgSeperator = ':';
		const string controlCodePageBreak = "Page";

		readonly static Encoding shiftJisEncoding = Encoding.GetEncoding(932);

		readonly static Dictionary<char, char> shiftJisToAscii = new Dictionary<char, char>()
		{
			{ '　', ' ' }, { '，', ',' }, { '．', '.' }, { '：', ':' }, { '；', ';' }, { '？', '?' }, { '！', '!' }, { '－', '-' },
			{ '／', '/' }, { '～', '~' }, { '’', '\'' }, { '”', '\"' }, { '（', '(' }, { '）', ')' }, { '［', '[' }, { '］', ']' },
			{ '〈', '<' }, { '〉', '>' }, { '＋', '+' }, { '＊', '*' }, { '＆', '&' },

			{ '０', '0' }, { '１', '1' }, { '２', '2' }, { '３', '3' }, { '４', '4' }, { '５', '5' }, { '６', '6' }, { '７', '7' },
			{ '８', '8' }, { '９', '9' },

			{ 'Ａ', 'A' }, { 'Ｂ', 'B' }, { 'Ｃ', 'C' }, { 'Ｄ', 'D' }, { 'Ｅ', 'E' }, { 'Ｆ', 'F' }, { 'Ｇ', 'G' }, { 'Ｈ', 'H' },
			{ 'Ｉ', 'I' }, { 'Ｊ', 'J' }, { 'Ｋ', 'K' }, { 'Ｌ', 'L' }, { 'Ｍ', 'M' }, { 'Ｎ', 'N' }, { 'Ｏ', 'O' }, { 'Ｐ', 'P' },
			{ 'Ｑ', 'Q' }, { 'Ｒ', 'R' }, { 'Ｓ', 'S' }, { 'Ｔ', 'T' }, { 'Ｕ', 'U' }, { 'Ｖ', 'V' }, { 'Ｗ', 'W' }, { 'Ｘ', 'X' },
			{ 'Ｙ', 'Y' }, { 'Ｚ', 'Z' },

			{ 'ａ', 'a' }, { 'ｂ', 'b' }, { 'ｃ', 'c' }, { 'ｄ', 'd' }, { 'ｅ', 'e' }, { 'ｆ', 'f' }, { 'ｇ', 'g' }, { 'ｈ', 'h' },
			{ 'ｉ', 'i' }, { 'ｊ', 'j' }, { 'ｋ', 'k' }, { 'ｌ', 'l' }, { 'ｍ', 'm' }, { 'ｎ', 'n' }, { 'ｏ', 'o' }, { 'ｐ', 'p' },
			{ 'ｑ', 'q' }, { 'ｒ', 'r' }, { 'ｓ', 's' }, { 'ｔ', 't' }, { 'ｕ', 'u' }, { 'ｖ', 'v' }, { 'ｗ', 'w' }, { 'ｘ', 'x' },
			{ 'ｙ', 'y' }, { 'ｚ', 'z' },
		};
		readonly static Dictionary<char, char> asciiToShiftJis = shiftJisToAscii.ToDictionary(x => x.Value, x => x.Key);

		readonly static Dictionary<ushort, Func<byte[], int, (string Output, int Index)>> controlCodeHandlers = new Dictionary<ushort, Func<byte[], int, (string Output, int Index)>>()
		{
			{ 0xF804, (b, i) => { return ControlCodeOneArgument(b, i, "Color"); } },
			{ 0xF810, (b, i) => { return ControlCodeOneArgument(b, i, "Number"); } },
			{ 0xF811, (b, i) => { return ControlCodeOneArgument(b, i, "Variable1"); } },
			{ 0xF815, (b, i) => { return ControlCodeOneArgument(b, i, "Variable2"); } },
			{ 0xF819, (b, i) => { return ControlCodeOneArgument(b, i, "Variable3"); } },
			{ 0xF840, (b, i) => { return ("Guild", i); } },
			{ 0xF841, (b, i) => { return ControlCodeOneArgument(b, i, "Item"); } },
			{ 0xF842, (b, i) => { return ControlCodeOneArgument(b, i, "Enemy1"); } },
			{ 0xF843, (b, i) => { return ControlCodeOneArgument(b, i, "Character"); } },
			{ 0xF844, (b, i) => { return ("Skyship", i); } },
			{ 0xF847, (b, i) => { return ("Location", i); } },
			{ 0xF848, (b, i) => { return ControlCodeOneArgument(b, i, "Enemy2"); } },
			{ 0xF849, (b, i) => { return ControlCodeOneArgument(b, i, "Item2"); } },
			{ 0xF84A, (b, i) => { return ControlCodeOneArgument(b, i, "Count"); } },
			{ 0xF850, (b, i) => { return ("Quest", i); } },
			{ 0xF851, (b, i) => { return ("Variable4", i); } }
		};

		readonly static Dictionary<string, ushort> controlCodeValues = new Dictionary<string, ushort>()
		{
			{ "Color", 0xF804 },
			{ "Number", 0xF810 },
			{ "Variable1", 0xF811 },
			{ "Variable2", 0xF815 },
			{ "Variable3", 0xF819 },
			{ "Guild", 0xF840 },
			{ "Item", 0xF841},
			{ "Enemy1", 0xF842 },
			{ "Character", 0xF843 },
			{ "Skyship", 0xF844 },
			{ "Location", 0xF847 },
			{ "Enemy2", 0xF848 },
			{ "Item2", 0xF849 },
			{ "Count", 0xF84A },
			{ "Quest", 0xF850 },
			{ "Variable4", 0xF851 }
		};

		public static string GetString(byte[] bytes)
		{
			var stringBuilder = new StringBuilder();

			for (int idx = 0; idx < bytes.Length; idx += 2)
			{
				if ((idx + 1) >= bytes.Length || (bytes[idx] == 0xFF && bytes[idx + 1] == 0xFF))
					break;

				var value = (ushort)(bytes[idx] << 8 | bytes[idx + 1]);

				if (value == 0xF801)
				{
					stringBuilder.AppendLine();
				}
				else if (value == 0xF802)
				{
					stringBuilder.AppendLine($"{controlCodeBegin}{controlCodePageBreak}{controlCodeEnd}");
					stringBuilder.AppendLine();
				}
				else if (controlCodeHandlers.ContainsKey(value) && controlCodeHandlers[value] != null)
				{
					var (codeResult, newIndex) = controlCodeHandlers[value](bytes, idx);
					if (codeResult != Environment.NewLine)
						stringBuilder.Append($"{controlCodeBegin}{codeResult}{controlCodeEnd}");
					else
						stringBuilder.Append($"{codeResult}");
					idx = newIndex;
				}
				else
				{
					var shiftJisChar = shiftJisEncoding.GetChars(bytes, idx, 2).FirstOrDefault();
					if ((false ? null : shiftJisToAscii).ContainsKey(shiftJisChar))
						stringBuilder.Append((false ? null : shiftJisToAscii)[shiftJisChar]);
					else stringBuilder.Append(shiftJisChar);
				}
			}

			return stringBuilder.ToString();
		}

		private static (string Output, int Index) ControlCodeOneArgument(byte[] bytes, int idx, string description)
		{
			return ($"{description}{controlCodeArgSeperator}{(ushort)(bytes[idx + 3] << 8 | bytes[idx + 2]):D4}", (idx + 2));
		}

		// TODO: maybe clean below up a bit?

		public static byte[] GetBytes(string str)
		{
			var bytes = new List<byte>();

			for (int idx = 0; idx < str.Length; idx++)
			{
				var chr = str[idx];
				if (chr == '\r')
				{
					continue;
				}
				else if (chr == '\n')
				{
					bytes.AddRange(new byte[] { 0xF8, 0x01 });
				}
				else if (chr == controlCodeBegin)
				{
					var controlCodeLen = GetControlCodeLength(str, idx + 1);
					if (controlCodeLen != -1)
					{
						var controlCode = str.Substring(idx + 1, controlCodeLen);
						var codeString = GetControlCode(controlCode);
						var argString = GetControlCodeArgument(controlCode);

						if (codeString == controlCodePageBreak)
						{
							bytes.AddRange(new byte[] { 0xF8, 0x02 });
							idx += ((controlCodeLen + (Environment.NewLine.Length * 2)) + 1);
						}
						else if (controlCodeValues.ContainsKey(codeString))
						{
							var controlCodeValue = controlCodeValues[codeString];
							bytes.AddRange(new byte[] { (byte)(controlCodeValue >> 8), (byte)(controlCodeValue & 0xFF) });

							if (argString != string.Empty && ushort.TryParse(argString, out ushort arg))
							{
								bytes.AddRange(new byte[] { (byte)(arg & 0xFF), (byte)(arg >> 8) });
							}

							idx += (controlCodeLen + 1);
						}
						else
							bytes.AddRange(GetCharacterBytes(chr));
					}
					else
						bytes.AddRange(GetCharacterBytes(chr));
				}
				else
					bytes.AddRange(GetCharacterBytes(chr));
			}

			return bytes.ToArray();
		}

		private static byte[] GetCharacterBytes(char chr)
		{
			var sjisBytes = shiftJisEncoding.GetBytes(new char[]
			{
				((false ? null: asciiToShiftJis).ContainsKey(chr) ? (false ? null: asciiToShiftJis)[chr] : chr)
			});

			// TODO still needed in 2018?   ----vvvv

			/* Dirty hack, replace with space! Required ex. when there's some garbage in Excel imports... */
			//if (sjisBytes.Length == 1) sjisBytes = new byte[] { 0x81, 0x40 };

			return sjisBytes;
		}

		private static int GetControlCodeLength(string str, int idx)
		{
			var length = 0;
			for (; idx < str.Length; idx++)
			{
				if (str[idx] == controlCodeBegin) break;
				if (str[idx] == controlCodeEnd) return length;
				length++;
			}
			return -1;
		}

		private static string GetControlCode(string code)
		{
			var argIdx = code.IndexOf(controlCodeArgSeperator);
			if (argIdx == -1) return code;
			return code.Substring(0, argIdx);
		}

		private static string GetControlCodeArgument(string code)
		{
			var argIdx = code.IndexOf(controlCodeArgSeperator);
			if (argIdx == -1) return string.Empty;
			return code.Substring(argIdx + 1);
		}
	}
}
