using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public static class RaftConfigExcelImporter
{
    const string OutputDirectory = "Assets/Resources/RaftConfigs";

    [MenuItem("Tools/Raft/Import Config Excel...")]
    public static void ImportFromDialog()
    {
        string excelPath = EditorUtility.OpenFilePanel("Select Raft Config Excel", Application.dataPath, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
            return;

        try
        {
            Import(excelPath);
            EditorUtility.DisplayDialog("Raft Config Import", "Excel was converted to JSON successfully.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Raft Config Import Failed", ex.Message, "OK");
        }
    }

    public static void Import(string excelPath)
    {
        if (!File.Exists(excelPath))
            throw new FileNotFoundException("Excel file was not found.", excelPath);

        Directory.CreateDirectory(OutputDirectory);

        var workbook = ExcelWorkbook.Load(excelPath);

        WriteJson("BuildingTable.json", ImportBuildingTable(workbook.RequireSheet("building")));
        WriteJson("ItemTable.json", ImportItemTable(workbook.RequireSheet("item")));
        WriteJson("RefreshTable.json", ImportRefreshTable(workbook.RequireSheet("refresh")));
        WriteJson("SurvivalTable.json", ImportSurvivalTable(workbook.RequireSheet("survival")));
        WriteJson("SynthesisTable.json", ImportSynthesisTable(workbook.RequireSheet("synthesis")));

        AssetDatabase.Refresh();
        Debug.Log($"Imported Raft config excel: {excelPath}");
    }

    static BuildingTable ImportBuildingTable(ExcelSheet sheet)
    {
        var configs = new Dictionary<int, BuildingConfig>();

        foreach (var row in sheet.Rows)
        {
            int buildingId = row.GetInt("buildingId");
            if (!configs.TryGetValue(buildingId, out var config))
            {
                config = new BuildingConfig
                {
                    buildingId = buildingId,
                    displayName = row.GetOrDefault("displayName", buildingId.ToString(CultureInfo.InvariantCulture))
                };
                configs.Add(buildingId, config);
            }

            int costItemTypeId = row.GetInt("costItemTypeId");
            int costAmount = row.GetInt("costAmount");
            var costs = config.costs == null ? new List<ItemAmountEntry>() : new List<ItemAmountEntry>(config.costs);
            costs.Add(new ItemAmountEntry
            {
                itemTypeId = costItemTypeId,
                amount = costAmount
            });
            config.costs = costs.ToArray();
        }

        return new BuildingTable
        {
            buildings = configs.Values.ToArray()
        };
    }

    static ItemTable ImportItemTable(ExcelSheet sheet)
    {
        var items = new List<ItemConfig>();
        foreach (var row in sheet.Rows)
        {
            items.Add(new ItemConfig
            {
                itemTypeId = row.GetInt("itemTypeId"),
                displayName = row.GetOrDefault("displayName", row.GetRequired("itemTypeId")),
                hungerRestore = row.GetFloat("hungerRestore"),
                thirstRestore = row.GetFloat("thirstRestore")
            });
        }

        return new ItemTable
        {
            items = items.ToArray()
        };
    }

    static RefreshTable ImportRefreshTable(ExcelSheet sheet)
    {
        var table = new RefreshTable();
        var resources = new List<RefreshRule>();

        foreach (var row in sheet.Rows)
        {
            string rowType = row.GetRequired("rowType").Trim().ToLowerInvariant();
            if (rowType == "setting")
            {
                string key = row.GetRequired("key");
                string value = row.GetRequired("value");
                ApplyRefreshSetting(table, key, value);
            }
            else if (rowType == "resource")
            {
                resources.Add(new RefreshRule
                {
                    itemTypeId = row.GetInt("itemTypeId"),
                    weight = row.GetFloat("weight")
                });
            }
            else
            {
                throw new InvalidDataException($"Unknown refresh rowType: {rowType}");
            }
        }

        table.resources = resources.ToArray();
        return table;
    }

    static SurvivalTable ImportSurvivalTable(ExcelSheet sheet)
    {
        var table = new SurvivalTable();

        foreach (var row in sheet.Rows)
        {
            string key = row.GetRequired("key");
            float value = row.GetFloat("value");
            ApplySurvivalSetting(table, key, value);
        }

        return table;
    }

    static SynthesisTable ImportSynthesisTable(ExcelSheet sheet)
    {
        var recipes = new Dictionary<string, SynthesisRecipe>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            string recipeId = row.GetRequired("recipeId");
            if (!recipes.TryGetValue(recipeId, out var recipe))
            {
                recipe = new SynthesisRecipe
                {
                    recipeId = recipeId,
                    displayName = row.GetOrDefault("displayName", recipeId),
                    outputItemTypeId = row.GetInt("outputItemTypeId"),
                    outputAmount = row.GetInt("outputAmount")
                };
                recipes.Add(recipeId, recipe);
            }

            int inputItemTypeId = row.GetInt("inputItemTypeId");
            int inputAmount = row.GetInt("inputAmount");
            var inputs = recipe.inputs == null ? new List<ItemAmountEntry>() : new List<ItemAmountEntry>(recipe.inputs);
            inputs.Add(new ItemAmountEntry
            {
                itemTypeId = inputItemTypeId,
                amount = inputAmount
            });
            recipe.inputs = inputs.ToArray();
        }

        return new SynthesisTable
        {
            recipes = recipes.Values.ToArray()
        };
    }

    static void ApplyRefreshSetting(RefreshTable table, string key, string value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "maxresources":
                table.maxResources = ParseInt(value);
                break;
            case "spawninterval":
                table.spawnInterval = ParseFloat(value);
                break;
            case "minspawndistance":
                table.minSpawnDistance = ParseFloat(value);
                break;
            case "maxspawndistance":
                table.maxSpawnDistance = ParseFloat(value);
                break;
            case "despawndistance":
                table.despawnDistance = ParseFloat(value);
                break;
            default:
                throw new InvalidDataException($"Unknown refresh setting key: {key}");
        }
    }

    static void ApplySurvivalSetting(SurvivalTable table, string key, float value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "maxhealth": table.maxHealth = value; break;
            case "maxhunger": table.maxHunger = value; break;
            case "maxthirst": table.maxThirst = value; break;
            case "initialhealth": table.initialHealth = value; break;
            case "initialhunger": table.initialHunger = value; break;
            case "initialthirst": table.initialThirst = value; break;
            case "hungerrate": table.hungerRate = value; break;
            case "thirstrate": table.thirstRate = value; break;
            case "starvedamage": table.starveDamage = value; break;
            case "respawndelay": table.respawnDelay = value; break;
            case "respawnhealth": table.respawnHealth = value; break;
            case "respawnhunger": table.respawnHunger = value; break;
            case "respawnthirst": table.respawnThirst = value; break;
            default:
                throw new InvalidDataException($"Unknown survival setting key: {key}");
        }
    }

    static void WriteJson(string fileName, object payload)
    {
        string jsonPath = Path.Combine(OutputDirectory, fileName);
        string json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
    }

    static int ParseInt(string raw)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new InvalidDataException($"Failed to parse int: {raw}");
        return value;
    }

    static float ParseFloat(string raw)
    {
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new InvalidDataException($"Failed to parse float: {raw}");
        return value;
    }

    sealed class ExcelWorkbook
    {
        readonly Dictionary<string, ExcelSheet> sheets;

        ExcelWorkbook(Dictionary<string, ExcelSheet> sheets)
        {
            this.sheets = sheets;
        }

        public ExcelSheet RequireSheet(string sheetName)
        {
            if (!sheets.TryGetValue(sheetName, out var sheet))
                throw new InvalidDataException($"Missing sheet: {sheetName}");
            return sheet;
        }

        public static ExcelWorkbook Load(string excelPath)
        {
            using var stream = File.OpenRead(excelPath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace docRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            var sharedStrings = LoadSharedStrings(archive, mainNs);
            var workbookXml = LoadXml(archive, "xl/workbook.xml");
            var relsXml = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            var relMap = relsXml.Root.Elements(relNs + "Relationship")
                .ToDictionary(
                    e => (string)e.Attribute("Id"),
                    e => NormalizeZipPath("xl/" + (string)e.Attribute("Target")),
                    StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, ExcelSheet>(StringComparer.OrdinalIgnoreCase);
            var sheetsElement = workbookXml.Root.Element(mainNs + "sheets");
            if (sheetsElement == null)
                throw new InvalidDataException("Workbook does not contain a sheets section.");

            foreach (var sheetElement in sheetsElement.Elements(mainNs + "sheet"))
            {
                string name = ((string)sheetElement.Attribute("name") ?? string.Empty).Trim();
                string relId = (string)sheetElement.Attribute(docRelNs + "id");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relId) || !relMap.TryGetValue(relId, out var path))
                    continue;

                var sheetXml = LoadXml(archive, path);
                result[name] = ExcelSheet.Parse(sheetXml, sharedStrings, mainNs);
            }

            return new ExcelWorkbook(result);
        }

        static List<string> LoadSharedStrings(ZipArchive archive, XNamespace mainNs)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            var xml = LoadXml(entry);
            return xml.Root.Elements(mainNs + "si")
                .Select(si => string.Concat(si.Descendants(mainNs + "t").Select(t => t.Value)))
                .ToList();
        }

        static XDocument LoadXml(ZipArchive archive, string entryPath)
        {
            var entry = archive.GetEntry(NormalizeZipPath(entryPath));
            if (entry == null)
                throw new InvalidDataException($"Missing workbook part: {entryPath}");
            return LoadXml(entry);
        }

        static XDocument LoadXml(ZipArchiveEntry entry)
        {
            using var entryStream = entry.Open();
            return XDocument.Load(entryStream);
        }

        static string NormalizeZipPath(string path)
        {
            return path.Replace("\\", "/").Replace("/./", "/");
        }
    }

    sealed class ExcelSheet
    {
        public List<ExcelRow> Rows { get; }

        ExcelSheet(List<ExcelRow> rows)
        {
            Rows = rows;
        }

        public static ExcelSheet Parse(XDocument xml, List<string> sharedStrings, XNamespace ns)
        {
            var sheetData = xml.Root.Element(ns + "sheetData");
            if (sheetData == null)
                return new ExcelSheet(new List<ExcelRow>());

            var rawRows = new List<Dictionary<int, string>>();
            foreach (var rowElement in sheetData.Elements(ns + "row"))
            {
                var row = new Dictionary<int, string>();
                foreach (var cell in rowElement.Elements(ns + "c"))
                {
                    string cellRef = (string)cell.Attribute("r") ?? string.Empty;
                    int columnIndex = GetColumnIndex(cellRef);
                    row[columnIndex] = ReadCellValue(cell, ns, sharedStrings);
                }
                rawRows.Add(row);
            }

            if (rawRows.Count < 2)
                return new ExcelSheet(new List<ExcelRow>());

            var headerRow = rawRows[1];
            int maxColumn = headerRow.Keys.Count == 0 ? -1 : headerRow.Keys.Max();
            var headers = new string[maxColumn + 1];
            foreach (var pair in headerRow)
                headers[pair.Key] = pair.Value?.Trim();

            var rows = new List<ExcelRow>();
            for (int i = 2; i < rawRows.Count; i++)
            {
                var rawRow = rawRows[i];
                var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool hasValue = false;

                foreach (var pair in rawRow)
                {
                    if (pair.Key < 0 || pair.Key >= headers.Length)
                        continue;

                    string header = headers[pair.Key];
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    string value = pair.Value?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(value))
                        hasValue = true;

                    data[header] = value;
                }

                if (hasValue)
                    rows.Add(new ExcelRow(data));
            }

            return new ExcelSheet(rows);
        }

        static string ReadCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");
            if (type == "inlineStr")
                return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

            string rawValue = cell.Element(ns + "v")?.Value ?? string.Empty;
            if (type == "s" && int.TryParse(rawValue, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                return sharedStrings[sharedIndex];

            return rawValue;
        }

        static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
                return -1;

            int index = 0;
            bool hasLetter = false;
            foreach (char c in cellReference)
            {
                if (!char.IsLetter(c))
                    break;

                hasLetter = true;
                index = index * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            }

            return hasLetter ? index - 1 : -1;
        }
    }

    sealed class ExcelRow
    {
        readonly Dictionary<string, string> data;

        public ExcelRow(Dictionary<string, string> data)
        {
            this.data = data;
        }

        public string GetRequired(string key)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Missing required column: {key}");
            return value.Trim();
        }

        public string GetOrDefault(string key, string fallback)
        {
            return data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : fallback;
        }

        public int GetInt(string key)
        {
            return ParseInt(GetRequired(key));
        }

        public float GetFloat(string key)
        {
            return ParseFloat(GetRequired(key));
        }
    }
}

