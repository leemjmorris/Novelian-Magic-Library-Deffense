using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public class CsvUtility
{
    /// <summary>
    /// Load CSV from file path
    /// </summary>
    /// <typeparam name="T">Data Class Type</typeparam>
    /// <param name="filePath">CSV File Path</param>
    /// <returns>List of Data Class Instances</returns>
    public static List<T> LoadCsv<T>(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<T>().ToList();
            return records;
        }
    }

    public static List<T> LoadCsvFromText<T>(string csvText)
    {
        using (var reader = new StringReader(csvText))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<T>().ToList();
            return records;
        }
    }
    
    public static string SaveCsv<T>(IEnumerable<T> data)
    {
        using (var writer = new StringWriter())
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(data);
            return writer.ToString();
        }
    }
}
