# 磁带艺术 · 验证清单

游戏：`E:\STEAM\steamapps\common\Probably Stolen`（1.0.0.15）
模组：`Mods\tape_art\`
日志：`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log`

## 0. 准备

- 游戏根目录需要空的 `dev.txt`（已建好），否则 F8/F9 不响应。
- 本地模组每次重编译都会变哈希：模组菜单里若提示「自你批准后已更改」，点**审核并启用**后重启；建议在详情面板开**开发者模式**，之后就不必反复审核。

## 1. 加载（不需要进游戏）

日志里应出现：

```
[ModLoader] Loading: TapeArt.dll
[ModLoader]   Harmony: 4 method(s) patched
[... Tape Art]: init, game version 1.0.0.15
[... Tape Art]: enabled: items and merchant registered
[ModLoader] Item sprite loaded: tape_art_sign / tape_art_core
[... Tape Art]: catalog: registered tape_art_sign, tape_art_core
```

## 2. 进档后（读档或开新局）

```
[... Tape Art]: cassette player: core bay installed (5 panels)
[... Tape Art]: catalog: sign=True core=True
[... Tape Art]: storages: storage=True cassette=True coreBay=True, core=False
[... Tape Art]: verify: run upgrade dict I=True II=True III=True, merchant blueprint = True
```

买了 Ⅱ 之后应额外出现：

```
[... Tape Art]: cassette player core installed (permanent)
```

## 3. F9 快速验证

F9 → Special → **Tape Art: Self Test**，F8 里应看到：

```
self test: panels = [storage,cassette,controls,volume,tape_art_core]
self test: storage=True cassette=True coreBay=True core=True
self test: usage=40/night, powered=?, attraction=+N
self test: I=True II=True III=True
```

- `usage` 会随核心等级从 40 降到 20（每级 −4）。
- `attraction`：播放区每盘完好磁带 +20，最多 180；现金够付当晚电费时立刻生效，不用等当晚结算。

## 4. 三项功能实测

| 功能 | 操作 | 期望 |
|---|---|---|
| 灯牌拉客 | 把艺术品灯牌放**展示柜** | 次日有几率来磁带迷商人；日志 `tape-hoarder merchant stocked (...)` |
| 商人 | 与商人对话 | 卖 5 盘随机品质磁带 + 灯牌；对话里有「我有些磁带要出手」，只收磁带，按正常估价 |
| 电费 | 睡觉 | 日志 `cassette core: powered (40 credits/night)`，现金 −40；付不起则 `no power`，吸引力归零 |
| 修复 | Ⅲ 后，把 `电子元件（普通）` 拖到储藏区里 `cassetteNl`/`d` | 日志 `cassette repaired: cassette3l -> cassette3` |

## 5. 常见问题

| 现象 | 原因 |
|---|---|
| `core install failed: slot rejected the item` | 认槽失败；把背包里那块核心手动拖进「核心」槽即可（槽只收核心） |
| `catalog: ... core=False` | 物品注册时机问题，需要反馈日志 |
| `attraction=+0` 但已装核心 | 现金不够付电费；或播放区里没有完好磁带 |
| 灯牌不拉客 | 灯牌必须在**展示柜**里且是自己拥有的 |
