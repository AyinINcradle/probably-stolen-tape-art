# 发布记录 · 磁带艺术 | Tape Art

## 创意工坊物品

| 项 | 值 |
|---|---|
| 物品 ID | **3841830230** |
| 游戏 | Probably Stolen（appid 4348910） |
| 标题 | 磁带艺术：可能是磁带增强 \| Tape Art |
| 可见性 | 公开（作者已在 Steam 手动设置） |
| 当前版本 | **1.0.1** |
| 页面 | https://steamcommunity.com/sharedfiles/filedetails/?id=3841830230 |

## 发布包

`D:\Deepseek\release\tape_art`（上传内容与它完全一致）

```
manifest.xml
TapeArt.dll
preview.png
Localization/zh.csv, en.csv
Textures/Items/tape_art_sign.png, tape_art_core.png, tape_art_icon.png
```

上传配置：`D:\Deepseek\release\workshop\tape_art.vdf`（`publishedfileid` 已写死为 3841830230，`visibility` 为 0）

## 版本历史

| 版本 | 内容 |
|---|---|
| 1.0.0 | 首次隐藏发布：灯牌、商人、两级升级、核心、修复、随色条标签、电费行 |
| 1.0.1 | 修复Ⅱ级价格在新存档未生效（`NetworkUpgrade.GetCost` postfix 兜底为 540）；重做封面与描述格式 |

## 更新流程（修 bug 后）

1. 改 `D:\Deepseek\mods-src\TapeArt` 源码（需要 F9 调试按钮时加编译常量 `TAPE_ART_DEBUG`）；
2. 构建：
   ```powershell
   dotnet build D:\Deepseek\mods-src\TapeArt\TapeArt.csproj -c Release
   ```
3. 覆盖发布包并递增 `<ModVersion>`：
   ```
   copy bin\Release\netstandard2.1\TapeArt.dll  D:\Deepseek\release\tape_art\
   ```
4. 更新 `workshop\tape_art.vdf` 的 `changenote`；
5. 上传（需要 Steam Guard 验证码）：
   ```powershell
   & D:\Deepseek\tools\steamcmd\steamcmd.exe `
       +set_steam_guard_code <5位码> `
       +login 1916964075 `
       +workshop_build_item D:\Deepseek\release\workshop\tape_art.vdf +quit
   ```
6. 玩家侧 Steam 会自动把订阅版更新到新版本。

## 注意

- 本地 `Mods\tape_art` 保持为空，避免和订阅版抢同一个 `<ID>`（`tape_art`）；
- 旧调试版备份：`D:\Deepseek\release\backup\mods_tape_art_0.26.2`；
- 早期测试存档里Ⅱ级仍显示 270（那局的运行时升级条目在旧版本生成），新存档正常，不影响玩家。
