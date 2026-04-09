---
name: 项目概况
description: Test01-Snake 项目技术栈、结构、UI 方案分析
type: project
---

## 项目基本信息
- Unity 2021.3.29f1，UGUI (com.unity.ugui 1.0.0)，TextMeshPro 3.0.6
- 包含三个小游戏：Snake、Tetris、Raft Survival + Lobby 大厅
- 所有 UI 当前为代码动态创建（无 prefab），使用 Text + Arial/Microsoft YaHei 字体

## 目录结构
- Assets/Scripts/Lobby/ — 大厅菜单
- Assets/Scripts/Snake/ — 贪吃蛇 (SnakeGame.cs)
- Assets/Scripts/Tetris/ — 俄罗斯方块 (TetrisGame.cs)
- Assets/Scripts/Raft/ — 木筏生存 (14个脚本，最完整的模块)
- Assets/Editor/ — Excel 配置导入工具
- Assets/Resources/RaftConfigs/ — JSON 配置表
- Assets/Excels/ — Excel 原始配置

## Raft 模块脚本清单
RaftGame, RaftUI, RaftManager, PlayerController, Ocean, SharkAI, Inventory, SurvivalStats, FloatingResource, ResourceSpawner, RaftBlock, RaftConfigTables, HookThrower, ProceduralMeshUtil

## UI 方案建议
**Why:** 项目 UI 简单，不需要热更新，NGUI+Lua 方案复杂度过高
**How to apply:**
- 推荐 UGUI + 纯 C# MonoBehaviour
- 如需从截图生成 prefab，可参考 WAO 项目的 skill 模式，创建一个 UGUI prefab 参考 skill
- UGUI prefab 结构比 NGUI 简单（RectTransform + Canvas + Image/TMP_Text/Button）
- 如未来有热更需求考虑 HybridCLR 而非 Lua
