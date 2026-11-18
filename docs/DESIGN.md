# 磁带艺术 (tape_art) — 调研结论与实施方案

环境：游戏 `E:\STEAM\steamapps\common\Probably Stolen`，版本 **1.0.0.15**（Unity 2021.3.45，Mono）。
工程：`D:\Deepseek\mods-src\TapeArt`（netstandard2.1）。产物安装到 `Mods\tape_art\`。

工具链（本机无外网 HTTPS，仅可经 Node 的 TLS 与 harness fetch）：
- 便携 SDK：`D:\Deepseek\tools\dotnet8`（build 前需设 `DOTNET_ROOT/APPDATA/LOCALAPPDATA/NUGET_PACKAGES` 指向工作区）
- 反编译：`dotnet D:\Deepseek\tools\ilspy-pkg\tools\net8.0\any\ilspycmd.dll`，游戏源码已导出到 `D:\Deepseek\decomp\ps`

## 关键游戏内部事实（已核对源码）

| 主题 | 结论 |
|---|---|
| 磁带物品 | `MiscItemDirectory.MusicCassette(trackID, name, sprite, composer, state)`；`cassette1..14` + 后缀 `l`（损坏）/`d`（严重损坏）。state 0=完好 150cr，类型 `ART`+`LUXURY_ITEM`；state 1=损坏 70cr，仅 `ART`；state 2=严重 35cr，仅 `ART` |
| 磁带标签 | `CASSETTE_TAG`、`CASSETTE_CONTENT`(trackId) |
| 磁带播放机 | `FurnitureItemDirectory` → `MachineCassettePlayer.CassettePlayer()`；窗口 4 个面板：0 storage(6x6)、1 cassette(6x6)、2 controls、3 volume；磁带 2x2 格 → 可放 9 个。实例：`EmporiumEntry.Instance.cassettePlayer` |
| 灯牌 | `ToolDirectory` 原版：`ShowcaseHelper.InitShowcaseSpecialItem(item, "LUXURY_ITEM")` + `IMPORTANT_TAG`，放**展示柜**生效。刷客由 `ShowcaseHelper.GetDisplayCasePrediction()` 硬编码灯牌 id → 需 postfix 追加 |
| 吸引力 | `PlayerStore.baseStoreAttractiveness=250`；`GetCurrentStoreAttractiveness() = base - trashPenalty + projectorAttractivenessBonus`；幻影投影仪每次循环把 bonus 设为 `100+品质/性能加成` |
| 人脉升级 | `NetworkUpgradeList.CreateAquaculture()` 模式；onUnlock 里 `PlayerStore.Instance.QueueFuturClient("retired_aquafarmer", 1)`；周常出现逻辑在 `StoreClientManager`（`dayCounter % 7 == 2`） |
| 商人 | `StoreClientListDict.storeClientDict` 是 **public static** `Dictionary<string, Func<StoreClient>>`，可注册新商人；`StoreClient` 字段：identifier/displayName/spriteName/clientFaction/clientIntent/`clientItemFeatureBuying`/`mainDialogue.SetText(...).SetEndAction(...)`，进货用 `PlayerStore.Instance.AddDirectSellingItemToTable(item)` |
| 开局词条 | 原版已有 **`tapehead`（磁带迷）**：`StartingPerk.IsTapehead()`，`StartingPerkContent.AddTapeheadCassettes()` 开局给 cassette1..7（SetShabby） |
| 买单加成 | `clientItemFeatureBuying.Add("premiumBuy15")` 等 → 按 % 加价；100% 回收需自定义 feature 或自定义 UI |
| 机器供电 | `MachineryHelper.GetMachinePowerUsage(item)` 读 tag `machinery_drawn_power`；`PowerHelper.DrawPowerSource`；`MachineHelper.SetupBatterySlot` / `MachineHelper.PowerPanel` |
| 芯片槽 | `new GameGridInventory(shape, width)` + `MachineHelper.Panel(label, grid, role)`；装上后 `ModuleHelper.ComputeModuleEffect/ApplyBasicModuleEffect`，并 `MachineryHelper.UpdatePowerUsageProcessTypeMachine(machine)` |
| 电子元件（普通） | item id `common_electronic`（`MaterialDirectory.Electronic()`） |

## 与原版实现的对应关系（改动时照这些文件抄）

| 本模组实现 | 参照的原版代码 | 关键点 |
|---|---|---|
| `Items.CreateSign` | `ToolDirectory.CreateLuxurySign()` | `IMPORTANT_TAG` + `ShowcaseHelper.InitShowcaseSpecialItem(item, 类型)` + `TypeHelper.GetTypeDisplayName` |
| `Items.CreateCore` | `VendingMachineHelper.VendingMachineCore()` | 极简：只有名字和图标，不带标签/类型 |
| `Core.InstallPanels` | `MachineCassettePlayer.CreateMachineInventoryWindow()` / `MachineProjector.CreateMachineInventoryWindow()` | 追加面板排在末尾；存储索引用 `MachineryPanel.CollectStorage` |
| `Core.InstallCore` | `VendingMachineHelper.UnlockVendingMachine()` | `ItemSpawner.Spawn` → `SetParentItem` → 临时放开拆卸 → `TryAcceptAll(物品, 子物品)` → 恢复 |
| `Merchant.Create` | `StoreClientListSpec.CreateRetiredAquafarmer()` | `new StoreClient()` → 设 identifier/displayName/sprite/faction/intent → `mainDialogue.SetText(...).SetEndAction(上架)` → `AddBasicDialog()` → `CompleteClientCreation()` |
| `Merchant.Stock` | 同上（`SetEndAction` 里逐件 `AddDirectSellingItemToTable`） | 上架商品不标记拥有 |
| `PatchElectronic`（升级/修复入口） | `MaterialDirectory.Electronic()` 里的 `mayThisTargetItemFunc` / `onThisTargetItemFunc` | 原版把各家升级助手串在这个委托里；模组只能用 Harmony 包一层 |
| `PatchAttractiveness` | `MachineProjector` 每次循环写 `PlayerStore.projectorAttractivenessBonus` | 我们不改字段，改为 postfix `GetCurrentStoreAttractiveness()` 叠加，避免和投影仪互相覆盖 |
| `PatchSignPrediction` | `ShowcaseHelper.GetDisplayCasePrediction()` 里按灯牌 id 硬编码 | postfix 追加一条 `(clientId, chance, 灯牌id)` |
| `Core.NightlyPower` | 无直接对应（原版机器靠电池） | 用 `OnHandlingNightlyServicesLate` + `ModCash(-usage, playSound:false)`，按天去重 |

## 与原始需求的差异（已确认）

1. 词条【磁带迷】原版已存在（`tapehead`，开局送 7 盘旧磁带）。方案：**不新建重复词条**，在 `OnNewGameStarted` 检测该词条 → 追发送灯牌 + 解锁磁带艺术Ⅰ。
2. 游戏里 state 0 那一档（原版本地化用词为「完好品质」，额外带 `LUXURY_ITEM`）就是吸引力计算所依据的品质；本文档统一称「完好品质」。
3. 商人按**游戏正常价格**、**只收购完好品质磁带**：`clientIntent = SELLNBUY` + `clientBuyingIdList = cassette1..14`（用精确 id 而不是 `CASSETTE_TAG`，否则损坏(l)/严重损坏(d) 也会被收）。收购完全走原版议价流程——玩家把磁带摆到**柜台**上他才会评估，不会翻背包。
4. 电费**直接扣信用点**，不装电池（用户明确要求，与原版售货机的电池方案不同）。

## 分阶段实施

- P0 骨架 + 日志（已完成，待进游戏验证）
- P1 艺术品灯牌：物品注册（`OnModItemDirectoryInit`）+ 淡紫磁带霓虹 PNG + 展示柜类型 `ART` + `GetDisplayCasePrediction` postfix 拉磁带卖家
- P2 人脉升级三级：`AddNetworkUpgrade` ×3（225/270/315，基础页、供货商之后），Ⅰ 送 2 盘完好磁带 + 次日商人 + 周常，Ⅱ 核心芯片 + 吸引力 + 耗电，Ⅲ 修复
- P3 商人：注册 `storeClientDict` 蓝图，固定 5 盘随机磁带 + 灯牌（已拥有则跳过），对话按钮回收艺术品
- P4 播放机改造：末尾追加电池面板 + 芯片面板（保持面板顺序兼容存档），核心物品 `tape_art_core`（芯片，`MODULE_TAG`），可播放区统计 ×20 吸引力，夜间扣电 40/天，可用 `common_electronic` 升级 −4/个（最低 20）
- P5 修复：`common_electronic` 拖到储藏区磁带上 → 转为同曲目 state 0

## 进度

- **v0.12.0（已安装）**：核心安装改为完全照 `VendingMachineHelper.UnlockVendingMachine` 的流程（`ItemSpawner.Spawn` → `SetParentItem` → 临时放开拆卸 → `TryAcceptAll` → 恢复）；灯牌描述改用 `TypeHelper.GetTypeDisplayName("ART")`；核心物品去掉 `MODULE_TAG`/`MODULE` 类型，与售货机核心一致。
- **v0.11.1（关键修复）**：`GraphUtils.TryAcceptAll` 返回的是「没放进去的数量」，成功为 `0`；此前用 `> 0` 判断导致核心被误判失败并销毁、Ⅲ 修复永不生效。已改为 `== 0 && parentInventory != null`。
- **v0.11.0**：Ⅰ 赠品改为「成功后才置位」并换用新标记键 `tier1_granted_v2`，老存档可重新发放；发放过程逐件写日志。
- **v0.8.2**：修掉 `core bay = False` 根因（存储序号 ≠ 面板序号），改按 `MachineryPanel.CollectStorage` 取存储。
- **v0.8.0**：核心改为解锁Ⅱ即装、不可拆；灯牌加淡紫霓虹外框并放大到 2×2。
- **v0.5.2**：商人上架改为 **5 盘随机品质**（完好 50% / 损坏 `l` 30% / 严重损坏 `d` 20%），对齐原始设定；补齐安装/上架/升级/修复的日志埋点；Ⅰ 的赠品改为「内存标记 + 存档标记」双保险，并在读档时补发漏发的一次。
- **已由日志确认（v0.4.1 实机）**：`Loading: TapeArt.dll`、`Harmony: 4 method(s) patched`、`2 item sprite(s) loaded`、`network upgrades registered`、`tapehead perk: art sign granted, TAPE_ART_I unlocked`、`audit ... running=True caps=none`，全程无 Error。
- **TODO**：①游戏内确认 v0.6.0（面板、商人、吸引力、耗电、修复）；②本地化 CSV；③商人收购价与提示文案打磨。

## 踩过的坑（重要）

- **`GraphUtils.TryAcceptAll(x, item)` 返回的是「没放进去的数量」，成功是 `0`**。写成 `> 0` 当成功会得到完全相反的结论：核心其实装进了槽，却被判定失败并销毁、再补发到背包（表现为「解锁Ⅱ后在背包里拿到一个芯片」）；Ⅲ 的修复也同样永远失败。正确写法：`== 0 && item.parentInventory != null`。排查时优先核对每个 API 的返回值语义，而不是只看日志措辞。
- **主菜单阶段绝对不能碰 `PlayerStore.Instance`**：它是懒加载 getter，null 时会 `new PlayerStore()`，而构造函数依赖场景对象，在主菜单必然 `NullReferenceException`。`ModHelper.AddNetworkUpgrade` → `NetworkUpgrade.RegisterModUpgrade` 内部就会读这个属性，所以**人脉升级不能在 `OnEnable` 里注册**（指南写的是 Init/OnEnable，实测 1.0.0.15 会崩）。现改为 `OnGameLoadedInit` / `OnNewGameStarted` 里惰性注册一次。
- 同理 `ModHelper.IsNetworkUpgradeUnlocked` 也会触发该 getter，菜单期调用要包 try/catch。
- 游戏运行中无法覆盖 `Mods\` 里的 DLL（file mapped），安装前必须确认游戏已退出。

## 待验证

- 骨架能否被加载（F8 控制台应出现 `[磁带艺术 | Tape Art]: Init ...`）
- 灯牌 PNG 的网格尺寸（`SetSpriteAndShapeFromMod` 按不透明像素推形状；原版灯牌占 1 格?）
- 玩家存档兼容性（新增面板必须排在末尾）
