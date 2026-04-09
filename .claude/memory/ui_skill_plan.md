---
name: UI Skill 规划
description: 基于截图生成 UGUI prefab 的 skill 开发计划
type: project
---

## 背景
WAO 项目有 ngui_prefab_structure skill（NGUI 参考文档）和 integrate-garden-building skill（自动生成 prefab）。本项目需要类似能力但基于 UGUI。

**Why:** 目前社区和官方没有现成的 UGUI prefab 生成 skill，需要自建
**How to apply:**

## 实施路径
1. 在 Unity Editor 中手动拼一个 UGUI 示例 prefab 作为模板
2. 分析该 prefab 的 YAML 结构，记录 UGUI 组件 GUID 和序列化格式
3. 编写 `.claude/commands/ugui_prefab_structure.md` 参考 skill
4. 后续可扩展为自动生成 skill（从截图 → prefab YAML）

## UGUI 核心组件（待补充 GUID）
- Canvas
- CanvasScaler
- GraphicRaycaster
- RectTransform
- Image
- Text / TextMeshProUGUI
- Button
- ScrollRect
- CanvasGroup
