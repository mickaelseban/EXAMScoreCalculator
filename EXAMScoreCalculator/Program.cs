using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EXAMScoreCalculator
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 1 && string.Equals(args[1], "debug", StringComparison.OrdinalIgnoreCase))
            {
                Debugger.Launch();
            }

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the folder path containing the JSON files.");
                return;
            }

            var folderPath = Path.GetFullPath(args[0]);

            Console.WriteLine($"Normalized folder path: {folderPath}");

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"The provided path '{folderPath}' is invalid.");
                return;
            }

            var techniquesSuspiciousnessScores = GetTechniquesSuspiciousnessScores(folderPath);
            var examScore = CalculateExamScore(techniquesSuspiciousnessScores);
            foreach (var kv in examScore)
            {
                Console.WriteLine($"Technique: {kv.Key}");
                Console.WriteLine($"Exam Score: {kv.Value}");
            }

            var jsonString = JsonConvert.SerializeObject(examScore, Formatting.Indented);

            var filePath = Path.Combine(folderPath, $"{Guid.NewGuid().ToString()} - EXAM Score.json");
            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.Write(jsonString);
                streamWriter.Flush();
            }
        }

        private static Dictionary<string, List<double>> GetTechniquesSuspiciousnessScores(string folderPath)
        {
            var aggregatedScores = new Dictionary<string, List<double>>();

            foreach (var filePath in Directory.GetFiles(folderPath, "*.json"))
            {
                var jsonContent = File.ReadAllText(filePath);
                var suspiciousnessResult = JsonConvert.DeserializeObject<SuspiciousnessResult>(jsonContent);

                foreach (var techniqueEntry in suspiciousnessResult.Techniques)
                {
                    var techniqueName = techniqueEntry.Key;

                    if (!aggregatedScores.ContainsKey(techniqueName))
                    {
                        aggregatedScores[techniqueName] = [];
                    }

                    var technique = techniqueEntry.Value;

                    foreach (var line in 
                             from assemblyEntry in technique.Assemblies 
                             select assemblyEntry.Value into assembly 
                             from fileEntry in assembly.Files 
                             select fileEntry.Value into file 
                             from classEntry in file.Classes 
                             select classEntry.Value into classObj from methodEntry in classObj.Methods 
                             select methodEntry.Value into method from lineEntry in method.Lines 
                             select lineEntry.Value)
                    {
                        aggregatedScores[techniqueName].Add(line.Score);
                    }
                }
            }

            return aggregatedScores;
        }

        public static Dictionary<string, double> CalculateExamScore(Dictionary<string, List<double>> techniquesScores)
        {
            var examScores = new Dictionary<string, double>();

            foreach ((var techniqueType, var scores) in techniquesScores)
            {
                var examScore = Math.Round(scores.Sum(x => x / scores.Count) / scores.Count, 4);

                examScores.Add(techniqueType, examScore);
            }

            return examScores;
        }
    }
}