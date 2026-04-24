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
    public string prefabPath;

    public GameJamItemDef(string id, string name, string desc, GameJamItemType type,
        GameJamRarity rarity, int maxStack, int sellPrice, Color iconColor,
        string iconPath = null, string prefabPath = null)
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
        this.prefabPath = prefabPath;
    }
}

public static class GameJamItemDB
{
    static Dictionary<string, GameJamItemDef> items;

    static void Init()
    {
        if (items != null) return;
        items = new Dictionary<string, GameJamItemDef>();

        Reg(new GameJamItemDef("鐭冲潡", "鐭冲潡", "甯歌鐨勭煶鏉愶紝鍙敤浜庡缓閫犲熀纭€璁炬柦銆?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 2,
            new Color(0.6f, 0.6f, 0.58f)));

        Reg(new GameJamItemDef("鏈ㄦ潗", "鏈ㄦ潗", "鍧氶煣鐨勬湪澶达紝寤洪€犲拰鍒朵綔鐨勫熀纭€鏉愭枡銆?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 3,
            new Color(0.5f, 0.33f, 0.15f)));

        Reg(new GameJamItemDef("閾佺熆", "閾佺熆", "鍚搧閲忚緝楂樼殑鐭跨煶锛屽喍鐐煎悗鍙埗浣滃伐鍏峰拰姝﹀櫒銆?",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 5,
            new Color(0.4f, 0.42f, 0.5f)));

        Reg(new GameJamItemDef("閾滅熆", "閾滅熆", "鏌旇蒋鐨勯噾灞炵熆鐭筹紝閫傚悎鍒朵綔瑁呴グ鍝佸拰鍩虹宸ュ叿銆?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 4,
            new Color(0.72f, 0.45f, 0.2f)));

        Reg(new GameJamItemDef("鑽夎嵂", "鑽夎嵂", "鍏锋湁娌绘剤鍔熸晥鐨勬鐗╋紝鍙埗浣滆嵂姘淬€?",
            GameJamItemType.Consumable, GameJamRarity.Common, 999, 3,
            new Color(0.2f, 0.65f, 0.3f)));

        Reg(new GameJamItemDef("鐭抽晲", "鐭抽晲", "绠€闄嬩絾瀹炵敤鐨勯噰鐭垮伐鍏凤紝鎻愬崌鐭跨煶閲囬泦鏁堢巼銆?",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 15,
            new Color(0.55f, 0.55f, 0.5f)));

        Reg(new GameJamItemDef("閾佹枾", "閾佹枾", "閿嬪埄鐨勪紣鏈ㄥ伐鍏凤紝澶у箙鎻愬崌鏈ㄦ潗閲囬泦鏁堢巼銆?",
            GameJamItemType.Tool, GameJamRarity.Uncommon, 1, 30,
            new Color(0.5f, 0.52f, 0.6f)));

        Reg(new GameJamItemDef("鐨敳", "鐨敳", "鐢ㄥ吔鐨埗浣滅殑杞诲瀷鎶ょ敳锛屾彁渚涘熀纭€闃叉姢銆?",
            GameJamItemType.Equipment, GameJamRarity.Common, 1, 25,
            new Color(0.55f, 0.35f, 0.18f)));

        Reg(new GameJamItemDef("绾㈠疂鐭?", "绾㈠疂鐭?", "绋€鏈夌殑瀹濈煶锛屾暎鍙戠潃绁炵鐨勭孩鑹插厜鑺掋€?",
            GameJamItemType.Material, GameJamRarity.Rare, 999, 50,
            new Color(0.85f, 0.15f, 0.2f)));

        Reg(new GameJamItemDef("榫欓碁", "榫欓碁", "浼犺涓法榫欒韩涓婅劚钀界殑槌炵墖锛屾瀬鍏剁弽璐点€?",
            GameJamItemType.Material, GameJamRarity.Epic, 999, 200,
            new Color(0.6f, 0.1f, 0.7f)));

        Reg(new GameJamItemDef("宸ヤ綔鍙?", "宸ヤ綔鍙?", "鍩虹鍒朵綔璁炬柦锛屽彲鐢ㄤ簬鍔犲伐鏈ㄦ潗鍜岀煶鏂欍€?",
            GameJamItemType.Building, GameJamRarity.Common, 99, 20,
            new Color(0.45f, 0.35f, 0.2f)));

        Reg(new GameJamItemDef("姘戠敤鐔旂倝", "姘戠敤鐔旂倝", "楂樻俯鍐剁偧璁炬柦锛屽彲灏嗙熆鐭冲喍鐐间负閲戝睘閿€?",
            GameJamItemType.Building, GameJamRarity.Uncommon, 99, 35,
            new Color(0.6f, 0.25f, 0.15f)));

        Reg(new GameJamItemDef("鍒囧壊鏈?", "鍒囧壊鏈?", "绮惧瘑鐨勬湪鏉愬姞宸ヨ澶囷紝鍙皢鍘熸湪鍒囧壊涓烘湪鏉裤€?",
            GameJamItemType.Building, GameJamRarity.Common, 99, 25,
            new Color(0.45f, 0.45f, 0.5f)));

        Reg(new GameJamItemDef("娌欏瓙", "娌欏瓙", "缁嗚吇鐨勬矙绮掞紝楂樻俯鍐剁偧鍙埗鎴愮幓鐠冦€?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 1,
            new Color(0.85f, 0.78f, 0.55f)));

        Reg(new GameJamItemDef("鐜荤拑", "鐜荤拑", "鏅惰幑閫忔槑锛岄渶瑕侀珮娓╁喍鐐艰€屾垚銆?",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 12,
            new Color(0.7f, 0.85f, 0.9f)));

        Reg(new GameJamItemDef("鏈ㄦ澘", "鏈ㄦ澘", "鍒囧壊鍔犲伐鍚庣殑鏈ㄦ潗锛屽钩鏁寸粨瀹烇紝閫傜敤浜庡缓閫犮€?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 5,
            new Color(0.6f, 0.45f, 0.25f)));

        Reg(new GameJamItemDef("鏈ㄧ偔", "鏈ㄧ偔", "鏈ㄦ潗鐑у埗鑰屾垚鐨勭噧鏂欙紝鐕冪儳娓╁害楂樹笖鎸佷箙銆?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 4,
            new Color(0.2f, 0.18f, 0.15f)));

        Reg(new GameJamItemDef("鍌ㄧ墿绠?", "鍌ㄧ墿绠?", "鐢ㄤ簬瀛樻斁澶氫綑鐗╄祫鐨勬湪鍒剁瀛愩€?",
            GameJamItemType.Building, GameJamRarity.Common, 99, 10,
            new Color(0.5f, 0.38f, 0.2f)));

        Reg(new GameJamItemDef("閾滈敪", "閾滈敪", "閾滅熆鍐剁偧鑰屾垚鐨勯噾灞為敪锛岀敤浜庡埗浣滃熀纭€宸ュ叿鍜屽缓绛戙€?",
            GameJamItemType.Material, GameJamRarity.Common, 999, 8,
            new Color(0.78f, 0.5f, 0.2f)));

        Reg(new GameJamItemDef("閾侀敪", "閾侀敪", "閾佺熆鍐剁偧鑰屾垚鐨勯噾灞為敪锛屽潥鍥鸿€愮敤锛岄珮绾у埗浣滄潗鏂欍€?",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 12,
            new Color(0.55f, 0.55f, 0.6f)));

        Reg(new GameJamItemDef("闈掗摐閿?", "闈掗摐閿?", "閾滈敪涓庨搧閿悎閲戝喍鐐肩殑楂樼骇鏉愭枡锛屽吋鍏烽煣鎬т笌纭害銆?",
            GameJamItemType.Material, GameJamRarity.Uncommon, 999, 15,
            new Color(0.7f, 0.55f, 0.25f)));

        Reg(new GameJamItemDef("閾滈晲", "閾滈晲", "閾滃埗閲囩熆宸ュ叿锛屾瘮鐭抽晲鏇撮珮鏁堛€?",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 20,
            new Color(0.75f, 0.48f, 0.18f)));

        Reg(new GameJamItemDef("閾滄枾", "閾滄枾", "閾滃埗浼愭湪宸ュ叿锛屾瘮寰掓墜浼愭湪蹇緱澶氥€?",
            GameJamItemType.Tool, GameJamRarity.Common, 1, 25,
            new Color(0.73f, 0.46f, 0.16f)));

        Reg(new GameJamItemDef("缁峰甫", "缁峰甫", "鐢ㄨ崏鑽埗鎴愮殑绠€鏄撶环甯︼紝鍙仮澶嶅皯閲忕敓鍛藉€笺€?",
            GameJamItemType.Consumable, GameJamRarity.Common, 99, 5,
            new Color(0.9f, 0.88f, 0.8f)));

        Reg(new GameJamItemDef(GameJamCropDB.PlanterItemId, GameJamCropDB.PlanterItemId, "可放置的种植容器。",
            GameJamItemType.Building, GameJamRarity.Common, 99, 5,
            new Color(0.50f, 0.34f, 0.20f),
            iconPath: "Assets/Games/GameJam/assets/UI/sprites/package/Item_PlantBox01.png",
            prefabPath: "Games/GameJam/assets/Model/itemmall/ItemMall_PlantBox_01.prefab"));

        Reg(new GameJamItemDef(GameJamCropDB.RadishSeedItemId, GameJamCropDB.RadishSeedItemId, "用于种植萝卜。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 1,
            new Color(0.72f, 0.58f, 0.20f),
            iconPath: "Assets/Games/GameJam/assets/UI/sprites/package/Item_seed_radish.png",
            prefabPath: "Games/GameJam/assets/Model/itemmall/Item_Plantseed.prefab"));

        Reg(new GameJamItemDef(GameJamCropDB.RadishItemId, GameJamCropDB.RadishItemId, "成熟后可收获的萝卜。",
            GameJamItemType.Material, GameJamRarity.Common, 999, 2,
            new Color(0.84f, 0.28f, 0.30f),
            iconPath: "Assets/Games/GameJam/assets/UI/sprites/package/Item_material_radish.png",
            prefabPath: "Games/GameJam/assets/Model/plant/Plant_radish.prefab"));

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
                iconPath,
                entry.prefabPath));
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
            def.iconPath ?? string.Empty, "|",
            def.prefabPath ?? string.Empty);

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
            case GameJamItemType.Material: return "鏉愭枡";
            case GameJamItemType.Tool: return "宸ュ叿";
            case GameJamItemType.Consumable: return "娑堣€楀搧";
            case GameJamItemType.Equipment: return "瑁呭";
            case GameJamItemType.Building: return "寤虹瓚";
            default: return "鏈煡";
        }
    }

    public static string GetRarityName(GameJamRarity rarity)
    {
        switch (rarity)
        {
            case GameJamRarity.Common: return "鏅€?";
            case GameJamRarity.Uncommon: return "浼樿壇";
            case GameJamRarity.Rare: return "绋€鏈?";
            case GameJamRarity.Epic: return "鍙茶瘲";
            default: return "鏈煡";
        }
    }
}
