using System;
using System.IO;
using System.Linq;
using OpenTuningTool.Parsing;
using OpenTuningTool.Models;

Console.WriteLine("Starting XDF Parser Test...\n");

try
{
    // Create parser instance
    XdfParser parser = new XdfParser();

    // Call the parse method with file path
    XdfDocument document = parser.Parse("Siemens_MS43_MS43X001_512K.xdf");

    // Print general result
    Console.WriteLine("Parsing finished successfully!\n");
    Console.WriteLine($"Number of tables in document: {document.Tables.Count}");
    Console.WriteLine($"Number of constants in document: {document.Constants.Count}\n");

    // Test with first table
    if (document.Tables.Count > 0)
    {
        XdfTable firstTable = document.Tables.First();
        Console.WriteLine("--- First Table Details ---");
        Console.WriteLine($"Title: {firstTable.Title}");
        Console.WriteLine($"ID: 0x{firstTable.UniqueId:X}"); 
        Console.WriteLine($"X Axis (ID: {firstTable.XAxis?.Id}) - Index Count: {firstTable.XAxis?.IndexCount}");
        Console.WriteLine($"Y Axis (ID: {firstTable.YAxis?.Id}) - Index Count: {firstTable.YAxis?.IndexCount}");
        Console.WriteLine($"Table Data (Address: 0x{firstTable.ZAxis?.Address:X}) - Rows: {firstTable.ZAxis?.RowCount}, Cols: {firstTable.ZAxis?.ColCount}\n");
    }

    // Test with first constant
    if (document.Constants.Count > 0)
    {
        XdfConstant firstConstant = document.Constants.First();
        Console.WriteLine("--- First Constant Details ---");
        Console.WriteLine($"Title: {firstConstant.Title}");
        Console.WriteLine($"ID: 0x{firstConstant.UniqueId:X}");
        Console.WriteLine($"Address: 0x{firstConstant.Address:X}");
        Console.WriteLine($"Element Size: {firstConstant.ElementSizeBits} bits\n");
    }

	// Test with a table in the middle of the file
    if (document.Tables.Count > 0)
    {
        int middleTableIndex = document.Tables.Count / 2;
        XdfTable middleTable = document.Tables[middleTableIndex];

        Console.WriteLine($"--- Table Details (Index {middleTableIndex}) ---");
        Console.WriteLine($"Title: {middleTable.Title}");
        Console.WriteLine($"ID: 0x{middleTable.UniqueId:X}"); 
        Console.WriteLine($"X Axis (ID: {middleTable.XAxis?.Id}) - Index Count: {middleTable.XAxis?.IndexCount}");
        Console.WriteLine($"Y Axis (ID: {middleTable.YAxis?.Id}) - Index Count: {middleTable.YAxis?.IndexCount}");
        Console.WriteLine($"Table Data (Address: 0x{middleTable.ZAxis?.Address:X}) - Rows: {middleTable.ZAxis?.RowCount}, Cols: {middleTable.ZAxis?.ColCount}\n");
    }

    // Constant in the middle of the file
    if (document.Constants.Count > 0)
    {
        int middleConstantIndex = document.Constants.Count / 2;
        XdfConstant middleConstant = document.Constants[middleConstantIndex];

        Console.WriteLine($"--- Constant Details (Index {middleConstantIndex}) ---");
        Console.WriteLine($"Title: {middleConstant.Title}");
        Console.WriteLine($"ID: 0x{middleConstant.UniqueId:X}");
        Console.WriteLine($"Address: 0x{middleConstant.Address:X}");
        Console.WriteLine($"Element Size: {middleConstant.ElementSizeBits} bits\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine("ERROR DURING PARSING:");
    Console.WriteLine(ex.Message);
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();