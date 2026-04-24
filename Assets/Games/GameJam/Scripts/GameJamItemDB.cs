using UnityEngine;
using System.Collections.Generic;

public enum GameJamItemType { Material, Tool, Consumable, Equipment, Building }

public enum GameJamRarity { Common, Uncommon, Rare, Epic }

public class GameJamItemDef
{
    public string id;
    public string name;
    public string description;
    public GameJamItemType type;
    public GameJamRarity rarity;
    public int maxStack;
    public int sellPrice;
    public Color iconColor;
    public string iconPath;
    public string iconName;
    public string prefabName;

    public GameJamItemDef(string id, string name, string desc, GameJamItemType type,
        GameJamRarity rarity, int maxStack, int sellPrice, Color iconColor,
        string iconPath = null, string iconName = null, string prefabName = null)
    {
        this.id = id;
        this.name = name;
        this.description = desc;
        this.type = type;
        this.rarity = rarity;
        this.maxStack = maxStack;
        this.sellPrice = sellPrice;
        this.iconColor = iconColor;
        this.iconPath = iconPath;
        this.iconName = iconName;
        this.prefabName = prefabName;
    }
}

public static class GameJamItemDB
{
    static Dictionary<string, GameJamItemDef> items;

    static void Init()
    {
        if (items != null) return;
        items = new Dictionary<string, GameJamItemDef>();
        Reg(new GameJamItemDef("石块", "石块", "常见的石材，可用于建造基础设施。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 2,
            new Color(0.6f, 0.6f, 0.58f)));

        Reg(new GameJamItemDef("木材", "木材", "坚韧的木头，建造和制作的基础材料。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 3,
            new Color(0.5f, 0.33f, 0.15f)));

        Reg(new GameJamItemDef("铁矿", "铁矿", "含铁量较高的矿石，冶炼后可制作工具和武器。",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 5,
            new Color(0.4f, 0.42f, 0.5f)));

        Reg(new GameJamItemDef("铜矿", "铜矿", "柔软的金属矿石，适合制作装饰品和基础工具。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 4,
            new Color(0.72f, 0.45f, 0.2f)));

        Reg(new GameJamItemDef("草药", "草药", "具有治愈功效的植物，可制作药水。",
            GameJamItemType.Consumable, GameJamRarity.Common, 999, 3,
            new Color(0.2f, 0.65f, 0.3f)));

        Reg(new GameJamItemDef("石镐", "石镐", "简陋但实用的采矿工具，提升矿石采集效率。",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 15,
            new Color(0.55f, 0.55f, 0.5f)));

        Reg(new GameJamItemDef("铁斧", "铁斧", "锋利的伐木工具，大幅提升木材采集效率。",
            GameJamItemType.Tool, GameJamRarity.Uncommon, 1, 30,
            new Color(0.5f, 0.52f, 0.6f)));

        Reg(new GameJamItemDef("皮甲", "皮甲", "用兽皮制作的轻型护甲，提供基础防护。",
            GameJamItemType.Equipment, GameJamRarity.Common, 1, 25,
            new Color(0.55f, 0.35f, 0.18f)));

        Reg(new GameJamItemDef("红宝石", "红宝石", "稀有的宝石，散发着神秘的红色光芒。",
            GameJamItemType.Material, GameJamRarity.Rare, 999, 50,
            new Color(0.85f, 0.15f, 0.2f)));

        Reg(new GameJamItemDef("龙鳞", "龙鳞", "传说中巨龙身上脱落的鳞片，极其珍贵。",
            GameJamItemType.Material, GameJamRarity.Epic, 999, 200,
            new Color(0.6f, 0.1f, 0.7f)));

        Reg(new GameJamItemDef("工作台", "工作台", "基础制作设施，可用于加工木材和石料。",
            GameJamItemType.Building, GameJamRarity.Common, 99, 20,
            new Color(0.45f, 0.35f, 0.2f)));

        Reg(new GameJamItemDef("民用熔炉", "民用熔炉", "高温冶炼设施，可将矿石冶炼为金属锭。",
            GameJamItemType.Building, GameJamRarity.Uncommon, 99, 35,
            new Color(0.6f, 0.25f, 0.15f)));

        Reg(new GameJamItemDef("切割机", "切割机", "精密的木材加工设备，可将原木切割为木板。",
            GameJamItemType.Building, GameJamRarity.Common, 99, 25,
            new Color(0.45f, 0.45f, 0.5f)));

        Reg(new GameJamItemDef("沙子", "沙子", "细腻的沙粒，高温冶炼可制成玻璃。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 1,
            new Color(0.85f, 0.78f, 0.55f)));

        Reg(new GameJamItemDef("玻璃", "玻璃", "晶莹透明，需要高温冶炼而成。",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 12,
            new Color(0.7f, 0.85f, 0.9f)));

        Reg(new GameJamItemDef("木板", "木板", "切割加工后的木材，平整结实，适用于建造。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 5,
            new Color(0.6f, 0.45f, 0.25f)));

        Reg(new GameJamItemDef("木炭", "木炭", "木材烧制而成的燃料，燃烧温度高且持久。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 4,
            new Color(0.2f, 0.18f, 0.15f)));

        Reg(new GameJamItemDef("储物箱", "储物箱", "用于存放多余物资的木制箱子。",
            GameJamItemType.Building, GameJamRarity.Common, 99, 10,
            new Color(0.5f, 0.38f, 0.2f)));

        Reg(new GameJamItemDef("铜锭", "铜锭", "铜矿冶炼而成的金属锭，用于制作基础工具和建筑。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 8,
            new Color(0.78f, 0.5f, 0.2f)));

        Reg(new GameJamItemDef("铁锭", "铁锭", "铁矿冶炼而成的金属锭，坚固耐用，高级制作材料。",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 12,
            new Color(0.55f, 0.55f, 0.6f)));

        Reg(new GameJamItemDef("青铜锭", "青铜锭", "铜锭与铁锭合金冶炼的高级材料，兼具韧性与硬度。",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 15,
            new Color(0.7f, 0.55f, 0.25f)));

        Reg(new GameJamItemDef("铜镐", "铜镐", "铜制采矿工具，比石镐更高效。",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 20,
            new Color(0.75f, 0.48f, 0.18f)));

        Reg(new GameJamItemDef("铜斧", "铜斧", "铜制伐木工具，比徒手伐木快得多。",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 25,
            new Color(0.73f, 0.46f, 0.16f)));

        Reg(new GameJamItemDef("绷带", "绷带", "用草药制成的简易绷带，可恢复少量生命值。",
            GameJamItemType.Consumable, GameJamRarity.Common, 99, 5,
            new Color(0.9f, 0.88f, 0.8f)));

        Reg(new GameJamItemDef(GameJamCropDB.PlanterItemId, GameJamCropDB.PlanterItemId, "可放置的种植容器。",
            GameJamItemType.Building, GameJamRarity.Common, 99, 5,
            new Color(0.50f, 0.34f, 0.20f)));

        Reg(new GameJamItemDef(GameJamCropDB.RadishSeedItemId, GameJamCropDB.RadishSeedItemId, "用于种植萝卜。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 1,
            new Color(0.72f, 0.58f, 0.20f)));

        Reg(new GameJamItemDef(GameJamCropDB.RadishItemId, GameJamCropDB.RadishItemId, "成熟后可收获的萝卜。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 2,
            new Color(0.84f, 0.28f, 0.30f)));
        ApplyConfigOverrides();
    }

    static void Reg(GameJamItemDef def) => items[def.id] = def;

    public static void Reload() => items = null;

    static void ApplyConfigOverrides()
    {
        var table = PortiaConfigTables.ItemTableData;
        if (table == null || table.items == null) return;
        foreach (var entry in table.items)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
                continue;
            items.TryGetValue(entry.itemId, out var existing);
            GameJamItemType itemType = existing != null ? existing.type : GameJamItemType.Material;
            if (!string.IsNullOrWhiteSpace(entry.itemType))
            {
                if (!PortiaConfigTables.TryParseItemType(entry.itemType, out itemType))
                {
                    Debug.LogWarning($"Invalid GameJam itemType in Portia config: {entry.itemType}");
                    if (existing == null)
                        continue;
                    itemType = existing.type;
                }
            }

            GameJamRarity rarity = existing != null ? existing.rarity : GameJamRarity.Common;
            if (!string.IsNullOrWhiteSpace(entry.rarity))
            {
                if (!PortiaConfigTables.TryParseRarity(entry.rarity, out rarity))
                {
                    Debug.LogWarning($"Invalid GameJam rarity in Portia config: {entry.rarity}");
                    if (existing == null)
                        continue;
                    rarity = existing.rarity;
                }
            }

            string displayName = !string.IsNullOrWhiteSpace(entry.displayName)
                ? entry.displayName
                : existing != null ? existing.name : entry.itemId;
            string description = !string.IsNullOrWhiteSpace(entry.description)
                ? entry.description
                : existing != null ? existing.description : string.Empty;
            int maxStack = entry.maxStack > 0 ? entry.maxStack : existing != null ? existing.maxStack : 1;
            int sellPrice = entry.sellPrice;
            string iconPath = !string.IsNullOrWhiteSpace(entry.iconPath)
                ? entry.iconPath
                : existing != null ? existing.iconPath : null;
            Color iconColor = entry.iconColor != null
                ? entry.iconColor.ToColor()
                : existing != null ? existing.iconColor : Color.white;
            Reg(new GameJamItemDef(
                entry.itemId,
                displayName,
                description,
                itemType,
                rarity,
                maxStack,
                sellPrice,
                iconColor,
                entry.iconPath,
                entry.iconName,
                entry.prefabName));
        }
    }

    public static GameJamItemDef Get(string id)
    {
        Init();
        return items.TryGetValue(id, out var def) ? def : null;
    }

    public static bool Exists(string id)
    {
        Init();
        return items.ContainsKey(id);
    }

    public static bool IsToolForGatherAnim(string itemId, GameJamGatherAnim gatherAnim)
    {
        var def = Get(itemId);
        if (def == null || def.type != GameJamItemType.Tool)
            return false;
        switch (gatherAnim)
        {
            case GameJamGatherAnim.CutTree:
            case GameJamGatherAnim.Saw:
                return MatchesAny(def, "axe", "hatchet", "\u65A7");
            case GameJamGatherAnim.Mine:
            case GameJamGatherAnim.Drill:
                return MatchesAny(def, "pick", "pickaxe", "pick-axe", "\u9550", "\u7A3F", "\u77FF");
            case GameJamGatherAnim.Dig:
                return MatchesAny(def, "shovel", "spade", "hoe", "\u94F2", "\u9504");
            default:
                return false;
        }
    }

    static bool MatchesAny(GameJamItemDef def, params string[] keywords)
    {
        if (def == null || keywords == null || keywords.Length == 0)
            return false;
        string search = string.Concat(
            def.id ?? string.Empty, "|",
            def.name ?? string.Empty, "|",
            def.description ?? string.Empty, "|",
            def.iconName ?? string.Empty, "|",
            def.prefabName ?? string.Empty);
        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
                continue;
            if (search.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    public static Color GetRarityColor(GameJamRarity rarity)
    {
        switch (rarity)
        {
            case GameJamRarity.Common: return new Color(0.6f, 0.6f, 0.6f);
            case GameJamRarity.Uncommon: return new Color(0.3f, 0.8f, 0.3f);
            case GameJamRarity.Rare: return new Color(0.3f, 0.5f, 0.9f);
            case GameJamRarity.Epic: return new Color(0.7f, 0.3f, 0.9f);
            default: return Color.white;
        }
    }

    public static string GetTypeName(GameJamItemType type)
    {
        switch (type)
        {
            case GameJamItemType.Material: return "材料";
            case GameJamItemType.Tool: return "工具";
            case GameJamItemType.Consumable: return "消耗品";
            case GameJamItemType.Equipment: return "装备";
            case GameJamItemType.Building: return "建筑";
            default: return "未知";
        }
    }

    public static string GetRarityName(GameJamRarity rarity)
    {
        switch (rarity)
        {
            case GameJamRarity.Common: return "普通";
            case GameJamRarity.Uncommon: return "优良";
            case GameJamRarity.Rare: return "稀有";
            case GameJamRarity.Epic: return "史诗";
            default: return "未知";
        }
    }
}
