using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TitanMessage
{
	public static class MessageBinaryHandler
	{
		public const string FileType = "MessageBinary";

		public static Translation ImportBinary(string fileName, string relativePath)
		{
			var jsonFile = new Translation(FileType, Path.Combine(relativePath, Path.GetFileName(fileName)));

			using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
			{
				var strings = new List<TranslatableEntry>();
				var validStrings = 0;

				/* Read and check magic number */
				reader.BaseStream.Seek(0x04, SeekOrigin.Begin);
				if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "MSG2") throw new Exception("Magic number mismatch; not an MBM file?");

				/* Read other header values */
				reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
				var numValidStrings = reader.ReadUInt32();
				var messageTableOffset = reader.ReadUInt32();

				/* Begin reading message table */
				reader.BaseStream.Seek(messageTableOffset, SeekOrigin.Begin);
				while (validStrings < numValidStrings)
				{
					/* Read ID, size and offset */
					var id = (int)reader.ReadUInt32();
					var numBytes = reader.ReadUInt32();
					var stringOffset = reader.ReadUInt32();

					/* Ensure entry is valid, i.e. has a size */
					if (numBytes != 0)
					{
						var streamPosition = reader.BaseStream.Position;

						/* Convert text data to string and store in translatable format */
						reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
						var text = TextHelper.GetString(reader.ReadBytes((int)numBytes));

						strings.Add(new TranslatableEntry()
						{
							ID = id,
							Original = text,
							Translation = text,
							Notes = string.Empty
						});

						reader.BaseStream.Seek(streamPosition + 4, SeekOrigin.Begin);
						validStrings++;
					}
					else
					{
						/* Just create a dummy entry */
						strings.Add(new TranslatableEntry()
						{
							ID = -1,
							Original = string.Empty,
							Translation = string.Empty,
							Notes = string.Empty
						});

						reader.BaseStream.Seek(4, SeekOrigin.Current);
					}
				}

				jsonFile.Entries = strings.ToArray();
			}

			return jsonFile;
		}

		public static void ExportBinary(Translation translation, string fileName)
		{
			using (BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
			{
				/* Write static stuff: 0, magic number, 65535 */
				writer.Write((uint)0);
				writer.Write(Encoding.ASCII.GetBytes("MSG2"));
				writer.Write((uint)65536);

				/* Write filesize dummy value, number of valid entries */
				var fileSizePosition = writer.BaseStream.Position;
				writer.Write((uint)0);
				writer.Write((uint)translation.Entries.Count(x => x.ID != -1));

				/* Write message table offset & padding */
				writer.Write((uint)0x20);
				writer.Write(new byte[8]);

				/* Write message table dummy data */
				var entryStartPosition = writer.BaseStream.Position;
				writer.Write(new byte[translation.Entries.Length * 0x10]);

				for (int i = 0; i < translation.Entries.Length; i++)
				{
					var translationEntry = translation.Entries[i];
					if (translationEntry.ID != -1)
					{
						/* Convert string and write text data */
						var textPosition = writer.BaseStream.Position;
						var textData = TextHelper.GetBytes(translationEntry.Translation).Concat(new byte[] { 0xFF, 0xFF }).ToArray();
						writer.Write(textData);
						var textEndPosition = writer.BaseStream.Position;

						/* Seek to correct message table entry position, then write entry */
						writer.BaseStream.Position = entryStartPosition + (i * 0x10);
						writer.Write((uint)translationEntry.ID);
						writer.Write((uint)textData.Length);
						writer.Write((uint)textPosition);
						writer.Write((uint)0);

						writer.BaseStream.Position = textEndPosition;
					}
				}

				/* Seek to filesize value position, then write filesize *excluding* dummy table entries */
				writer.BaseStream.Position = fileSizePosition;
				writer.Write((uint)(writer.BaseStream.Length - (translation.Entries.Count(x => x.ID == -1) * 0x10)));
			}
		}
	}
}
