using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

namespace TitanMessage
{
	class Program
	{
		// import "D:\Translation\Etrian Odyssey 4 German\RomFS (Original EUR)\" "D:\Translation\Etrian Odyssey 4 German\JSON\"
		// export "D:\Translation\Etrian Odyssey 4 German\JSON\" "D:\Translation\Etrian Odyssey 4 German\Generated Data\"

		static void Main(string[] args)
		{
			Console.WriteLine("Titan's Message - Etrian Odyssey IV text converter");
			Console.WriteLine("Written 2018 by xdaniel - https://github.com/xdanieldzd/");
			Console.WriteLine();

			args = CommandLineTools.CreateArgs(Environment.CommandLine);

			if (args.Length != 4)
				PrintUsageAndExit(-1);

			try
			{
				var mode = args[1];
				var sourceRoot = new DirectoryInfo(args[2]);
				var targetRoot = new DirectoryInfo(args[3]);

				switch (mode.ToLowerInvariant())
				{
					case "import":
						// Special cases: include only known valid TBLs, exclude known incompatible leftovers (TestData folder, EO3 seafaring, etc...)
						var importFiles = sourceRoot.EnumerateFiles("*", SearchOption.AllDirectories)
							.Where(x => (x.Extension == ".tbl" && (x.Name.EndsWith("nametable.tbl") || x.Name == "skyitemname.tbl")) || x.Extension == ".mbm")
							.Where(x => !x.DirectoryName.Contains("TestData") && !x.Name.StartsWith("sea") && !x.Name.StartsWith("FacilityEntranceText"));

						foreach (var importFile in importFiles)
						{
							var relative = importFile.DirectoryName.Replace(sourceRoot.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
							ProcessFile(importFile.FullName, targetRoot.FullName, relative);
						}
						break;

					case "export":
						var exportFiles = sourceRoot.EnumerateFiles("*.json", SearchOption.AllDirectories);
						foreach (var exportFile in exportFiles)
						{
							var relative = exportFile.DirectoryName.Replace(sourceRoot.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
							ProcessFile(exportFile.FullName, targetRoot.FullName, relative);
						}
						break;

					default:
						throw new Exception($"Unknown conversion mode '{mode}'");
				}

				Console.WriteLine();
				Console.WriteLine("Press any key to exit.");
				Console.ReadKey();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception occured: {ex.Message}");
				Console.WriteLine();
				PrintUsageAndExit(-1);
			}
		}

		static void PrintUsageAndExit(int code)
		{
			Console.WriteLine("Usage: [import | export] <source path> <target path>");
			Console.WriteLine();
			Console.WriteLine("  import        Convert TBL/MBM to JSON");
			Console.WriteLine("  export        Convert JSON to TBL/MBM");
			Console.WriteLine("  source path   Path to source files to be converted");
			Console.WriteLine("  target path   Path to write converted files to");
			Console.WriteLine();
			Console.WriteLine("Example: TitanMessage.exe import \"C:\\EO4\\RomFS\\\" \"C:\\EO4\\JSON\\\"");
			Console.WriteLine();
			Console.WriteLine("Use 'import' OR 'export' mode. Directory structure is preserved.");
			Console.WriteLine();
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();

			Environment.Exit(code);
		}

		static void ProcessFile(string sourceFileName, string targetRoot, string relativePath)
		{
			string outputFullName = string.Empty;
			Translation translationFile = null;

			switch (Path.GetExtension(sourceFileName))
			{
				case ".tbl":
				case ".mbm":
					outputFullName = Path.Combine(targetRoot, relativePath, $"{Path.GetFileNameWithoutExtension(sourceFileName)}.json");
					break;

				case ".json":
					translationFile = sourceFileName.DeserializeFromFile<Translation>();
					outputFullName = Path.Combine(targetRoot, translationFile.RelativePath);
					break;
			}

			if (File.Exists(outputFullName))
			{
				Console.WriteLine($"File {Path.GetFileName(outputFullName)} already exists, skipping...");
				return;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(outputFullName));

			switch (Path.GetExtension(sourceFileName))
			{
				case ".tbl":
					Console.WriteLine($"Converting {Path.GetFileName(sourceFileName)} to JSON...");
					translationFile = StringTableHandler.ImportBinary(sourceFileName, relativePath);
					translationFile.SerializeToFile(outputFullName);
					break;

				case ".mbm":
					Console.WriteLine($"Converting {Path.GetFileName(sourceFileName)} to JSON...");
					translationFile = MessageBinaryHandler.ImportBinary(sourceFileName, relativePath);
					translationFile.SerializeToFile(outputFullName);
					break;

				case ".json":
					switch (translationFile.FileType)
					{
						case StringTableHandler.FileType:
							Console.WriteLine($"Converting {Path.GetFileName(sourceFileName)} to TBL...");
							StringTableHandler.ExportBinary(translationFile, outputFullName);
							break;

						case MessageBinaryHandler.FileType:
							Console.WriteLine($"Converting {Path.GetFileName(sourceFileName)} to MBM...");
							MessageBinaryHandler.ExportBinary(translationFile, outputFullName);
							break;
					}
					break;
			}
		}
	}
}
