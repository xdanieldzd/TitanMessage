using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

using TitanTools;

namespace TitanMessage
{
	class Program
	{
		// --overwrite --json "D:\Translation\Etrian Odyssey 4 German\RomFS (Original EUR)\" "D:\Translation\Etrian Odyssey 4 German\JSON\" --binary "D:\Translation\Etrian Odyssey 4 German\JSON\" "D:\Translation\Etrian Odyssey 4 German\Generated Data\"

		readonly static List<ConsoleHelper.ArgumentHandler> argumentHandlers = new List<ConsoleHelper.ArgumentHandler>()
		{
			{ new ConsoleHelper.ArgumentHandler("json", "j", "Convert binary files to JSON files", "[source path] [target path]", ArgumentHandlerBinaryToJson) },
			{ new ConsoleHelper.ArgumentHandler("binary", "b", "Convert JSON files to binary files", null, ArgumentHandlerJsonToBinary) },
			{ new ConsoleHelper.ArgumentHandler("overwrite", "o", "Allow overwriting of existing files", null, (arg) => { overwriteExistingFiles = true; }) },
			{ new ConsoleHelper.ArgumentHandler("ignore", "i", "Ignore untranslated files on binary creation", null, (arg) => { ignoreUntranslatedFiles = true; }) },
			{ new ConsoleHelper.ArgumentHandler("unattended", "u", "Run unattended, i.e. don't wait for key on exit", null, (arg) => { verbose = false; }) },
			{ new ConsoleHelper.ArgumentHandler("characters", "c", "Load character overrides", "[override JSON path]", ArgumentHandlerCharaOverrides) },
		};

		static bool overwriteExistingFiles = false;
		static bool ignoreUntranslatedFiles = false;
		static bool verbose = true;

		static void Main()
		{
			ConsoleHelper.PrintApplicationInformation();

			var args = ConsoleHelper.GetAndVerifyArguments(1, () =>
			{
				Console.WriteLine("No arguments specified!");
				Console.WriteLine();
				ConsoleHelper.PrintUsageAndExit(argumentHandlers, -1);
			});

			ConsoleHelper.ExecuteArguments(args, argumentHandlers, ref verbose);
		}

		static string GetRelativePath(FileInfo sourceFile, DirectoryInfo sourceRoot)
		{
			return sourceFile.DirectoryName.Replace(sourceRoot.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		static bool CheckOkayToContinue(FileInfo outputFileInfo)
		{
			if (outputFileInfo.Exists && !overwriteExistingFiles)
			{
				Console.WriteLine($"[-] File {outputFileInfo.Name} already exists, skipping...");
				return false;
			}
			else
				return true;
		}

		static void ArgumentHandlerBinaryToJson(string[] args)
		{
			if (args.Length != 3) throw new Exception($"Invalid number of arguments for {args[0]}");

			var sourceRoot = new DirectoryInfo(args[1]);
			var targetRoot = new DirectoryInfo(args[2]);

			// Special cases: include only known valid TBLs, exclude known incompatible leftovers (TestData folder, EO3 seafaring, etc...)
			var binaryFiles = sourceRoot.EnumerateFiles("*", SearchOption.AllDirectories)
				.Where(x => (x.Extension == ".tbl" && (x.Name.EndsWith("nametable.tbl") || x.Name == "skyitemname.tbl")) || x.Extension == ".mbm")
				.Where(x => !x.DirectoryName.Contains("TestData") && !x.Name.StartsWith("sea") && !x.Name.StartsWith("FacilityEntranceText"));

			foreach (var binaryFile in binaryFiles)
			{
				var relativePath = GetRelativePath(binaryFile, sourceRoot);
				var outputFileInfo = new FileInfo(Path.Combine(targetRoot.FullName, relativePath, $"{Path.GetFileNameWithoutExtension(binaryFile.FullName)}.json"));

				if (CheckOkayToContinue(outputFileInfo))
				{
					Console.WriteLine($"[*] Converting binary {binaryFile.Name} to JSON...");

					outputFileInfo.Directory.Create();

					Translation translationFile;
					switch (binaryFile.Extension)
					{
						case ".tbl": translationFile = StringTableHandler.ImportBinary(binaryFile.FullName, relativePath); break;
						case ".mbm": translationFile = MessageBinaryHandler.ImportBinary(binaryFile.FullName, relativePath); break;
						default: throw new Exception($"Unrecognized file extension {binaryFile.Extension}");
					}
					translationFile.SerializeToFile(outputFileInfo.FullName);
				}
			}
		}

		static void ArgumentHandlerJsonToBinary(string[] args)
		{
			if (args.Length != 3) throw new Exception($"Invalid number of arguments for {args[0]}");

			var sourceRoot = new DirectoryInfo(args[1]);
			var targetRoot = new DirectoryInfo(args[2]);

			var jsonFiles = sourceRoot.EnumerateFiles("*.json", SearchOption.AllDirectories);

			foreach (var jsonFile in jsonFiles)
			{
				Translation translationFile = jsonFile.FullName.DeserializeFromFile<Translation>();

				if (ignoreUntranslatedFiles && !translationFile.Entries.Any(x => string.Compare(x.Original, x.Translation) != 0))
				{
					Console.WriteLine($"[-] File {jsonFile.Name} has no translated entries, skipping...");
					continue;
				}

				var relativePath = GetRelativePath(jsonFile, sourceRoot);
				var outputFileInfo = new FileInfo(Path.Combine(targetRoot.FullName, translationFile.RelativePath));

				if (CheckOkayToContinue(outputFileInfo))
				{
					Console.WriteLine($"[*] Converting JSON {jsonFile.Name} to binary...");

					outputFileInfo.Directory.Create();

					switch (translationFile.FileType)
					{
						case StringTableHandler.FileType: StringTableHandler.ExportBinary(translationFile, outputFileInfo.FullName); break;
						case MessageBinaryHandler.FileType: MessageBinaryHandler.ExportBinary(translationFile, outputFileInfo.FullName); break;
						default: throw new Exception($"Unrecognized translation type {translationFile.FileType}");
					}
				}
			}
		}

		static void ArgumentHandlerCharaOverrides(string[] args)
		{
			if (args.Length != 2) throw new Exception($"Invalid number of arguments for {args[0]}");

			var jsonObject = JObject.Parse(File.ReadAllText(args[1]));
			var charaOverrides = jsonObject["CharacterOverrides"].ToObject<Dictionary<char, char>>();

			Console.WriteLine($"[*] Loading {charaOverrides.Count} character override{((charaOverrides.Count == 1) ? "" : "s")}...");

			TextHelper.SetCharacterOverrides(charaOverrides);
		}
	}
}
