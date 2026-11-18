---
name: probably-stolen-modding
description: 为《深空当铺：可能是偷的》(Probably Stolen) 开发、调试并发布模组：需求访谈、反编译定位原版实现、Harmony/ModHook 编码、编译安装、读日志验证、创意工坊与 GitHub 发布。当用户提到"深空当铺模组""Probably Stolen mod""做灯牌/物品/人脉升级/商人"时使用。
whenToUse: 用户要求为 Probably Stolen 制作或修改模组、排查模组不生效、更新创意工坊物品或把模组项目推到 GitHub 时。
---

# 深空当铺模组开发（Probably Stolen Modding）

## 0. 铁律：先问清楚，再动手

用户描述通常是"我想要一个……"的雏形，越早写代码越容易返工。本技能的硬性要求：

1. **一次只问一个问题**（最多两个高度相关的），不要一次抛出五六个问题。
2. 每个回答后**追问**关键歧义：数值来源、触发时机、与已有系统冲突时怎么处理、失败了要什么表现。
3. **直到你有 ≥95% 的把握理解真实目标**，才给出最终方案；把握不足时继续问，不要"先做一版看看"。
4. 方案里显式列出：改动清单、要碰的原版系统、需要的资源（美术/文案）、验收方式。
5. 实现过程中若发现需求与游戏机制冲突（例如"完美品质"在原版叫"完好品质"、"该词条已存在"），**先报告再改**，不要默默替换用户设定。

判断"95%"的自检清单：

- 触发条件明确（什么时候发生）
- 数值与上限明确（含"没解锁时"的行为）
- 与原有系统冲突时的优先级明确
- 失败/满仓/未通电等边界的期望表现明确
- 是否需要新美术资源、文案风格明确

## 1. 环境（本机已具备，换机时照做）

```
游戏          E:\STEAM\steamapps\common\Probably Stolen   （1.0.0.15，Unity 2021.3.45，Mono）
源码工程      D:\Deepseek\mods-src\TapeArt
反编译源码    D:\Deepseek\decomp\ps
便携 .NET SDK D:\Deepseek\tools\dotnet8      （8.0.404）
ILSpy 命令行  D:\Deepseek\tools\ilspy-pkg\tools\net8.0\any\ilspycmd.dll
说明文档      D:\Deepseek\probably-stolen-modding\ModdingGuide.zh-CN.md（本地缓存）
```

构建必须设置这些环境变量，否则 SDK 会去写 `C:\Users\...\.dotnet` 或系统 TEMP 而被沙箱拒绝：

```powershell
$env:TEMP="D:\Deepseek\tools\tmp"; $env:TMP=$env:TEMP
$env:DOTNET_ROOT="D:\Deepseek\tools\dotnet8"
$env:DOTNET_CLI_HOME="D:\Deepseek\tools\dotnethome"
$env:APPDATA="D:\Deepseek\tools\appdata\Roaming"
$env:LOCALAPPDATA="D:\Deepseek\tools\appdata\Local"
$env:NUGET_PACKAGES="D:\Deepseek\tools\nugetcache"
& "$env:DOTNET_ROOT\dotnet.exe" build <proj>.csproj -c Release -v q
```

网络注意：命令行 HTTPS 常被 Schannel 拦（`SEC_E_NO_CREDENTIALS`），下载一律用 **Node**（自带 OpenSSL）：`node D:\Deepseek\tools\dl.js <url> <out>`。GitHub 域名在 hosts 里指向 127.0.0.1，但本地代理会转发，Node 里 `NODE_TLS_REJECT_UNAUTHORIZED=0` 即可访问 API。

## 2. 反编译与定位原版实现

```powershell
& "$env:DOTNET_ROOT\dotnet.exe" "D:\Deepseek\tools\ilspy-pkg\tools\net8.0\any\ilspycmd.dll" `
    -p -o "D:\Deepseek\decomp\ps" "E:\...\Probably Stolen_Data\Managed\Assembly-CSharp.dll"
```

**永远先读同类型原版代码再写**：灯牌照 `ToolDirectory.CreateLuxurySign`，机器核心照 `VendingMachineHelper.VendingMachineCore`，商人人照 `StoreClientListSpec.CreateRetiredAquafarmer`，升级照 `NetworkUpgradeList.CreateAquaculture`。文案语气用运行时取样：`LocHelper.GetLocalizedNetworkUpgrade("network_<ID>_desc")`。

## 3. 工程骨架

`mods-src/<Mod>/` 下需要：`manifest.xml`、`*.cs`、`<Mod>.csproj`（`netstandard2.1`，引用游戏 `Managed` 里的 `Assembly-CSharp`、`UnityEngine*`、`0Harmony`、`TextMeshPro`，全部 `Private=false`）、`Localization/zh.csv`+`en.csv`、`Textures/Items/*.png`、`preview.png`。

清单必填 `<Assemblies>`、`<Capabilities>`（多数情况留空）；`<GameVersions>` 写 `1.0.0`（覆盖 1.0.0.x）。

安装到游戏需要提权（工作区之外）：

```
Mods\<ID>\{manifest.xml, <Mod>.dll, Localization, Textures}
```

写文件前确认游戏已退出，否则 DLL 被映射锁定（`user-mapped section open`）；必要时先 `Remove-Item` 再复制。

## 4. 必须避开的原版坑（都踩过）

| 坑 | 现象 | 正确做法 |
|---|---|---|
| `PlayerStore.Instance` 是懒加载 | 主菜单阶段访问 → `new PlayerStore()` 空引用，整个模组 Init 失败 | 人脉升级等只能在 `OnGameLoadedInit` / `OnNewGameStarted` 注册 |
| `GraphUtils.TryAcceptAll` 返回**没放进去的数量** | 用 `>0` 当成功 → 物品其实放好了却被销毁、修复永远失败 | `== 0 && item.parentInventory != null` |
| `MachineryPanelRow.GetStorage(i)` 用的是**含存储的面板的序号** | `coreBay` 永远 false | 用 `MachineryPanel.CollectStorage(list)` 直接向面板要 |
| 机器窗口追加面板 | 面板序号 ≠ 存储序号（控制/音量不产生存储） | 面板排末尾，存储按 role 找 |
| `AddDirectSellingItemToTable` 满仓会**销毁**物品 | 奖励凭空消失 | 用带预检查的 `TryAddDirectSellingItemToTable(string,...)` |
| 升级价格在创建那一局就复制进运行时条目 | 改代码后老存档仍是旧价 | postfix `NetworkUpgrade.GetCost()` 兜底 |
| 投影仪/售货机色条的标签用西文字体 | 改建的标签写中文 → 完全不显示 | 换用能显示中文的 TMP 字体与材质 |
| 奖励物品被塞进背包后玩家看不到 | 用户以为没发 | 原版做法是 `GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.backInvinvElement, item)` |
| `LocHelper` 键不存在会回退成 key | UI 显示 `item_xxx_name` | 自己包一层 `Loc.Get(key, 默认中文)` |
| 菜单期跑 `AddNetworkUpgrade` | 同上空引用 | 惰性注册一次，`upgradesRegistered` 标志 |

Harmony 只在原版没有钩子时用（加核心槽、吸引力叠加、日历色条、电子元件交互、刷客预测、价格兜底）。补丁目标全部用 `typeof(游戏类型)`，不要字符串拼名。

## 5. 验证循环

1. 让用户**在沙箱外**启动游戏（沙箱内启动会因 Steam 需要写注册表而以 exit code 53 退出）——这是本技能唯一无法自动完成的一步。
2. 读 `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log`，**必须用 UTF-8 解码**，否则中文全是乱码：
   ```powershell
   Get-Content $log -Encoding UTF8 | Select-String "\[你的模组名\]|Harmony:|audit: <id>"
   ```
3. 关键日志：`Harmony: N method(s) patched`（补丁命中）、`audit: <id> ... running=True caps=none`（加载成功）。
4. 开发期在模组里挂 F9 调试按钮（`ModHelper.AddDebugFunction`）+ Self Test，用 `#if <MOD>_DEBUG` 包起来，发布构建默认剔除。
5. 交付前跑 `mod-scan <id>`（F8）确认能力声明与扫描结果一致。

## 6. 发布

**Steam 创意工坊**（steamcmd 在 `D:\Deepseek\tools\steamcmd`）：

```powershell
& .\steamcmd.exe +set_steam_guard_code <5位码> +login <user> `
    +workshop_build_item D:\Deepseek\release\workshop\<id>.vdf +quit
```

- VDF 里 `publishedfileid` 首次填 `0`，成功后 steamcmd 自动回写；之后同一命令就是更新。
- `visibility`：`0` 公开、`1` 好友、`2` 隐藏（先发隐藏自测更稳）。
- 描述里**不要用 `\n` 转义**，KeyValues 支持引号内真实换行，否则页面出现一堆 `\n`。
- 需要手机 Steam Guard 验证码；推送确认常收不到，直接用 App 里的 5 位码 + `+set_steam_guard_code` 最可靠。

**GitHub**（无 git 也可，用 REST API + Node）：

`node D:\Deepseek\tools\gh_push.js <token> <repo> <dir> <desc>` 建库并提交；`gh_release.js` 补 About/topics、加英文 README、发 Release 并上传 zip。空仓库要先 PUT 一个 README 做初始 commit，否则建 blob 报 409。

提醒用户：PAT 用完应撤销；密码只用于 steamcmd 登录，不写入任何文件。

## 7. 交付物清单

- `mods-src/<Mod>/`：源码 + CSV + 贴图 + `tools/make_art.py`
- `release/<mod>_v<ver>.zip`：可直接解压进 `Mods/` 的包
- `release/workshop/<id>.vdf`：发布配置（含 publishedfileid）
- `release/PUBLISH.md`：物品 ID、版本历史、更新流程
- 仓库内 `README.md` / `README.en.md` / `docs/{DESIGN,VERIFY,WORKSHOP,PUBLISH}.md`
