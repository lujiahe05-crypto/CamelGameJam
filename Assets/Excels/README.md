# Raft Config Excel 说明

把一个 `.xlsx` 文件导入到 Unity 后，可以通过菜单 `Tools/Raft/Import Config Excel...` 转成：

- `Assets/Resources/RaftConfigs/BuildingTable.json`
- `Assets/Resources/RaftConfigs/ItemTable.json`
- `Assets/Resources/RaftConfigs/RefreshTable.json`
- `Assets/Resources/RaftConfigs/SurvivalTable.json`

## 工作表名称

Excel 里需要这 4 张工作表，名字固定：

- `building`
- `item`
- `refresh`
- `survival`

## building 表

一行代表一个建造消耗项。

| buildingId | displayName | costItemType | costAmount |
| --- | --- | --- | --- |
| raft_foundation | 木筏地基 | Wood | 1 |
| raft_foundation | 木筏地基 | Plastic | 2 |

说明：

- 同一个 `buildingId` 可以写多行，表示多个消耗项。
- `costItemType` 必须和代码里的 `ItemType` 名字一致，比如 `Wood`、`Plastic`。

## item 表

一行代表一个可恢复物品。

| itemType | displayName | hungerRestore | thirstRestore |
| --- | --- | --- | --- |
| Beet | 甜菜 | 35 | 0 |
| WaterBottle | 矿泉水 | 0 | 40 |
| Coconut | 椰子 | 15 | 20 |

## refresh 表

同一张表里同时写刷新基础规则和资源权重。

### 设置行

| rowType | key | value |
| --- | --- | --- |
| setting | maxResources | 20 |
| setting | spawnInterval | 2.5 |
| setting | minSpawnDistance | 15 |
| setting | maxSpawnDistance | 40 |
| setting | despawnDistance | 60 |

### 资源权重行

| rowType | resourceType | weight |
| --- | --- | --- |
| resource | Wood | 30 |
| resource | Plastic | 25 |
| resource | Coconut | 15 |
| resource | Beet | 15 |
| resource | WaterBottle | 15 |

说明：

- `resourceType` 必须和代码里的 `ResourceType` 名字一致。

## survival 表

用键值形式写生存数值。

| key | value |
| --- | --- |
| maxHealth | 100 |
| maxHunger | 100 |
| maxThirst | 100 |
| initialHealth | 100 |
| initialHunger | 100 |
| initialThirst | 100 |
| hungerRate | 0.5555556 |
| thirstRate | 0.8333333 |
| starveDamage | 5 |
| respawnDelay | 3 |
| respawnHealth | 100 |
| respawnHunger | 80 |
| respawnThirst | 80 |
