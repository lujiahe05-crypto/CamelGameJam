# Raft Config Excel 说明

Excel 需要 5 张工作表：

- `building`
- `item`
- `refresh`
- `survival`
- `synthesis`

## 读取规则

- 第 1 行：中文备注，不读取，只给策划看含义。
- 第 2 行：字段名，导入器按这一行识别列。
- 第 3 行开始：正式数据。

## 道具 ID

当前 `ItemType` 数字 ID：

- `0` = None
- `1` = Hook
- `2` = BuildHammer
- `3` = Wood
- `4` = Plastic
- `5` = Coconut
- `6` = Beet
- `7` = WaterBottle

## building 表

- 一行表示一个建造消耗项。`buildingId` 使用数字 ID 配置。
- 同一个 `buildingId` 可以写多行，表示多个消耗。

字段：

- `buildingId`
- `displayName`
- `costItemTypeId`
- `costAmount`

## item 表

- 一行表示一个物品配置。
- 建议把当前游戏内全部道具都维护进来，不只是可食用道具。
- 非消耗类道具的 `hungerRestore` 和 `thirstRestore` 填 `0`。

字段：

- `itemTypeId`
- `displayName`
- `hungerRestore`
- `thirstRestore`

## refresh 表

- `rowType=setting`：刷新系统基础配置。
- `rowType=resource`：漂浮物权重配置。

字段：

- `rowType`
- `key`
- `value`
- `itemTypeId`
- `weight`

## survival 表

- 使用键值配置生存数值。

字段：

- `key`
- `value`

## synthesis 表

- 一行表示一个合成输入项。
- 同一个 `recipeId` 可以写多行，表示多个输入材料。
- 当前只负责配置数据，不负责实现合成逻辑。

字段：

- `recipeId`
- `displayName`
- `inputItemTypeId`
- `inputAmount`
- `outputItemTypeId`
- `outputAmount`

