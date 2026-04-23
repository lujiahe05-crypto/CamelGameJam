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

public static class PortiaWorkbookConfigImporter
{
    const string DefaultWorkbookPath = "Assets/Excel/Portia/PortiaConfig.xlsx";
    const string OutputDirectory = "Assets/Games/GameJam/Resources/PortiaConfigs";

    [MenuItem("Tools/Portia/Rebuild Config Json From Workbook")]
    public static void ImportDefaultWorkbook()
    {
        string excelPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultWorkbookPath);
        Import(excelPath);
        EditorUtility.DisplayDialog("Portia Config", "Workbook import completed.", "OK");
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
        var mapbuildingSheet = workbook.RequireSheet("mapbuilding");
        var mapbuildingXyzSheet = workbook.RequireSheet("mapbuildingXYZ");

        var itemRows = BuildItemRows(itemSheet);
        var itemLookup = itemRows.ToDictionary(row => row.ItemTypeId);
        var machineRows = BuildMachineRows(machineSheet);
        var machineLookup = machineRows.ToDictionary(row => row.BuildingId);
        var recipeRows = BuildRecipeRows(synthesisSheet);
        var resourceRows = BuildResourceRows(mapbuildingSheet, itemLookup);
        var resourcePositions = BuildResourcePositionRows(mapbuildingXyzSheet);

        WriteJson("ItemTable.json", BuildItemTable(itemRows));
        WriteJson("BuildingTable.json", BuildBuildingTable(machineRows));
        WriteJson("MachineTable.json", BuildMachineTable(machineRows, machineLookup, recipeRows, itemLookup));
        WriteJson("SettingsTable.json", BuildSettingsTable(resourceRows, resourcePositions, itemLookup, machineLookup));

        AssetDatabase.Refresh();
        Debug.Log($"Imported workbook config from {excelPath}");
    }

    static List<ItemRow> BuildItemRows(ExcelSheet sheet)
    {
        var rows = new List<ItemRow>();
        foreach (var row in sheet.Rows)
        {
            rows.Add(new ItemRow
            {
                ItemTypeId = row.GetInt("itemTypeId"),
                DisplayName = row.GetRequired("displayName"),
                RawType = row.GetInt("Type"),
                FoodValue = row.GetIntOrDefault("FoodValue", 0),
                Energy = row.GetIntOrDefault("Energy", 0),
                IconPath = row.GetOrDefault("iconPath", string.Empty),
                PrefabPath = row.GetOrDefault("prefabPath", string.Empty)
            });
        }

        return rows;
    }

    static List<MachineRow> BuildMachineRows(ExcelSheet sheet)
    {
        var rows = new List<MachineRow>();
        foreach (var row in sheet.Rows)
        {
            rows.Add(new MachineRow
            {
                BuildingId = row.GetInt("buildingId"),
                Type = row.GetRequired("Type"),
                FuelItemTypeId = row.GetIntOrDefault("Fuel_Id", 0),
                FuelMax = row.GetIntOrDefault("Fuel_Max", 0),
                FuelMinutes = row.GetFloatOrDefault("Minutes", 0f),
                OutputItemTypeId = row.GetIntOrDefault("item", 0),
                PrefabPath = row.GetOrDefault("prefabPath", row.GetOrDefault("cityLevel_Open", string.Empty))
            });
        }

        return rows;
    }

    static List<RecipeRow> BuildRecipeRows(ExcelSheet sheet)
    {
        var rows = new List<RecipeRow>();
        foreach (var row in sheet.Rows)
        {
            rows.Add(new RecipeRow
            {
                RecipeId = row.GetRequired("recipeId"),
                MachineBuildingId = row.GetInt("Machines"),
                OutputItemTypeId = row.GetInt("outputItemTypeId"),
                OutputAmount = row.GetIntOrDefault("outputAmount", 1),
                CraftTime = row.GetFloatOrDefault("time", 0f),
                InputItemTypeIds = ParseItemIdList(row.GetRequired("inputItemTypeId")),
                InputAmounts = ParseNumberList(row.GetRequired("inputAmount"))
            });
        }

        return rows;
    }

    static List<ResourceRow> BuildResourceRows(ExcelSheet sheet, Dictionary<int, ItemRow> itemLookup)
    {
        var rows = new List<ResourceRow>();
        foreach (var row in sheet.Rows)
        {
            int resourceId = row.GetInt("id");
            var dropIds = ParseItemIdList(row.GetOrDefault("drop", string.Empty));
            var weights = ParseNumberList(row.GetOrDefault("dropweight", string.Empty));

            var drops = new List<PortiaResourceDropConfig>();
            for (int i = 0; i < dropIds.Count; i++)
            {
                drops.Add(new PortiaResourceDropConfig
                {
                    itemId = ResolveItemName(itemLookup, dropIds[i]),
                    amount = 1,
                    weight = i < weights.Count ? weights[i] : 1f
                });
            }

            rows.Add(new ResourceRow
            {
                ResourceId = resourceId,
                Label = row.GetRequired("name"),
                AttackCount = row.GetIntOrDefault("attacknum", 1),
                TotalAmount = row.GetIntOrDefault("num", 0),
                Drops = drops,
                PrefabPath = row.GetOrDefault("prefabPath", string.Empty)
            });
        }

        return rows;
    }

    static List<ResourcePositionRow> BuildResourcePositionRows(ExcelSheet sheet)
    {
        var rows = new List<ResourcePositionRow>();
        foreach (var row in sheet.Rows)
        {
            string rawId = row.GetOrDefault("mapbuildingid", string.Empty);
            if (string.IsNullOrWhiteSpace(rawId) || !int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resourceId))
                continue;

            if (!TryParseLooseFloat(row.GetOrDefault("X", string.Empty), out var x) ||
                !TryParseLooseFloat(row.GetOrDefault("Y", string.Empty), out var y) ||
                !TryParseLooseFloat(row.GetOrDefault("Z", string.Empty), out var z))
            {
                Debug.LogWarning($"Skipped invalid mapbuildingXYZ row for resourceId={resourceId}.");
                continue;
            }

            rows.Add(new ResourcePositionRow
            {
                ResourceId = resourceId,
                Position = new PortiaVector3Data { x = x, y = y, z = z }
            });
        }

        return rows;
    }

    static PortiaItemTable BuildItemTable(List<ItemRow> itemRows)
    {
        return new PortiaItemTable
        {
            items = itemRows
                .Select(row => new PortiaItemConfig
                {
                    itemId = row.DisplayName,
                    displayName = row.DisplayName,
                    description = BuildItemDescription(row),
                    itemType = MapItemType(row.RawType),
                    rarity = "Common",
                    maxStack = GetDefaultMaxStack(row.RawType),
                    sellPrice = 0,
                    iconColor = GetColorByItemTypeId(row.ItemTypeId),
                    iconPath = row.IconPath,
                    prefabPath = row.PrefabPath
                })
                .ToArray()
        };
    }

    static PortiaBuildingTable BuildBuildingTable(List<MachineRow> machineRows)
    {
        return new PortiaBuildingTable
        {
            buildings = machineRows
                .Select(row =>
                {
                    var size = GetBuildingSize(row.BuildingId);
                    return new PortiaBuildingConfig
                    {
                        itemId = row.Type,
                        gridW = size.gridW,
                        gridH = size.gridH,
                        height = size.height,
                        prefabPath = row.PrefabPath
                    };
                })
                .ToArray()
        };
    }

    static PortiaMachineTable BuildMachineTable(
        List<MachineRow> machineRows,
        Dictionary<int, MachineRow> machineLookup,
        List<RecipeRow> recipeRows,
        Dictionary<int, ItemRow> itemLookup)
    {
        var machines = machineRows
            .Select(row => new PortiaMachineConfig
            {
                machineId = row.Type,
                displayName = row.Type,
                hasFuelSystem = row.FuelItemTypeId > 0,
                fuelItemId = ResolveItemName(itemLookup, row.FuelItemTypeId),
                fuelPerWood = row.FuelMinutes > 0f ? row.FuelMinutes * 60f : 0f,
                maxFuelUnits = row.FuelMax > 0 ? row.FuelMax : 10,
                recipes = Array.Empty<PortiaRecipeConfig>()
            })
            .ToArray();

        var recipesByMachine = machines.ToDictionary(machine => machine.machineId, machine => new List<PortiaRecipeConfig>());

        foreach (var recipeRow in recipeRows)
        {
            if (!machineLookup.TryGetValue(recipeRow.MachineBuildingId, out var machineRow))
                continue;

            var inputs = new List<PortiaRecipeInputConfig>();
            for (int i = 0; i < recipeRow.InputItemTypeIds.Count; i++)
            {
                inputs.Add(new PortiaRecipeInputConfig
                {
                    itemId = ResolveItemName(itemLookup, recipeRow.InputItemTypeIds[i]),
                    amount = i < recipeRow.InputAmounts.Count ? Mathf.Max(1, Mathf.RoundToInt(recipeRow.InputAmounts[i])) : 1
                });
            }

            recipesByMachine[machineRow.Type].Add(new PortiaRecipeConfig
            {
                recipeId = recipeRow.RecipeId,
                outputItemId = ResolveItemName(itemLookup, recipeRow.OutputItemTypeId),
                outputAmount = Mathf.Max(1, recipeRow.OutputAmount),
                craftTime = recipeRow.CraftTime,
                requiresFuel = machineRow.FuelItemTypeId > 0,
                inputs = inputs.ToArray()
            });
        }

        foreach (var machine in machines)
            machine.recipes = recipesByMachine[machine.machineId].ToArray();

        return new PortiaMachineTable { machines = machines };
    }

    static PortiaSettingsTable BuildSettingsTable(
        List<ResourceRow> resourceRows,
        List<ResourcePositionRow> resourcePositions,
        Dictionary<int, ItemRow> itemLookup,
        Dictionary<int, MachineRow> machineLookup)
    {
        var settings = new PortiaSettingsTable
        {
            initialInventory = new[]
            {
                new PortiaInventoryGrantConfig { itemId = ResolveItemName(itemLookup, 101), amount = 1 },
                new PortiaInventoryGrantConfig { itemId = ResolveItemName(itemLookup, 102), amount = 1 },
                new PortiaInventoryGrantConfig { itemId = ResolveItemName(itemLookup, 201), amount = 20 },
                new PortiaInventoryGrantConfig { itemId = ResolveItemName(itemLookup, 202), amount = 10 },
                new PortiaInventoryGrantConfig { itemId = ResolveItemName(itemLookup, 401), amount = 1 }
            },
            placedMachines = machineLookup.TryGetValue(3, out var assemblyRow)
                ? new[]
                {
                    new PortiaPlacedMachineConfig
                    {
                        machineId = assemblyRow.Type,
                        position = new PortiaVector3Data { x = -2f, y = 0f, z = -5f }
                    }
                }
                : Array.Empty<PortiaPlacedMachineConfig>()
        };

        var positionsByResourceId = resourcePositions
            .GroupBy(entry => entry.ResourceId)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Position).ToList());

        var nodes = new List<PortiaResourceNodeConfig>();
        foreach (var row in resourceRows)
        {
            var preset = GetResourcePreset(row.ResourceId);
            var positions = positionsByResourceId.TryGetValue(row.ResourceId, out var configuredPositions) && configuredPositions.Count > 0
                ? configuredPositions
                : preset.FallbackPositions
                    .Select(position => new PortiaVector3Data { x = position.x, y = position.y, z = position.z })
                    .ToList();

            foreach (var position in positions)
            {
                nodes.Add(new PortiaResourceNodeConfig
                {
                    label = row.Label,
                    itemId = row.Drops.Count > 0 ? row.Drops[0].itemId : null,
                    amount = Mathf.Max(1, row.AttackCount),
                    num = Mathf.Max(0, row.TotalAmount),
                    shape = preset.Shape,
                    scale = new PortiaVector3Data
                    {
                        x = string.IsNullOrWhiteSpace(row.PrefabPath) ? preset.Scale.x : 1f,
                        y = string.IsNullOrWhiteSpace(row.PrefabPath) ? preset.Scale.y : 1f,
                        z = string.IsNullOrWhiteSpace(row.PrefabPath) ? preset.Scale.z : 1f
                    },
                    position = new PortiaVector3Data { x = position.x, y = position.y, z = position.z },
                    color = preset.Color,
                    drops = row.Drops.ToArray(),
                    prefabPath = row.PrefabPath
                });
            }
        }

        settings.resourceNodes = nodes.ToArray();
        return settings;
    }

    static ResourcePreset GetResourcePreset(int resourceId)
    {
        switch (resourceId)
        {
            case 1:
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
            case 2:
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
            case 3:
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
            case 4:
                return new ResourcePreset(
                    "Cylinder",
                    new Vector3(0.18f, 0.25f, 0.18f),
                    ColorOf(0.56f, 0.36f, 0.18f),
                    new[]
                    {
                        new Vector3(4f, 0.15f, 6f),
                        new Vector3(-5f, 0.15f, 4f)
                    });
            case 5:
                return new ResourcePreset(
                    "Sphere",
                    new Vector3(0.4f, 0.25f, 0.35f),
                    ColorOf(0.62f, 0.62f, 0.6f),
                    new[]
                    {
                        new Vector3(-6f, 0.15f, 12f),
                        new Vector3(11f, 0.15f, -2f)
                    });
            case 6:
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
                return new ResourcePreset(nameof(PrimitiveType.Cube), Vector3.one, ColorOf(1f, 1f, 1f), new[] { Vector3.zero });
        }
    }

    static string BuildItemDescription(ItemRow row)
    {
        if (row.FoodValue > 0 || row.Energy > 0)
            return "Consumable item.";

        switch (row.RawType)
        {
            case 1: return "Tool item.";
            case 2: return "Material item.";
            case 3: return "Consumable item.";
            case 4: return "Building item.";
            default: return row.DisplayName;
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

    static (int gridW, int gridH, float height) GetBuildingSize(int buildingId)
    {
        switch (buildingId)
        {
            case 1: return (2, 2, 1.8f);
            case 2: return (3, 2, 1.4f);
            case 3: return (3, 2, 1.5f);
            default: return (2, 2, 1f);
        }
    }

    static PortiaColorData GetColorByItemTypeId(int itemTypeId)
    {
        switch (itemTypeId)
        {
            case 101: return ColorOf(0.55f, 0.55f, 0.6f);
            case 102: return ColorOf(0.5f, 0.5f, 0.52f);
            case 201: return ColorOf(0.5f, 0.33f, 0.15f);
            case 202: return ColorOf(0.6f, 0.6f, 0.58f);
            case 203: return ColorOf(0.72f, 0.45f, 0.2f);
            case 204: return ColorOf(0.2f, 0.8f, 0.95f);
            case 205: return ColorOf(0.65f, 0.46f, 0.26f);
            case 301: return ColorOf(0.45f, 0.72f, 0.95f);
            case 302: return ColorOf(0.78f, 0.22f, 0.2f);
            case 401: return ColorOf(0.45f, 0.35f, 0.2f);
            case 402: return ColorOf(0.6f, 0.25f, 0.15f);
            case 403: return ColorOf(0.45f, 0.45f, 0.5f);
            case 404: return ColorOf(0.75f, 0.4f, 0.22f);
            case 501: return ColorOf(0.68f, 0.68f, 0.65f);
            case 502: return ColorOf(0.6f, 0.45f, 0.25f);
            case 503: return ColorOf(0.85f, 0.55f, 0.28f);
            case 505: return ColorOf(0.82f, 0.62f, 0.32f);
            case 506: return ColorOf(0.72f, 0.72f, 0.75f);
            case 507: return ColorOf(0.78f, 0.58f, 0.3f);
            default: return ColorOf(1f, 1f, 1f);
        }
    }

    static PortiaColorData ColorOf(float r, float g, float b)
    {
        return new PortiaColorData { r = r, g = g, b = b, a = 1f };
    }

    static string ResolveItemName(Dictionary<int, ItemRow> itemLookup, int itemTypeId)
    {
        if (itemTypeId <= 0)
            return string.Empty;

        if (itemLookup.TryGetValue(itemTypeId, out var row))
            return row.DisplayName;

        throw new InvalidDataException($"Missing item definition for itemTypeId={itemTypeId}.");
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

    static bool TryParseLooseFloat(string raw, out float value)
    {
        value = 0f;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string normalized = raw.Trim().TrimEnd('f', 'F', ',', '，');
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static void WriteJson(string fileName, object payload)
    {
        string outputPath = Path.Combine(OutputDirectory, fileName);
        string json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));
    }

    sealed class ResourcePreset
    {
        public readonly string Shape;
        public readonly Vector3 Scale;
        public readonly PortiaColorData Color;
        public readonly Vector3[] FallbackPositions;

        public ResourcePreset(string shape, Vector3 scale, PortiaColorData color, Vector3[] fallbackPositions)
        {
            Shape = shape;
            Scale = scale;
            Color = color;
            FallbackPositions = fallbackPositions;
        }
    }

    sealed class ItemRow
    {
        public int ItemTypeId;
        public string DisplayName;
        public int RawType;
        public int FoodValue;
        public int Energy;
        public string IconPath;
        public string PrefabPath;
    }

    sealed class MachineRow
    {
        public int BuildingId;
        public string Type;
        public int FuelItemTypeId;
        public int FuelMax;
        public float FuelMinutes;
        public int OutputItemTypeId;
        public string PrefabPath;
    }

    sealed class RecipeRow
    {
        public string RecipeId;
        public int MachineBuildingId;
        public int OutputItemTypeId;
        public int OutputAmount;
        public float CraftTime;
        public List<int> InputItemTypeIds;
        public List<float> InputAmounts;
    }

    sealed class ResourceRow
    {
        public int ResourceId;
        public string Label;
        public int AttackCount;
        public int TotalAmount;
        public List<PortiaResourceDropConfig> Drops;
        public string PrefabPath;
    }

    sealed class ResourcePositionRow
    {
        public int ResourceId;
        public PortiaVector3Data Position;
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
                    element => (string)element.Attribute("Id"),
                    element => NormalizeZipPath("xl/" + (string)element.Attribute("Target")),
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
            if (type == "s" &&
                int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex) &&
                sharedIndex >= 0 &&
                sharedIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedIndex];
            }

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
