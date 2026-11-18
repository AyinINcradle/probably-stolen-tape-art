# 磁带艺术：可能是磁带增强 · Tape Art

《深空当铺：可能是偷的》(Probably Stolen) 的模组。把店里那台吃灰的磁带播放机，变成整条街最有品味的声音。

- 创意工坊：<https://steamcommunity.com/sharedfiles/filedetails/?id=3841830230>
- 适用游戏版本：**1.0.0.x**（清单声明 `1.0.0`）

## 内容

**艺术品灯牌** —— 淡紫色磁带霓虹灯牌，占 2×2 格。放进展示柜即可招来磁带迷商人；开局选【磁带迷】也能获得一块。

**磁带迷商人** —— 上架五盘损坏或严重损坏的磁带与一块艺术品灯牌（已拥有则不再卖），只按店里的正常价格收购完好品质的磁带。收购走原版议价流程：只有摆到柜台上的磁带他会评估。

**王尔德人脉 · 磁带艺术**（基础页，供货商之后）

| 等级 | 价格 | 效果 |
|---|---|---|
| Ⅰ | 225 | 获得两盘完好磁带（后柜台），商人次日登门、此后每周至少一次；同时解锁用电子元件（普通）修复储藏区的损坏磁带 |
| Ⅱ | 540 | 播放机加装核心，播放区的完好磁带提升店铺吸引力；每晚扣除信用点，可用电子元件降低 |

**磁带播放机核心** —— 播放区里每盘完好品质磁带 +20 点店铺吸引力（最多 9 盘 = 180）；只看播放区，储藏区不算。核心装上后不可取下；未解锁Ⅱ之前播放机与原版一致。

**日历夜间报告** —— 吸引力条里磁带贡献的部分显示为淡紫色「磁带」段，并在下方写出当期电费。

## 安装

- 创意工坊订阅，或
- 把 `manifest.xml`、编译好的 `TapeArt.dll`、`Localization/`、`Textures/` 放进 `Probably Stolen/Mods/tape_art/`

游戏内「模组」菜单启用后需重启游戏。

## 从源码构建

需要 .NET SDK 6+（本项目用 netstandard2.1 目标）与游戏本体提供引用程序集。

```powershell
# 默认引用 E:\STEAM\steamapps\common\Probably Stolen，可用 -p:GameDir=... 覆盖
dotnet build src/TapeArt/TapeArt.csproj -c Release
```

产物在 `src/TapeArt/bin/Release/netstandard2.1/TapeArt.dll`。

**调试**：默认构建不含 F9 调试按钮；需要时加编译常量：

```powershell
dotnet build src/TapeArt/TapeArt.csproj -c Release -p:DefineConstants=TAPE_ART_DEBUG
```

此时会注册「Tape Art: Self Test」「Give 3 Pristine Tapes」「Unlock Tape Art I-II」「Reinstall Core Now」等按钮（需要游戏根目录有 `dev.txt`）。

## 仓库结构

```
src/TapeArt/          模组源码与资源
  *.cs                IMod 入口、钩子、补丁、物品、商人、日历色条
  Localization/       zh.csv / en.csv（文案外置，改文字不用重编译）
  Textures/Items/     灯牌、核心、图标的像素画
  tools/make_art.py   生成上述 PNG（Python + Pillow）
docs/                 创意工坊文案、验证清单、设计笔记、发布记录
```

## 实现要点

- 全部功能优先用游戏的 `ModHook` 钩子与 `ModHelper` API；只有原版没有对应入口时才用 Harmony（追加核心槽、吸引力叠加、日历色条、元件交互、刷客预测、升级价格兜底）。
- 文案外置在 CSV，缺条目时回退到内置中文，不会把 key 显示给玩家。
- 详细的原版对照与踩坑记录见 [docs/DESIGN.md](docs/DESIGN.md)。

## License

MIT，见 [LICENSE](LICENSE)。
