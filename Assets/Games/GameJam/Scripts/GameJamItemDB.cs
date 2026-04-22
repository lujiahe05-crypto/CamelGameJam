using UnityEngine;
using System.Collections.Generic;

public enum GameJamItemType { Material, Tool, Consumable, Equipment }
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

    public GameJamItemDef(string id, string name, string desc, GameJamItemType type,
        GameJamRarity rarity, int maxStack, int sellPrice, Color iconColor)
    {
        this.id = id;
        this.name = name;
        this.description = desc;
        this.type = type;
        this.rarity = rarity;
        this.maxStack = maxStack;
        this.sellPrice = sellPrice;
        this.iconColor = iconColor;
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
    }

    static void Reg(GameJamItemDef def) => items[def.id] = def;

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
