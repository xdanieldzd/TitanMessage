using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TitanMessage
{
	class Program
	{
		// --overwrite --json "D:\Translation\Etrian Odyssey 4 German\RomFS (Original EUR)\" "D:\Translation\Etrian Odyssey 4 German\JSON\" --binary "D:\Translation\Etrian Odyssey 4 German\JSON\" "D:\Translation\Etrian Odyssey 4 German\Generated Data\"

		readonly static List<(string Long, string Short, string Description, string Syntax, Action<string[]> Method)> argumentHandlers = new List<(string Long, string Short, string Description, string Syntax, Action<string[]> Method)>()
		{
			{ ("json", "j", "Convert binary files to JSON files", "[source path] [target path]", ArgumentHandlerBinaryToJson) },
			{ ("binary", "b", "Convert JSON files to binary files", null, ArgumentHandlerJsonToBinary) },
			{ ("overwrite", "o", "Allow overwriting of existing files", null, (arg) => { overwriteExistingFiles = true; }) },
		};

		static bool overwriteExistingFiles = false;

		static string applicationExecutable;

		static void Main(string[] args)
		{
			Console.WriteLine("Titan's Message - Etrian Odyssey IV text converter");
			Console.WriteLine("Written 2018 by xdaniel - https://github.com/xdanieldzd/");
			Console.WriteLine();

			args = CommandLineTools.CreateArgs(Environment.CommandLine);
			applicationExecutable = Path.GetFileName(args[0]);

			if (args.Length <= 1)
			{
				Console.WriteLine("No arguments specified!");
				Console.WriteLine();
				PrintUsageAndExit(-1);
			}

			foreach (var argGroup in ParseArguments(args))
			{
				try
				{
					argumentHandlers.FirstOrDefault(x => x.Long == argGroup[0] || x.Short == argGroup[0]).Method?.Invoke(argGroup);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Exception occured: {ex.Message}");
					Console.WriteLine();
					PrintUsageAndExit(-1);
				}
			}
		}

		static List<string[]> ParseArguments(string[] args)
		{
			var argGroups = new List<string[]>();
			for (int argIdx = 1; argIdx < args.Length; argIdx++)
			{
				if (args[argIdx].StartsWith("-"))
				{
					var argGroup = new List<string> { args[argIdx].TrimStart('-') };
					argGroup.AddRange(args.Skip(argIdx + 1).TakeWhile(x => !x.StartsWith("-")));
					argGroups.Add(argGroup.ToArray());
					argIdx += (argGroup.Count - 1);
				}
			}
			return argGroups;
		}

		static string GetRelativePath(FileInfo sourceFile, DirectoryInfo sourceRoot)
		{
			return sourceFile.DirectoryName.Replace(sourceRoot.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		static bool CheckOkayToContinue(FileInfo outputFileInfo)
		{
			if (outputFileInfo.Exists && !overwriteExistingFiles)
			{
				Console.WriteLine($"File {outputFileInfo.Name} already exists, skipping...");
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
					Console.WriteLine($"Converting binary {binaryFile.Name} to JSON...");

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

				var relativePath = GetRelativePath(jsonFile, sourceRoot);
				var outputFileInfo = new FileInfo(Path.Combine(targetRoot.FullName, translationFile.RelativePath));

				if (CheckOkayToContinue(outputFileInfo))
				{
					Console.WriteLine($"Converting JSON {jsonFile.Name} to binary...");

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

		static void PrintUsageAndExit(int code)
		{
			Console.WriteLine($"Usage: {applicationExecutable} [options]...");
			Console.WriteLine();
			Console.WriteLine("Options:");

			var maxSpecifierLength = argumentHandlers.Select(x => $" {x.Short}{(x.Long != string.Empty ? "," : " ")} {x.Long}").Max(x => x.Length);
			foreach (var (Long, Short, Description, Syntax, _) in argumentHandlers)
			{
				var specifierString = $"{Short}{(Long != string.Empty ? "," : " ")} {Long}";
				var padding = string.Empty.PadRight((maxSpecifierLength + 3) - specifierString.Length, ' ');
				Console.WriteLine($" {specifierString}{padding}{Description}");
				if (!string.IsNullOrWhiteSpace(Syntax))
					Console.WriteLine($"{new string(' ', specifierString.Length)}{padding}  [{specifierString.Replace(", ", "|")}] {Syntax}");
			}
			Console.WriteLine();
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();

			Environment.Exit(code);
		}
	}
}
