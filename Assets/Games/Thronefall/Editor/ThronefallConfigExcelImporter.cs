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

public static class ThronefallConfigExcelImporter
{
    const string OutputDirectory = "Assets/Games/Thronefall/Resources/ThronefallConfigs";

    [MenuItem("Tools/Thronefall/Import Config Excel...")]
    public static void ImportFromDialog()
    {
        string excelPath = EditorUtility.OpenFilePanel("Select Thronefall Config Excel", Application.dataPath, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
            return;

        try
        {
            Import(excelPath);
            EditorUtility.DisplayDialog("Thronefall Config Import", "Excel was converted to JSON successfully.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Thronefall Config Import Failed", ex.Message, "OK");
        }
    }

    public static void Import(string excelPath)
    {
        if (!File.Exists(excelPath))
            throw new FileNotFoundException("Excel file was not found.", excelPath);

        Directory.CreateDirectory(OutputDirectory);

        var workbook = ExcelWorkbook.Load(excelPath);

        WriteJson("BuildingTable.json", ImportBuildingTable(workbook.RequireSheet("building")));
        WriteJson("MonsterTable.json", ImportMonsterTable(workbook.RequireSheet("monster")));
        WriteJson("WaveTable.json", ImportWaveTable(workbook.RequireSheet("wave")));
        WriteJson("HeroTable.json", ImportHeroTable(workbook.RequireSheet("hero")));
        WriteJson("AllyUnitTable.json", ImportAllyUnitTable(workbook.RequireSheet("allyUnit")));

        AssetDatabase.Refresh();
        Debug.Log($"Imported Thronefall config excel: {excelPath}");
    }

    // ─── Building Table ─────────────────────────────────────────────

    static TFBuildingTable ImportBuildingTable(ExcelSheet sheet)
    {
        var buildings = new List<TFBuildingConfig>();

        foreach (var row in sheet.Rows)
        {
            var config = new TFBuildingConfig
            {
                buildingId = row.GetInt("buildingId"),
                buildingName = row.GetOrDefault("buildingName", ""),
                description = row.GetOrDefault("description", ""),
                buildingType = row.GetOrDefault("buildingType", ""),
                coinCost = row.GetIntOrDefault("coinCost"),
                maxHP = row.GetInt("maxHP"),
                atk = row.GetIntOrDefault("atk"),
                def = row.GetIntOrDefault("def"),
                attackRange = row.GetFloatOrDefault("attackRange"),
                attackInterval = row.GetFloatOrDefault("attackInterval"),
                arrowSpeed = row.GetFloatOrDefault("arrowSpeed"),
                arcHeight = row.GetFloatOrDefault("arcHeight"),
                aoeRadius = row.GetFloatOrDefault("aoeRadius"),
                dailyYield = row.GetIntOrDefault("dailyYield"),
                recruitCost = row.GetIntOrDefault("recruitCost"),
                maxRecruits = row.GetIntOrDefault("maxRecruits"),
                branchIcon = row.GetOrDefault("branchIcon", ""),
                allyUnitType = row.GetOrDefault("allyUnitType", "")
            };

            string upgradeStr = row.GetOrDefault("upgradeIds", "");
            if (!string.IsNullOrWhiteSpace(upgradeStr))
            {
                var parts = upgradeStr.Split(',');
                var ids = new List<int>();
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                        ids.Add(id);
                }
                config.upgradeIds = ids.ToArray();
            }
            else
            {
                config.upgradeIds = new int[0];
            }

            buildings.Add(config);
        }

        return new TFBuildingTable { buildings = buildings.ToArray() };
    }

    // ─── Ally Unit Table ────────────────────────────────────────────

    static TFAllyUnitTable ImportAllyUnitTable(ExcelSheet sheet)
    {
        var units = new List<TFAllyUnitConfig>();

        foreach (var row in sheet.Rows)
        {
            units.Add(new TFAllyUnitConfig
            {
                unitType = row.GetRequired("unitType"),
                unitName = row.GetOrDefault("unitName", ""),
                maxHP = row.GetInt("maxHP"),
                atk = row.GetInt("atk"),
                def = row.GetIntOrDefault("def"),
                moveSpeed = row.GetFloat("moveSpeed"),
                attackRange = row.GetFloat("attackRange"),
                attackInterval = row.GetFloat("attackInterval"),
                respawnTime = row.GetFloatOrDefault("respawnTime", 15f),
                arrowSpeed = row.GetFloatOrDefault("arrowSpeed"),
                arcHeight = row.GetFloatOrDefault("arcHeight"),
                kiteDistance = row.GetFloatOrDefault("kiteDistance"),
                chargeSpeed = row.GetFloatOrDefault("chargeSpeed"),
                chargeDuration = row.GetFloatOrDefault("chargeDuration"),
                chargeMultiplier = row.GetFloatOrDefault("chargeMultiplier"),
                chargeCooldown = row.GetFloatOrDefault("chargeCooldown")
            });
        }

        return new TFAllyUnitTable { units = units.ToArray() };
    }

    // ─── Monster Table ────────────────────────────────────────────────

    static TFMonsterTable ImportMonsterTable(ExcelSheet sheet)
    {
        var monsters = new List<TFMonsterConfig>();

        foreach (var row in sheet.Rows)
        {
            monsters.Add(new TFMonsterConfig
            {
                monsterId = row.GetInt("monsterId"),
                monsterName = row.GetOrDefault("monsterName", ""),
                maxHP = row.GetInt("maxHP"),
                atk = row.GetInt("atk"),
                def = row.GetIntOrDefault("def"),
                moveSpeed = row.GetFloat("moveSpeed"),
                attackRange = row.GetFloat("attackRange"),
                attackInterval = row.GetFloat("attackInterval"),
                iconLabel = row.GetOrDefault("iconLabel", "?")
            });
        }

        return new TFMonsterTable { monsters = monsters.ToArray() };
    }

    // ─── Wave Table ───────────────────────────────────────────────────

    struct SpawnRowData
    {
        public int dayNumber;
        public float spawnX, spawnY, spawnZ;
        public int monsterId;
        public int count;
        public float spawnInterval;
    }

    static TFWaveTable ImportWaveTable(ExcelSheet sheet)
    {
        var table = new TFWaveTable();
        var spawnRows = new List<SpawnRowData>();

        foreach (var row in sheet.Rows)
        {
            string rowType = row.GetRequired("rowType").Trim().ToLowerInvariant();

            if (rowType == "setting")
            {
                string key = row.GetRequired("key").Trim().ToLowerInvariant();
                string value = row.GetRequired("value");
                switch (key)
                {
                    case "dailybasecoins": table.dailyBaseCoins = ParseInt(value); break;
                    case "startingcoins": table.startingCoins = ParseInt(value); break;
                    default: throw new InvalidDataException($"Unknown wave setting key: {key}");
                }
            }
            else if (rowType == "spawn")
            {
                spawnRows.Add(new SpawnRowData
                {
                    dayNumber = row.GetInt("dayNumber"),
                    spawnX = row.GetFloat("spawnX"),
                    spawnY = row.GetFloatOrDefault("spawnY"),
                    spawnZ = row.GetFloat("spawnZ"),
                    monsterId = row.GetInt("monsterId"),
                    count = row.GetInt("count"),
                    spawnInterval = row.GetFloat("spawnInterval")
                });
            }
            else
            {
                throw new InvalidDataException($"Unknown wave rowType: {rowType}");
            }
        }

        // Group by dayNumber → by position → actions
        var wavesByDay = new SortedDictionary<int, List<SpawnRowData>>();
        foreach (var sr in spawnRows)
        {
            if (!wavesByDay.TryGetValue(sr.dayNumber, out var list))
            {
                list = new List<SpawnRowData>();
                wavesByDay[sr.dayNumber] = list;
            }
            list.Add(sr);
        }

        var waves = new List<TFWaveConfig>();
        foreach (var kvp in wavesByDay)
        {
            var spawnPointsMap = new Dictionary<string, (TFSerializedVector3 pos, List<TFSpawnAction> actions)>();

            foreach (var sr in kvp.Value)
            {
                string posKey = $"{sr.spawnX:F2},{sr.spawnY:F2},{sr.spawnZ:F2}";
                if (!spawnPointsMap.TryGetValue(posKey, out var entry))
                {
                    entry = (new TFSerializedVector3 { x = sr.spawnX, y = sr.spawnY, z = sr.spawnZ },
                             new List<TFSpawnAction>());
                    spawnPointsMap[posKey] = entry;
                }
                entry.actions.Add(new TFSpawnAction
                {
                    monsterId = sr.monsterId,
                    count = sr.count,
                    spawnInterval = sr.spawnInterval
                });
            }

            var spawnPoints = new List<TFSpawnPointConfig>();
            foreach (var sp in spawnPointsMap.Values)
            {
                spawnPoints.Add(new TFSpawnPointConfig
                {
                    position = sp.pos,
                    actions = sp.actions.ToArray()
                });
            }

            waves.Add(new TFWaveConfig
            {
                dayNumber = kvp.Key,
                spawnPoints = spawnPoints.ToArray()
            });
        }

        table.waves = waves.ToArray();
        return table;
    }

    // ─── Hero Table ───────────────────────────────────────────────────

    static TFHeroTable ImportHeroTable(ExcelSheet sheet)
    {
        var hero = new TFHeroConfig();
        var weaponsMap = new Dictionary<string, TFWeaponConfig>(StringComparer.OrdinalIgnoreCase);
        var skillsMap = new Dictionary<string, TFSkillConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            string rowType = row.GetRequired("rowType").Trim().ToLowerInvariant();

            if (rowType == "hero")
            {
                string key = row.GetRequired("key").Trim().ToLowerInvariant();
                float value = row.GetFloat("value");
                switch (key)
                {
                    case "maxhp": hero.maxHP = (int)value; break;
                    case "movespeed": hero.moveSpeed = value; break;
                    case "revivetime": hero.reviveTime = value; break;
                    default: throw new InvalidDataException($"Unknown hero setting key: {key}");
                }
            }
            else if (rowType == "weapon")
            {
                string weaponId = row.GetRequired("weaponId");
                weaponsMap[weaponId] = new TFWeaponConfig
                {
                    weaponId = weaponId,
                    weaponName = row.GetOrDefault("weaponName", weaponId),
                    atk = row.GetInt("atk"),
                    def = row.GetInt("def"),
                    attackRange = row.GetFloat("attackRange"),
                    attackInterval = row.GetFloat("attackInterval"),
                    arrowSpeed = row.GetFloatOrDefault("arrowSpeed"),
                    arcHeight = row.GetFloatOrDefault("arcHeight")
                };
            }
            else if (rowType == "skill")
            {
                string weaponId = row.GetRequired("weaponId");
                skillsMap[weaponId] = new TFSkillConfig
                {
                    skillId = row.GetRequired("skillId"),
                    cooldown = row.GetFloat("cooldown"),
                    damageMultiplier = row.GetFloat("damageMultiplier"),
                    accelTime = row.GetFloatOrDefault("accelTime"),
                    decelTime = row.GetFloatOrDefault("decelTime"),
                    maxSpeed = row.GetFloatOrDefault("maxSpeed"),
                    arrowCount = row.GetIntOrDefault("arrowCount"),
                    spreadAngle = row.GetFloatOrDefault("spreadAngle"),
                    arrowSpeed = row.GetFloatOrDefault("arrowSpeed"),
                    arcHeight = row.GetFloatOrDefault("arcHeight")
                };
            }
            else
            {
                throw new InvalidDataException($"Unknown hero rowType: {rowType}");
            }
        }

        // Link skills to weapons
        foreach (var kvp in skillsMap)
        {
            if (weaponsMap.TryGetValue(kvp.Key, out var weapon))
                weapon.skill = kvp.Value;
        }

        hero.weapons = weaponsMap.Values.ToArray();
        return new TFHeroTable { hero = hero };
    }

    // ─── Helpers ──────────────────────────────────────────────────────

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

    // ─── Excel XLSX Parser (same approach as RaftConfigExcelImporter) ─

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
            {
                string available = string.Join(", ", sheets.Keys);
                throw new InvalidDataException($"Missing sheet: '{sheetName}'. Available sheets: {available}");
            }
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

            // Row 0 = Chinese notes (skip), Row 1 = headers, Row 2+ = data
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

        public int GetIntOrDefault(string key, int fallback = 0)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return fallback;
            return ParseInt(value.Trim());
        }

        public float GetFloat(string key)
        {
            return ParseFloat(GetRequired(key));
        }

        public float GetFloatOrDefault(string key, float fallback = 0f)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return fallback;
            return ParseFloat(value.Trim());
        }
    }
}
