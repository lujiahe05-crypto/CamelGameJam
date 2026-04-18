# Thronefall Config Excel 说明

Excel 需要 4 张工作表：

- `building`
- `monster`
- `wave`
- `hero`

## 读取规则

- 第 1 行：中文备注，不读取，只给策划看含义。
- 第 2 行：字段名，导入器按这一行识别列。
- 第 3 行开始：正式数据。

菜单入口：`Tools → Thronefall → Import Config Excel...`

导入后输出到 `Assets/Resources/ThronefallConfigs/` 下 4 个 JSON 文件。

---

## building 表（建筑配置）

一行表示一个建筑变体（含升级后的版本）。

字段：

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `buildingId` | int | 建筑唯一 ID |
| `buildingName` | string | 建筑名称 |
| `description` | string | 功能描述 |
| `buildingType` | string | 类型：`economic` / `tower` / `wall` / `barracks` / `base` |
| `coinCost` | int | 建造/升级消耗金币 |
| `maxHP` | int | 最大生命值 |
| `atk` | int | 攻击力（非攻击建筑填 0） |
| `def` | int | 防御力 |
| `attackRange` | float | 攻击距离（非攻击建筑填 0） |
| `attackInterval` | float | 攻击间隔（非攻击建筑填 0） |
| `arrowSpeed` | float | 箭矢飞行速度（塔类填值，其他填 0） |
| `arcHeight` | float | 抛物线最高点高度（塔类填值，其他填 0） |
| `aoeRadius` | float | 范围伤害半径（0 = 单体伤害） |
| `dailyYield` | int | 每日产出金币（经济类填值，其他填 0） |
| `recruitCost` | int | 单次招募花费金币（兵营填值，其他填 0） |
| `maxRecruits` | int | 最大招募人数（兵营填值，其他填 0） |
| `upgradeIds` | string | 升级指向的建筑 ID 列表，逗号分隔（如 `10,11`）。单线升级填 1 个，分支填多个，终极不填 |
| `branchIcon` | string | 分支选择面板的图标文字（如 `LB`、`FO`） |
| `allyMaxHP` | int | 友军最大生命值（兵营填值，其他填 0） |
| `allyAtk` | int | 友军攻击力（兵营填值，其他填 0） |
| `allyDef` | int | 友军防御力（兵营填值，其他填 0） |
| `allyMoveSpeed` | float | 友军移动速度（兵营填值，其他填 0） |
| `allyAttackRange` | float | 友军攻击距离（兵营填值，其他填 0） |
| `allyAttackInterval` | float | 友军攻击间隔（兵营填值，其他填 0） |

当前建筑 ID：

- `1` = Arrow Tower（箭塔，可升级为 10 或 11）
- `2` = Wall（城墙，可升级为 20）
- `3` = Castle Center（城堡中心 / 主基地）
- `4` = House（房屋，可升级为 40）
- `5` = Barracks（兵营）
- `10` = Longbow Tower（长弓塔，箭塔分支升级）
- `11` = Fire Oil Tower（火油塔，箭塔分支升级）
- `20` = Fortified Wall（强化城墙，城墙升级）
- `40` = Manor（庄园，房屋升级）

---

## monster 表（怪物配置）

一行表示一个怪物类型。

字段：

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `monsterId` | int | 怪物 ID |
| `monsterName` | string | 怪物名称 |
| `maxHP` | int | 最大生命值 |
| `atk` | int | 攻击力 |
| `def` | int | 防御力 |
| `moveSpeed` | float | 移动速度 |
| `attackRange` | float | 攻击距离 |
| `attackInterval` | float | 攻击间隔（秒） |
| `iconLabel` | string | 波次预警图标字符 |

当前怪物 ID：

- `1` = Goblin（哥布林）
- `2` = Orc（兽人）
- `3` = Skeleton（骷髅）

---

## wave 表（波次配置）

混合行类型，使用 `rowType` 区分。

### rowType = setting（全局设置）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `rowType` | string | 固定 `setting` |
| `key` | string | 配置键名 |
| `value` | string | 配置值 |

可用键名：

- `dailyBaseCoins` — 每天基础奖励金币
- `startingCoins` — 初始金币数量

### rowType = spawn（刷怪行）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `rowType` | string | 固定 `spawn` |
| `dayNumber` | int | 第几天（从 1 开始） |
| `spawnX` | float | 刷怪点 X 坐标 |
| `spawnY` | float | 刷怪点 Y 坐标（通常 0） |
| `spawnZ` | float | 刷怪点 Z 坐标 |
| `monsterId` | int | 刷出的怪物 ID |
| `count` | int | 该类怪物数量 |
| `spawnInterval` | float | 两只怪之间的间隔（秒） |

**分组规则：**

- 相同 `dayNumber` 的行归入同一波次。
- 同一波次中，相同坐标 `(spawnX, spawnY, spawnZ)` 的行归入同一个刷怪点。
- 同一刷怪点内的多行按顺序形成刷怪序列（先刷完第一行再刷第二行）。

**示例：**

| rowType | key | value | dayNumber | spawnX | spawnY | spawnZ | monsterId | count | spawnInterval |
|---------|-----|-------|-----------|--------|--------|--------|-----------|-------|---------------|
| setting | dailyBaseCoins | 40 | | | | | | | |
| setting | startingCoins | 100 | | | | | | | |
| spawn | | | 1 | 30 | 0 | 0 | 1 | 3 | 1.5 |
| spawn | | | 2 | 30 | 0 | 0 | 1 | 5 | 1.2 |
| spawn | | | 2 | -30 | 0 | 0 | 1 | 3 | 1.5 |
| spawn | | | 3 | 30 | 0 | 0 | 1 | 4 | 1.0 |
| spawn | | | 3 | 30 | 0 | 0 | 2 | 2 | 2.0 |
| spawn | | | 3 | 0 | 0 | 30 | 3 | 4 | 1.2 |

---

## hero 表（英雄 / 武器 / 技能配置）

混合行类型，使用 `rowType` 区分。

### rowType = hero（英雄基础属性）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `rowType` | string | 固定 `hero` |
| `key` | string | 配置键名 |
| `value` | float | 配置值 |

可用键名：

- `maxHP` — 英雄最大生命值
- `moveSpeed` — 移动速度
- `reviveTime` — 复活等待时间（秒）

### rowType = weapon（武器配置）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `rowType` | string | 固定 `weapon` |
| `weaponId` | string | 武器唯一标识（如 `spear`、`bow`） |
| `weaponName` | string | 武器显示名称 |
| `atk` | int | 基础攻击力 |
| `def` | int | 附加防御力 |
| `attackRange` | float | 攻击距离 |
| `attackInterval` | float | 攻击间隔（秒） |
| `arrowSpeed` | float | 箭矢水平飞行速度（近战武器填 0） |
| `arcHeight` | float | 抛物线最高点高度（近战武器填 0） |

### rowType = skill（技能配置）

通过 `weaponId` 关联到对应武器。

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `rowType` | string | 固定 `skill` |
| `weaponId` | string | 关联的武器 ID |
| `skillId` | string | 技能唯一标识（如 `thrust`、`volley`） |
| `cooldown` | float | 技能冷却时间（秒） |
| `damageMultiplier` | float | 技能伤害倍率 |
| `accelTime` | float | 突刺加速时长（弓箭填 0） |
| `decelTime` | float | 突刺减速时长（弓箭填 0） |
| `maxSpeed` | float | 突刺最大速度（弓箭填 0） |
| `arrowCount` | int | 齐射箭矢数量（长矛填 0） |
| `spreadAngle` | float | 齐射扇形扩散角度（长矛填 0） |
| `arrowSpeed` | float | 齐射箭矢飞行速度（长矛填 0） |
| `arcHeight` | float | 齐射抛物线高度（长矛填 0） |

**示例：**

| rowType | key | value | weaponId | weaponName | atk | def | attackRange | attackInterval | arrowSpeed | arcHeight | skillId | cooldown | damageMultiplier | accelTime | decelTime | maxSpeed | arrowCount | spreadAngle |
|---------|-----|-------|----------|------------|-----|-----|-------------|----------------|------------|-----------|---------|----------|------------------|-----------|-----------|----------|------------|-------------|
| hero | maxHP | 100 | | | | | | | | | | | | | | | | |
| hero | moveSpeed | 8 | | | | | | | | | | | | | | | | |
| hero | reviveTime | 10 | | | | | | | | | | | | | | | | |
| weapon | | | spear | Spear | 20 | 3 | 2.5 | 0.8 | 0 | 0 | | | | | | | | |
| weapon | | | bow | Bow | 12 | 1 | 12 | 1.2 | 15 | 4 | | | | | | | | |
| skill | | | spear | | | | | | | | thrust | 8 | 1.5 | 0.5 | 0.5 | 20 | 0 | 0 |
| skill | | | bow | | | | | | | | volley | 12 | 0.8 | 0 | 0 | 0 | 5 | 60 |
