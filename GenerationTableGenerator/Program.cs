using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenerationTableGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Invalid input.");
                Console.WriteLine("Format: GenerationTableGenerator.exe <packages-directory> <filename>");
                return;
            }

            var packagesDir = args[0];
            var targetFile = args[1];

            var packageDirs = Directory.EnumerateDirectories(packagesDir)
                .Select(dirPath => new DirectoryInfo(dirPath))
                .OrderBy(directoryInfo => directoryInfo.Name);

            var generationTableRows = new List<List<string>>();
            generationTableRows.Add(new string[] { "Contract", "5.1", "5.2", "5.3", "5.4", "5.5" }.ToList());
            foreach (var contractDir in packageDirs)
            {
                var row = GetContractRow(contractDir);
                if (row != null)
                {
                    generationTableRows.Add(row);
                }
            }

            var markdown = GenerateTableInMarkdown(generationTableRows);

            File.AppendAllText(targetFile, markdown);
        }

        private static string GenerateTableInMarkdown(List<List<string>> generationTable)
        {
            var outputBuilder = new StringBuilder();

            for (var i = 0; i < generationTable.Count; i++)
            {
                if (i == 0)
                {
                    var headerRow = generationTable[i];
                    AddRow(outputBuilder, headerRow);
                    AddRow(outputBuilder, headerRow.Select(headerName => new string('-', headerName.Length)));
                    continue;
                }

                AddRow(outputBuilder, generationTable[i]);
            }

            return outputBuilder.ToString();
        }

        private static void AddRow(StringBuilder outputBuilder, IEnumerable<string> row)
        {
            var s = string.Join(" | ", row);
            outputBuilder.AppendLine($"| {s} |");
        }

        private static List<string> GetContractRow(DirectoryInfo contractDir)
        {
            var contractInfo = new ContractInfo(contractDir);

            var formattedGenerations = new string[contractInfo.Generations.Count];
            KeyValuePair<string, bool>? previousGeneration = null;
            for (var i = 0; i < contractInfo.Generations.Count; i++)
            {
                var generation = contractInfo.Generations[i];

                if (generation.Value)
                {
                    formattedGenerations[i] = "X";
                    previousGeneration = generation;
                }
                else
                {
                    // for superscript
                    //formattedGenerations[i] =
                    //    (previousGeneration != null) ? $"X<sup>{previousGeneration.Value.Key}</sup>" : string.Empty;

                    formattedGenerations[i] = (previousGeneration != null) ? "⇠" : string.Empty;
                }
            }

            if (!formattedGenerations.All(version => string.IsNullOrEmpty(version)))
            {
                var rowColumns = new List<string>();
                rowColumns.Add(contractInfo.Name);
                rowColumns.AddRange(formattedGenerations);
                return rowColumns;
            }

            return null;
        }

        private class ContractInfo
        {
            public ContractInfo(DirectoryInfo dir)
            {
                Name = dir.Name;
                Generations = new List<KeyValuePair<string, bool>>(5);

                var refDir = dir.GetDirectories("ref", SearchOption.AllDirectories).FirstOrDefault();
                if (refDir != null)
                {
                    Generations.Add(new KeyValuePair<string, bool>("5.1", refDir.GetDirectories("dotnet5.1").Length == 1));
                    Generations.Add(new KeyValuePair<string, bool>("5.2", refDir.GetDirectories("dotnet5.2").Length == 1));
                    Generations.Add(new KeyValuePair<string, bool>("5.3", refDir.GetDirectories("dotnet5.3").Length == 1));
                    Generations.Add(new KeyValuePair<string, bool>("5.4", refDir.GetDirectories("dotnet5.4").Length == 1));
                    Generations.Add(new KeyValuePair<string, bool>("5.5", refDir.GetDirectories("dotnet5.5").Length == 1));
                }
            }

            public string Name { get; }

            public List<KeyValuePair<string, bool>> Generations { get; }
        }
    }
}
