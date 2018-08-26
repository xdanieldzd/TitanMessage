using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TitanMessage
{
	public static class StringTableHandler
	{
		public const string FileType = "StringTable";

		public static Translation ImportBinary(string fileName, string relativePath)
		{
			var jsonFile = new Translation(FileType, Path.Combine(relativePath, Path.GetFileName(fileName)));

			using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				var numStrings = reader.ReadUInt16();

				var stringEndOffsets = new ushort[numStrings];
				for (int i = 0; i < stringEndOffsets.Length; i++) stringEndOffsets[i] = reader.ReadUInt16();

				var stringTableOffset = reader.BaseStream.Position;
				var lastStringOffset = stringTableOffset;

				jsonFile.Entries = new TranslatableEntry[numStrings];
				for (int i = 0; i < numStrings; i++)
				{
					var start = (i == 0 ? stringTableOffset : lastStringOffset);
					var end = (stringTableOffset + stringEndOffsets[i]);

					var length = (int)(end - start - 1);

					reader.BaseStream.Seek(lastStringOffset, SeekOrigin.Begin);
					var text = TextHelper.GetString(reader.ReadBytes(length));
					reader.ReadByte();

					jsonFile.Entries[i] = new TranslatableEntry()
					{
						ID = i,
						Original = text,
						Translation = text,
						Notes = string.Empty
					};

					lastStringOffset = reader.BaseStream.Position;
				}
			}

			return jsonFile;
		}

		public static void ExportBinary(Translation translation, string fileName)
		{
			using (BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
			{
				writer.Write((ushort)translation.Entries.Length);

				var offsetListPosition = writer.BaseStream.Position;
				var stringEndOffsets = new ushort[translation.Entries.Length];

				writer.Write(new byte[stringEndOffsets.Length * sizeof(char)]);

				var currentEndOffset = 0;
				for (int i = 0; i < stringEndOffsets.Length; i++)
				{
					var translationEntry = translation.Entries[i];
					currentEndOffset += ((translationEntry.Translation.Length * sizeof(char)) + 1);

					writer.Write(TextHelper.GetBytes(translationEntry.Translation));
					writer.Write((byte)0x00);

					stringEndOffsets[i] = (ushort)currentEndOffset;
				}

				writer.BaseStream.Position = offsetListPosition;
				foreach (var endOffset in stringEndOffsets) writer.Write(endOffset);
			}
		}
	}
}
