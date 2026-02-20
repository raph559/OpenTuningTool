using System;
using OpenTuningTool.Parsing;
using OpenTuningTool.Models;

Console.WriteLine("Starting XDF Parser Test...");

// Create parser instance
XdfParser parser = new XdfParser();

// Call the parse method with file path
XdfDocument document = parser.Parse("Siemens_MS43_MS43X001_512K.xdf");

// Print result
Console.WriteLine("Parsing finished successfully!");
Console.WriteLine($"Number of tables in document: {document.Tables.Count}");
Console.WriteLine($"Number of constants in document: {document.Constants.Count}");