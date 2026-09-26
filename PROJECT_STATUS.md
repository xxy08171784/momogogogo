# MOMOGOGOGO 当前开发状态

> 更新时间：2026-09-26  
> 当前阶段：赛前练习 Demo / 核心玩法原型  
> 当前主要开发语言：C#
> 架构方向：MVC 思路拆分，玩法逻辑与 UI 表现尽量解耦
> 2026-09-26：核心玩法脚本已从 GDScript 转换为 C#。原 GDScript 已本地备份到 `Backup/GDScript/`，该目录已加入 `.gitignore`，不会上传 GitHub。

## 1. 当前整体框架

目前已经搭建出的核心流程是：

```text
ReadyToRoll
    ↓ 玩家点击“掷骰子”
Rolling
    ↓ 红蓝骰同时生成结果并播放动画
WaitingForDiceSelection
    ↓ UI / Controller 把玩家选择的 red / blue 传给玩法层
Moving
    ↓ Player 根据骰子点数计算目标格，并逐格 Tween 移动
Resolving
    ↓ Map 根据落脚格执行地块结算
ReadyToRoll
```

当前职责大致如下：

- `GameState.cs`：全局运行时数据、角色基础属性、临时属性、回合状态、骰子结果。
- `MapTileData.cs`：单个地图格的数据结构。
- `Map.cs`：地图生成、骰子规则、回合流程、地块结算、对 UI / Controller 提供玩法接口。
- `Player.cs`：目标格计算、逐格移动动画、角色升级接口。
- `Dice.cs`：骰子的视觉动画。
- `map.tscn`：地图、Player、红蓝骰、RollButton、12 个格子 Marker2D 的场景组合。
- `Player.tscn`：角色场景。
- `Dice.tscn`：可复用骰子场景。

---

## 2. 主要文件与职责

### Scripts/Data/GameState.cs

作为全局状态数据源，目前保存：

```text
TileCount = 12

PlayerLevel
PlayerHp
PlayerAtk
PlayerDef

TempAtk
TempDef

PlayerPosition
DiceColor
dice["red"]
dice["blue"]
CurrentTurnState
```

当前回合状态：

```csharp
enum TurnState {
    ReadyToRoll,
    Rolling,
    WaitingForDiceSelection,
    Moving,
    Resolving
}
```

临时属性清空接口：

```csharp
GameState.Instance.ResetTempStats()
```

当前初始化角色属性：

```text
Level = 1
HP = 30
ATK = 3
DEF = 1
```

---

### Scripts/Data/MapTileData.cs

每个地图格是一个 `MapTileData` Resource。

当前数据：

```text
color
level
value
```

地块颜色：

```text
WHITE
BLACK
RED
BLUE
```

当前最大等级：

```text
MaxUpgrade = 2
```

---

### Scripts/Map/Map.cs

当前负责：

1. 创建 12 个逻辑地块。
2. 给 12 个地块指定颜色。
3. 生成红骰和蓝骰随机结果。
4. 管理本回合状态。
5. 接收 UI / Controller 的骰子选择。
6. 调用 Player 完成人物移动。
7. 等移动结束后执行地块结算。
8. 控制 RollButton 在不允许掷骰时禁用。

地图当前固定颜色顺序：

```text
0  WHITE
1  RED
2  BLUE
3  WHITE
4  RED
5  BLUE
6  BLACK
7  RED
8  BLUE
9  WHITE
10 RED
11 WHITE
```

---

### Scripts/Entities/Player.cs

当前负责：

1. 游戏开始时把 Player 放到 0 号格。
2. 根据步数计算目标格。
3. 支持循环地图：

```text
例如：
当前位置 10
前进 4 格

10 → 11 → 0 → 1 → 2
```

4. 根据 `Marker2D0 ~ Marker2D11` 逐格移动。
5. 每格使用 Tween 移动约 0.2 秒。
6. 移动完成后更新 `GameState.Instance.PlayerPosition`。
7. 提供角色升级接口。

---

### Scenes/Entities/Dice.cs

当前只负责骰子视觉动画。

动画逻辑：

```text
快速随机切换 10 次骰子面
每次间隔 0.06 秒
最后停在真正的骰子结果
```

不会自己决定游戏规则，也不会直接负责角色移动。

---

### Scenes/Map/map.tscn

目前场景包含：

```text
Map
├── Sprite2D
├── Entities
│   ├── RedDice
│   ├── BlueDice
│   └── Player
├── UI
│   └── RollButton
└── TilePoints
    ├── Marker2D0
    ├── Marker2D1
    ├── ...
    └── Marker2D11
```

其中：

- `RedDice.DiceColor = "red"`
- `BlueDice.DiceColor = "blue"`
- 12 个 Marker2D 对应 12 个地图格的实际画面位置。

---

## 3. 当前已实现功能

### 地图

- 12 格循环地图数据。
- 四种地块颜色。
- 每个地块拥有 `color / level / value`。
- 已建立 12 个 Marker2D 作为角色实际移动坐标。

### 骰子

- 红骰随机 1~6。
- 蓝骰随机 1~6。
- 一个按钮同时投掷两个骰子。
- 两个骰子可同时播放简单动画。
- 最终骰子画面会停在真实结果。

### 玩家移动

- 根据被选骰子的点数计算目标格。
- 使用 `posmod` 支持循环地图。
- Player 会沿 Marker2D 一格一格移动，而不是直接瞬移。
- 每一格使用 Tween 平滑移动。
- 动画完成后才更新逻辑位置。

### 回合控制

- 准备掷骰。
- 正在掷骰。
- 等待玩家选择骰子。
- 正在移动。
- 正在结算。
- 非 `ReadyToRoll` 状态下 RollButton 会被禁用。
- 移动期间重复选择骰子会被玩法接口拒绝。

### 临时属性

- 已增加：

```csharp
GameState.Instance.TempAtk
GameState.Instance.TempDef
```

- 当前红格提供临时攻击。
- 当前蓝格提供临时防御。
- 可通过：

```csharp
GameState.Instance.ResetTempStats()
```

统一清空。

### 地块成长

当前保留的成长逻辑：

- RED / BLUE / WHITE 会增加 `value`。
- BLACK 会减少 `value`。
- 地块达到 `MaxUpgrade` 后不再继续升级。
- 当前结算顺序是：

```text
先按当前 value 给予本次效果
↓
再执行地块自身成长
```

### 角色升级接口

当前提供：

```csharp
player.Upgrade("hp")
player.Upgrade("atk")
player.Upgrade("def")
```

也可以指定数值：

```csharp
player.Upgrade("atk", 2)
```

成功升级会同时使：

```text
PlayerLevel + 1
```

具体升级数值目前仍属于临时规则，后续按策划方案调整。

---

## 4. 对外接口

### UI / Controller 最重要的接口

玩家点击红骰时：

```csharp
map.HandleDiceSelected("red")
```

玩家点击蓝骰时：

```csharp
map.HandleDiceSelected("blue")
```

该接口内部会自动执行：

```text
检查当前是否允许选择
↓
记录骰子颜色
↓
读取对应骰子点数
↓
切换为 Moving
↓
Player 计算目标格
↓
Player 逐格移动
↓
切换为 Resolving
↓
地块结算
↓
恢复 ReadyToRoll
```

调用者不需要自己处理移动和结算。

---

### 骰子选择底层接口

```csharp
map.SelectDiceColor(string color) -> bool
```

合法值：

```text
"red"
"blue"
```

非法字符串返回 `false`。

通常 UI 层应该优先调用 `HandleDiceSelected()`，而不是直接调用这个底层接口。

---

### 投骰接口

```csharp
map.RollDice()
```

执行后结果保存在：

```csharp
GameState.Instance.Dice["red"]
GameState.Instance.Dice["blue"]
```

正常游戏流程中由 `RollButton` 自动调用。

---

### Player 移动接口

```csharp
await player.MoveBySteps(steps)
```

输入前进格数，Player 自动：

```text
计算目标格
→ 逐格移动
→ 更新 GameState.Instance.PlayerPosition
```

更底层的移动接口：

```csharp
await player.MoveToPosition(target_tile)
```

---

### 地块结算接口

```csharp
map.ResolveTileEffect()
```

根据：

```csharp
GameState.Instance.PlayerPosition
```

找到当前落脚格并执行效果。

正常流程中由 `HandleDiceSelected()` 在人物移动结束后自动调用。

---

### 回合状态接口

```csharp
map.SetTurnState(newState)
```

状态定义位于：

```csharp
GameState.TurnState
```

UI 如果需要根据回合状态控制显示，也可以读取：

```csharp
GameState.Instance.CurrentTurnState
```

---

### 角色升级接口

```csharp
player.Upgrade(choice, amount)
```

`choice` 当前支持：

```text
"hp"
"atk"
"def"
```

非法选择返回 `false`。

---

## 5. 当前尚未实现 / 暂不实现

### 本阶段明确暂不实现

- 攻击系统。
- 防御系统。
- 完整战斗系统。

### 仍待实现

- 玩家点击红骰 / 蓝骰的 UI 交互。
  - 建议由 UI / Controller 负责。
  - UI 最终只需要调用：
    `HandleDiceSelected("red")` 或 `HandleDiceSelected("blue")`。

- WHITE 地块对玩家的具体效果。
- BLACK 地块对玩家的具体效果。
- `TempAtk / TempDef` 的正式清空时机。
- 角色升级的正式策划数值。
- 升级选择 UI。
- 地块结算反馈 UI / 动画。
- 移动过程中的角色正式行走动画、美术表现。
- 骰子正式 UI、美术和交互反馈。
- 完整存档接入当前 GameState。
- 回合状态与 UI 的完整联动。

---

## 6. 当前建议的 UI / 玩法分工

### 玩法侧

负责：

- 地图规则。
- 骰子数值。
- 玩家目标格计算。
- 玩家实际移动。
- 地块结算。
- 回合状态。
- 角色属性数据。

### UI / Controller 侧

负责：

- 红骰 / 蓝骰是否可点击及视觉反馈。
- 玩家点击具体骰子后调用玩法接口。
- 骰子结果显示。
- 面板、按钮、文字。
- 升级选项界面。
- 根据 `GameState.Instance.CurrentTurnState` 调整 UI 状态。

双方当前最核心的对接点：

```csharp
map.HandleDiceSelected("red")
map.HandleDiceSelected("blue")
```

---

## 7. 当前需要继续确认的规则

以下内容仍需要策划规则最终确定：

1. WHITE 地块的实际效果。
2. BLACK 地块的实际效果。
3. 红蓝地块临时属性持续多久。
4. `TempAtk / TempDef` 在什么时候清空。
5. HP / ATK / DEF 每次升级分别增加多少。
6. 地块满级后是否仍然触发玩家属性效果。
7. 地块成长与玩家获得效果的最终先后顺序。

这些规则确认后，可以继续补全 `ResolveTileEffect()` 和角色成长部分。
