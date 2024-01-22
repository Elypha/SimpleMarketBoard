#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CsvData
{
    public int Key { get; set; }
    public string Name { get; set; }
}

public class WorldParser
{
    private List<CsvData> data;

    public WorldParser(string csvFilePath)
    {
        LoadCsv(csvFilePath);
    }

    private void LoadCsv(string csvFilePath)
    {
        data = File.ReadLines(csvFilePath)
            .Skip(3) // Skip header line
            .Select(line => line.Split(','))
            .Select(parts => new CsvData
            {
                Key = int.Parse(parts[0]),
                Name = parts[2]
            })
            .ToList();
    }

    public string GetNameForKey(int key)
    {
        CsvData entry = data.FirstOrDefault(d => d.Key == key);
        return entry.Name.Substring(1, entry.Name.Length - 2);
    }

    public int GetKeyForName(string name)
    {
        CsvData entry = data.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return entry.Key;
    }
}
