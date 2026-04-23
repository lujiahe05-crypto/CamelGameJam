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

public static class PortiaConfigExcelImporter
{
    const string OutputDirectory = "Assets/Games/GameJam/Resources/PortiaConfigs";

    [MenuItem("Tools/Portia/Import Config Excel...")]
    public static void ImportFromDialog()
    {
        string defaultDir = Path.Combine(Application.dataPath, "Excel", "Portia").Replace('/', '\\');
        string excelPath = EditorUtility.OpenFilePanel("Select Portia Config Excel", defaultDir, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
            return;

        try
        {
            Import(excelPath);
            EditorUtility.DisplayDialog("Portia Config Import", "Excel was converted to JSON successfully.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Portia Config Import Failed", ex.Message, "OK");
        }
    }

    public static void Import(string excelPath)
    {
        if (!File.Exists(excelPath))
            throw new FileNotFoundException("Excel file was not found.", excelPath);

        Directory.CreateDirectory(OutputDirectory);

        var workbook = ExcelWorkbook.Load(excelPath);
        var itemSheet = workbook.RequireSheet("item");
        var machineSheet = workbook.RequireSheet("Machines");
        var synthesisSheet = workbook.RequireSheet("synthesis");
        var mapBuildingSheet = workbook.RequireSheet("mapbuilding");

        var itemLookup = BuildItemLookup(itemSheet);
        var machineLookup = BuildMachineLookup(machineSheet, itemLookup);
        var resourceRows = BuildResourceRows(mapBuildingSheet, itemLookup);

        WriteJson("ItemTable.json", ImportItemTable(itemSheet));
        WriteJson("BuildingTable.json", ImportBuildingTable(machineLookup.Values));
        WriteJson("MachineTable.json", ImportMachineTable(machineLookup, synthesisSheet, itemLookup));
        WriteJson("SettingsTable.json", ImportSettingsTable(resourceRows));

        AssetDatabase.Refresh();
        Debug.Log($"Imported Portia config excel: {excelPath}");
    }

    static Dictionary<int, ItemSheetRow> BuildItemLookup(ExcelSheet itemSheet)
    {
        var items = new Dictionary<int, ItemSheetRow>();
        foreach (var row in itemSheet.Rows)
        {
            int itemId = row.GetInt("itemTypeId");
            items[itemId] = new ItemSheetRow
            {
                itemTypeId = itemId,
                displayName = row.GetRequired("displayName"),
                rawType = row.GetRequired("Type"),
                foodValue = row.GetIntOrDefault("FoodValue", 0),
                energy = row.GetIntOrDefault("Energy", 0)
            };
        }

        return items;
    }

    static Dictionary<int, MachineSheetRow> BuildMachineLookup(ExcelSheet machineSheet, Dictionary<int, ItemSheetRow> itemLookup)
    {
        var machines = new Dictionary<int, MachineSheetRow>();
        foreach (var row in machineSheet.Rows)
        {
            int buildingId = row.GetInt("buildingId");
            int fuelId = row.GetIntOrDefault("Fuel_Id", 0);
            int fuelMax = row.GetIntOrDefault("Fuel_Max", 0);
            float minutesPerFuel = row.GetFloatOrDefault("Minutes", 0f);

            machines[buildingId] = new MachineSheetRow
            {
                buildingId = buildingId,
                machineId = row.GetRequired("Type"),
                fuelItemId = ResolveItemName(itemLookup, fuelId),
                hasFuelSystem = fuelId > 0,
                maxFuelUnits = fuelMax,
                fuelSecondsPerItem = minutesPerFuel > 0f ? minutesPerFuel * 60f : 0f,
                outputItemId = ResolveItemName(itemLookup, row.GetIntOrDefault("item", 0))
            };
        }

        return machines;
    }

    static List<ResourceSheetRow> BuildResourceRows(ExcelSheet sheet, Dictionary<int, ItemSheetRow> itemLookup)
    {
        var rows = new List<ResourceSheetRow>();
        foreach (var row in sheet.Rows)
        {
            var drops = ParseItemIdList(row.GetOrDefault("drop", string.Empty))
                .Select(id => ResolveItemName(itemLookup, id))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
            var weights = ParseNumberList(row.GetOrDefault("dropweight", string.Empty));

            var dropConfigs = new List<PortiaResourceDropConfig>();
            for (int i = 0; i < drops.Count; i++)
            {
                dropConfigs.Add(new PortiaResourceDropConfig
                {
                    itemId = drops[i],
                    amount = 1,
                    weight = i < weights.Count ? weights[i] : 1f
                });
            }

            rows.Add(new ResourceSheetRow
            {
                label = row.GetRequired("id"),
                toolItemId = ResolveItemName(itemLookup, row.GetIntOrDefault("useitem", 0)),
                attackCount = row.GetIntOrDefault("attacknum", 1),
                totalAmount = row.GetIntOrDefault("num", 0),
                respawnSeconds = row.GetFloatOrDefault("time", 0f),
                drops = dropConfigs
            });
        }

        return rows;
    }

    static PortiaItemTable ImportItemTable(ExcelSheet sheet)
    {
        var items = new List<PortiaItemConfig>();
        foreach (var row in sheet.Rows)
        {
            string displayName = row.GetRequired("displayName");
            int rawType = row.GetInt("Type");

            items.Add(new PortiaItemConfig
            {
                itemId = displayName,
                displayName = displayName,
                description = BuildItemDescription(displayName, row.GetIntOrDefault("FoodValue", 0), row.GetIntOrDefault("Energy", 0), rawType),
                itemType = MapItemType(rawType),
                rarity = "Common",
                maxStack = GetDefaultMaxStack(rawType),
                sellPrice = 0,
                iconColor = GetDefaultColor(displayName)
            });
        }

        return new PortiaItemTable
        {
            items = items.ToArray()
        };
    }

    static PortiaBuildingTable ImportBuildingTable(IEnumerable<MachineSheetRow> machines)
    {
        var buildings = new List<PortiaBuildingConfig>();
        foreach (var machine in machines)
        {
            var size = GetBuildingSize(machine.machineId);
            buildings.Add(new PortiaBuildingConfig
            {
                itemId = machine.machineId,
                gridW = size.gridW,
                gridH = size.gridH,
                height = size.height
            });
        }

        return new PortiaBuildingTable
        {
            buildings = buildings.ToArray()
        };
    }

    static PortiaMachineTable ImportMachineTable(
        Dictionary<int, MachineSheetRow> machineLookup,
        ExcelSheet synthesisSheet,
        Dictionary<int, ItemSheetRow> itemLookup)
    {
        var machineTable = new PortiaMachineTable
        {
            machines = machineLookup.Values
                .OrderBy(machine => machine.buildingId)
                .Select(machine => new PortiaMachineConfig
                {
                    machineId = machine.machineId,
                    displayName = machine.machineId,
                    hasFuelSystem = machine.hasFuelSystem,
                    fuelItemId = machine.fuelItemId,
                    fuelPerWood = machine.fuelSecondsPerItem,
                    maxFuelUnits = machine.maxFuelUnits,
                    recipes = Array.Empty<PortiaRecipeConfig>()
                })
                .ToArray()
        };

        var recipesByMachine = machineTable.machines.ToDictionary(machine => machine.machineId, machine => new List<PortiaRecipeConfig>());

        foreach (var row in synthesisSheet.Rows)
        {
            int machineBuildingId = row.GetInt("Machines");
            if (!machineLookup.TryGetValue(machineBuildingId, out var machine))
                continue;

            var inputIds = ParseItemIdList(row.GetRequired("inputItemTypeId"))
                .Select(id => ResolveItemName(itemLookup, id))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
            var inputAmounts = ParseNumberList(row.GetRequired("inputAmount"));

            var inputs = new List<PortiaRecipeInputConfig>();
            for (int i = 0; i < inputIds.Count; i++)
            {
                inputs.Add(new PortiaRecipeInputConfig
                {
                    itemId = inputIds[i],
                    amount = i < inputAmounts.Count ? Mathf.Max(1, Mathf.RoundToInt(inputAmounts[i])) : 1
                });
            }

            recipesByMachine[machine.machineId].Add(new PortiaRecipeConfig
            {
                recipeId = row.GetRequired("recipeId"),
                outputItemId = ResolveItemName(itemLookup, row.GetInt("outputItemTypeId")),
                outputAmount = row.GetIntOrDefault("outputAmount", 1),
                craftTime = row.GetFloatOrDefault("time", 0f),
                requiresFuel = machine.hasFuelSystem,
                inputs = inputs.ToArray()
            });
        }

        foreach (var machine in machineTable.machines)
            machine.recipes = recipesByMachine[machine.machineId].ToArray();

        return machineTable;
    }

    static PortiaSettingsTable ImportSettingsTable(List<ResourceSheetRow> resourceRows)
    {
        var settings = new PortiaSettingsTable
        {
            initialInventory = new[]
            {
                new PortiaInventoryGrantConfig { itemId = "斧头", amount = 1 },
                new PortiaInventoryGrantConfig { itemId = "矿稿", amount = 1 },
                new PortiaInventoryGrantConfig { itemId = "木材", amount = 20 },
                new PortiaInventoryGrantConfig { itemId = "石头", amount = 10 }
            },
            placedMachines = new[]
            {
                new PortiaPlacedMachineConfig
                {
                    machineId = "组装台",
                    position = new PortiaVector3Data { x = -2f, y = 0f, z = -5f }
                }
            }
        };

        var nodes = new List<PortiaResourceNodeConfig>();
        foreach (var row in resourceRows)
        {
            var preset = GetResourcePreset(row.label);
            foreach (var position in preset.positions)
            {
                nodes.Add(new PortiaResourceNodeConfig
                {
                    label = row.label,
                    itemId = row.drops.Count > 0 ? row.drops[0].itemId : null,
                    amount = Mathf.Max(1, row.attackCount),
                    num = Mathf.Max(0, row.totalAmount),
                    shape = preset.shape,
                    scale = new PortiaVector3Data { x = preset.scale.x, y = preset.scale.y, z = preset.scale.z },
                    position = new PortiaVector3Data { x = position.x, y = position.y, z = position.z },
                    color = preset.color,
                    drops = row.drops.ToArray()
                });
            }
        }
        settings.resourceNodes = nodes.ToArray();

        return settings;
    }

    static string BuildItemDescription(string displayName, int foodValue, int energy, int rawType)
    {
        if (foodValue > 0 || energy > 0)
            return $"{displayName} 可回复食物或体力。";

        switch (rawType)
        {
            case 1: return $"{displayName} 是可装备的工具。";
            case 2: return $"{displayName} 是制作与建造材料。";
            case 3: return $"{displayName} 是可消耗道具。";
            case 4: return $"{displayName} 是可放置建筑。";
            default: return displayName;
        }
    }

    static string MapItemType(int rawType)
    {
        switch (rawType)
        {
            case 1: return "Tool";
            case 2: return "Material";
            case 3: return "Consumable";
            case 4: return "Building";
            default: return "Material";
        }
    }

    static int GetDefaultMaxStack(int rawType)
    {
        switch (rawType)
        {
            case 1: return 1;
            case 4: return 99;
            default: return 999;
        }
    }

    static PortiaColorData GetDefaultColor(string itemName)
    {
        switch (itemName)
        {
            case "斧头": return ColorOf(0.55f, 0.55f, 0.6f);
            case "矿稿": return ColorOf(0.5f, 0.5f, 0.52f);
            case "木材": return ColorOf(0.5f, 0.33f, 0.15f);
            case "石头": return ColorOf(0.6f, 0.6f, 0.58f);
            case "铜矿": return ColorOf(0.72f, 0.45f, 0.2f);
            case "能源石": return ColorOf(0.2f, 0.8f, 0.95f);
            case "矿泉水": return ColorOf(0.45f, 0.72f, 0.95f);
            case "蘑菇": return ColorOf(0.78f, 0.22f, 0.2f);
            case "组装台": return ColorOf(0.45f, 0.35f, 0.2f);
            case "熔炉": return ColorOf(0.6f, 0.25f, 0.15f);
            case "切割机": return ColorOf(0.45f, 0.45f, 0.5f);
            case "石砖": return ColorOf(0.68f, 0.68f, 0.65f);
            case "木板": return ColorOf(0.6f, 0.45f, 0.25f);
            case "铜锭": return ColorOf(0.85f, 0.55f, 0.28f);
            case "铜质板材": return ColorOf(0.82f, 0.62f, 0.32f);
            default: return ColorOf(1f, 1f, 1f);
        }
    }

    static PortiaColorData ColorOf(float r, float g, float b)
    {
        return new PortiaColorData { r = r, g = g, b = b, a = 1f };
    }

    static (int gridW, int gridH, float height) GetBuildingSize(string machineId)
    {
        switch (machineId)
        {
            case "组装台": return (3, 2, 1.5f);
            case "熔炉": return (2, 2, 1.8f);
            case "切割机": return (3, 2, 1.4f);
            default: return (2, 2, 1f);
        }
    }

    static ResourcePreset GetResourcePreset(string label)
    {
        switch (label)
        {
            case "树":
                return new ResourcePreset(
                    "Cylinder",
                    new Vector3(0.3f, 0.8f, 0.3f),
                    ColorOf(0.5f, 0.33f, 0.15f),
                    new[]
                    {
                        new Vector3(10f, 0.8f, 3f),
                        new Vector3(-9f, 0.9f, 7f),
                        new Vector3(2f, 0.7f, -18f)
                    });
            case "石头":
                return new ResourcePreset(
                    "Sphere",
                    new Vector3(1.1f, 0.75f, 1f),
                    ColorOf(0.6f, 0.6f, 0.58f),
                    new[]
                    {
                        new Vector3(7f, 0.4f, 10f),
                        new Vector3(-3f, 0.35f, 14f),
                        new Vector3(-14f, 0.45f, -10f)
                    });
            case "矿脉":
                return new ResourcePreset(
                    "Sphere",
                    new Vector3(0.85f, 0.85f, 0.85f),
                    ColorOf(0.65f, 0.46f, 0.26f),
                    new[]
                    {
                        new Vector3(-17f, 0.4f, 0f),
                        new Vector3(14f, 0.35f, 16f),
                        new Vector3(0f, 0.45f, -8f)
                    });
            case "采集物树枝u":
                return new ResourcePreset(
                    "Cylinder",
                    new Vector3(0.18f, 0.25f, 0.18f),
                    ColorOf(0.56f, 0.36f, 0.18f),
                    new[]
                    {
                        new Vector3(4f, 0.15f, 6f),
                        new Vector3(-5f, 0.15f, 4f)
                    });
            case "采集物石块":
                return new ResourcePreset(
                    "Sphere",
                    new Vector3(0.4f, 0.25f, 0.35f),
                    ColorOf(0.62f, 0.62f, 0.6f),
                    new[]
                    {
                        new Vector3(-6f, 0.15f, 12f),
                        new Vector3(11f, 0.15f, -2f)
                    });
            case "采集物蘑菇":
                return new ResourcePreset(
                    "Capsule",
                    new Vector3(0.2f, 0.25f, 0.2f),
                    ColorOf(0.78f, 0.22f, 0.2f),
                    new[]
                    {
                        new Vector3(6f, 0.12f, -12f),
                        new Vector3(-10f, 0.12f, -5f)
                    });
            default:
                return new ResourcePreset(
                    nameof(PrimitiveType.Cube),
                    Vector3.one,
                    ColorOf(1f, 1f, 1f),
                    new[] { Vector3.zero });
        }
    }

    static string ResolveItemName(Dictionary<int, ItemSheetRow> itemLookup, int itemTypeId)
    {
        if (itemTypeId <= 0)
            return null;

        if (itemLookup.TryGetValue(itemTypeId, out var item))
            return item.displayName;

        throw new InvalidDataException($"Missing item definition for itemTypeId={itemTypeId}");
    }

    static List<int> ParseItemIdList(string raw)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string normalized = raw.Trim();
        if (normalized.Contains(";") || normalized.Contains(","))
        {
            foreach (var part in normalized.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                result.Add(ParseInt(part.Trim()));
            return result;
        }

        if (normalized.All(char.IsDigit) && normalized.Length > 3 && normalized.Length % 3 == 0)
        {
            for (int i = 0; i < normalized.Length; i += 3)
                result.Add(ParseInt(normalized.Substring(i, 3)));
            return result;
        }

        result.Add(ParseInt(normalized));
        return result;
    }

    static List<float> ParseNumberList(string raw)
    {
        var result = new List<float>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var part in raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            result.Add(ParseFloat(part.Trim()));

        return result;
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

    sealed class ResourcePreset
    {
        public readonly string shape;
        public readonly Vector3 scale;
        public readonly PortiaColorData color;
        public readonly Vector3[] positions;

        public ResourcePreset(string shape, Vector3 scale, PortiaColorData color, Vector3[] positions)
        {
            this.shape = shape;
            this.scale = scale;
            this.color = color;
            this.positions = positions;
        }
    }

    sealed class ItemSheetRow
    {
        public int itemTypeId;
        public string displayName;
        public string rawType;
        public int foodValue;
        public int energy;
    }

    sealed class MachineSheetRow
    {
        public int buildingId;
        public string machineId;
        public bool hasFuelSystem;
        public string fuelItemId;
        public int maxFuelUnits;
        public float fuelSecondsPerItem;
        public string outputItemId;
    }

    sealed class ResourceSheetRow
    {
        public string label;
        public string toolItemId;
        public int attackCount;
        public int totalAmount;
        public float respawnSeconds;
        public List<PortiaResourceDropConfig> drops;
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

        public int GetIntOrDefault(string key, int fallback)
        {
            return data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? ParseInt(value.Trim())
                : fallback;
        }

        public float GetFloatOrDefault(string key, float fallback)
        {
            return data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? ParseFloat(value.Trim())
                : fallback;
        }
    }
}
