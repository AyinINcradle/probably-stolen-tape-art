using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeArt
{
    /// <summary>磁带播放机核心：芯片槽、吸引力、夜间供电、元件升级与磁带修复。</summary>
    internal static class Core
    {
        public const string LevelTag = "TAPE_CORE_LEVEL_INT";
        public const string PoweredKey = "core_powered";
        public const string CoreRole = "tape_art_core";

        /// <summary>原版 4 个面板（储藏/磁带/控制/音量）之后追加核心槽。
        /// 注意：存储区索引只统计「含存储的面板」，控制/音量面板不占索引，所以核心=2。</summary>
        public const int ModulePanel = 4;
        public const int ModuleStorageIndex = 2;

        public const int BaseUsage = 40;
        public const int UsagePerLevel = 4;
        public const int MinUsage = 20;
        public const int MaxLevel = 5;
        public const int AttractionPerCassette = 20;
        public const int MaxAttractionCassettes = 9; // 磁带 2x2 格，播放区 6x6

        public static GameItem Player => EmporiumEntry.Instance != null ? EmporiumEntry.Instance.cassettePlayer : null;

        private static MachineryPanelRow Row(GameItem machine)
        {
            return machine?.contentWindow?.childElement as MachineryPanelRow;
        }

        /// <summary>按面板 role 取它自己的存储。MachineryPanelRow.GetStorage 用的是「存储区序号」，
        /// 与面板序号不同（控制和音量面板不产生存储），所以这里直接问面板要。</summary>
        private static GraphNodeStorage StorageByRole(string role, int fallbackStorageIndex)
        {
            GameItem machine = Player;
            MachineryPanelRow row = Row(machine);
            if (row != null)
            {
                MachineryPanel panel = row.GetPanel(role);
                if (panel != null)
                {
                    List<GraphNodeStorage> list = new List<GraphNodeStorage>();
                    panel.CollectStorage(list);
                    if (list.Count > 0)
                    {
                        return list[0];
                    }
                }
            }
            return machine == null ? null : MachineHelper.GetBodyStorage(machine, fallbackStorageIndex);
        }

        public static GameInventory ModuleGrid => StorageByRole(CoreRole, ModuleStorageIndex) as GameInventory;

        public static GameInventory CassetteGrid => StorageByRole("cassette", 1) as GameInventory;

        public static GameInventory StorageGrid => StorageByRole("storage", 0) as GameInventory;

        /// <summary>自检用：列出当前播放机窗口的面板角色。</summary>
        public static string DescribePanels()
        {
            GameItem machine = Player;
            if (machine == null)
            {
                return "cassette player missing";
            }
            MachineryPanelRow row = Row(machine);
            if (row == null)
            {
                return "window is not a machinery panel row";
            }

            List<string> names = new List<string>();
            foreach (MachineryPanel panel in row.Panels)
            {
                names.Add(panel?.role ?? "?");
            }
            return string.Join(",", names.ToArray());
        }

        /// <summary>解锁Ⅱ时把核心直接装进播放机专用槽（装上后不可拆，和自动售货机核心一致）。</summary>
        public static bool InstallCore()
        {
            GameItem machine = Player;
            GameInventory grid = ModuleGrid;
            if (machine == null || grid == null)
            {
                TapeArtMod.Log?.Error("core install skipped: player=" + (machine != null) + " bay=" + (grid != null));
                GrantCoreFallback();
                return false;
            }
            if (FindCore() != null)
            {
                return true;
            }

            // 尽量照抄原版 VendingMachineHelper.UnlockVendingMachine：
            // ItemSpawner.Spawn → SetParentItem → 临时放开拆卸限制 → TryAcceptAll → 恢复限制。
            GameItem core = ItemSpawner.Spawn(Ids.Core);
            if (core == null)
            {
                TapeArtMod.Log?.Error("core install failed: item not created");
                GrantCoreFallback();
                return false;
            }
            GeneralHelper.SetParentItem(core, machine);
            GeneralHelper.SetItemOwned(core);

            // 第一次：完全照 VendingMachineHelper.UnlockVendingMachine（按物品放）。
            Func<GameItem, GameInventory, bool> savedRemove = grid.mayInventoryRemoveItemFunc;
            try
            {
                grid.mayInventoryRemoveItemFunc = (item, inventory) => true;
                int leftover = GraphUtils.TryAcceptAll(machine, core);
                if (leftover == 0 && core.parentInventory != null)
                {
                    return true;
                }
                TapeArtMod.Log?.Error("core install: item path rejected (leftover " + leftover + ")");
            }
            finally
            {
                grid.mayInventoryRemoveItemFunc = savedRemove;
            }

            if (core.parentInventory != null)
            {
                return true;
            }

            // 第二次：直接放进槽，临时关掉「只收核心」的过滤器再试一次。
            Func<GameItem, GameInventory, bool> savedAdd = grid.mayInventoryAddItemFunc;
            try
            {
                grid.mayInventoryAddItemFunc = null;
                int leftover2 = GraphUtils.TryAcceptAll(grid, core);
                if (leftover2 == 0 && core.parentInventory != null)
                {
                    return true;
                }
                TapeArtMod.Log?.Error("core install: direct path rejected (leftover " + leftover2 + ")");
            }
            finally
            {
                grid.mayInventoryAddItemFunc = savedAdd;
            }

            if (core.parentInventory != null)
            {
                return true;
            }

            core.Destroy();
            TapeArtMod.Log?.Error("core install failed: slot rejected the item");
            GrantCoreFallback();
            return false;
        }

        /// <summary>装不进槽时把核心放到柜台上（看得见），柜台也放不下才进背包。</summary>
        private static void GrantCoreFallback()
        {
            try
            {
                GameItem core = ModHelper.SpawnItem(Ids.Core);
                if (core == null)
                {
                    TapeArtMod.Log?.Error("core fallback: item not created");
                    return;
                }

                GeneralHelper.SetItemOwned(core);
                bool onCounter = false;
                try
                {
                    // 与升级奖励一致：放到后柜台
                    GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.backInvinvElement, core);
                    onCounter = core.parentInventory != null;
                }
                catch (Exception)
                {
                }

                if (!onCounter)
                {
                    ModHelper.MoveItemToInventory(core);
                }
                if (core.parentInventory == null)
                {
                    TapeArtMod.Log?.Error("core fallback: no room anywhere, item destroyed");
                    core.Destroy();
                    return;
                }

            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("core fallback failed: " + e);
            }
        }

        /// <summary>每早重试一次：解锁Ⅱ但槽里没有核心（上次装失败）时补装。</summary>
        public static void RetryInstall()
        {
            try
            {
                if (Network.Unlocked(2) && FindCore() == null)
                {
                    InstallCore();
                }
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("core retry failed: " + e);
            }
        }

        public static GameItem FindCore()
        {            GameInventory grid = ModuleGrid;
            if (grid == null)
            {
                return null;
            }
            foreach (GameItem child in grid.childItems)
            {
                if (child != null && child.identifier == Ids.Core)
                {
                    return child;
                }
            }
            return null;
        }

        public static int Level(GameItem core)
        {
            return core == null ? 0 : core.GetTagReadonly(LevelTag).GetInt();
        }

        public static int Usage()
        {
            int level = Level(FindCore());
            return Mathf.Max(MinUsage, BaseUsage - UsagePerLevel * level);
        }

        public static bool IsPowered()
        {
            try
            {
                return ModHelper.GetModDataBool(Ids.ModId, PoweredKey);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void SetPowered(bool value)
        {
            ModHelper.SetModDataBool(Ids.ModId, PoweredKey, value);
        }

        /// <summary>现金够不够付今晚的电费（当晚结算之前的即时判断）。</summary>
        public static bool CanAffordPower()
        {
            try
            {
                PlayerStore store = PlayerStore.Instance;
                return store != null && store.playerCash >= Usage();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>确认Ⅱ已解锁时才给玩家机器补上核心槽（未解锁时窗口与原版完全一致）。</summary>
        public static void EnsurePanels()
        {
            GameItem machine = Player;
            if (machine != null)
            {
                InstallPanels(machine);
            }
        }

        /// <summary>在磁带播放机窗口末尾追加核心槽（只加一次）。</summary>
        public static void InstallPanels(GameItem machine)
        {
            if (machine == null || machine.contentWindow == null)
            {
                return;
            }
            if (!(machine.contentWindow.childElement is MachineryPanelRow row) || row.GetPanel(CoreRole) != null)
            {
                return;
            }

            // 原版核心贴图是 2x2 格，槽必须至少 2x2，否则 GraphUtils 永远返回 leftover 1。
            GameGridInventory modules = new GameGridInventory(2, 2);
            // 专用核心槽：只收磁带播放机核心，且装上后拆不下来（与自动售货机核心一致）。
            modules.mayInventoryAddItemFunc = (item, inventory) => item != null && item.identifier == Ids.Core;
            modules.mayInventoryRemoveItemFunc = (item, inventory) => false;
            row.Add(MachineHelper.Panel("核心", modules, CoreRole));

        }

        /// <summary>播放区里每盘完好磁带 +20 吸引力，未装核心或没通电时为 0。</summary>
        public static int AttractionBonus()
        {
            if (!Network.Unlocked(2) || FindCore() == null)
            {
                return 0;
            }
            // 当晚结算之前也要生效：只要现金付得起今晚的电费，就先按通电算。
            if (!IsPowered() && !CanAffordPower())
            {
                return 0;
            }

            GameInventory grid = CassetteGrid;
            if (grid == null)
            {
                return 0;
            }

            int count = 0;
            foreach (GameItem child in grid.childItems)
            {
                if (child == null || !child.IsTag("CASSETTE_TAG"))
                {
                    continue;
                }
                if (Array.IndexOf(Ids.PristineCassettes, child.identifier) >= 0)
                {
                    count++;
                }
            }

            return Mathf.Min(count, MaxAttractionCassettes) * AttractionPerCassette;
        }

        /// <summary>每晚直接从现金里扣电费（不装电池）；付不起就断电，吸引力归零。</summary>
        public static void NightlyPower(ModLog log)
        {
            try
            {
                if (!Network.Unlocked(2) || FindCore() == null)
                {
                    SetPowered(false);
                    return;
                }

                int usage = Usage();
                int day = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
                int chargedDay;
                try
                {
                    chargedDay = ModHelper.GetModDataInt(Ids.ModId, "power_day", -1);
                }
                catch (Exception)
                {
                    chargedDay = -1;
                }
                if (chargedDay == day)
                {
                    return; // 今晚已经扣过，夜间服务重复触发时不再扣钱
                }

                PlayerStore store = PlayerStore.Instance;
                bool ok = store != null && store.playerCash >= usage;
                if (ok)
                {
                    store.ModCash(-usage, playSound: false);
                }
                try
                {
                    ModHelper.SetModDataInt(Ids.ModId, "power_day", day);
                }
                catch (Exception)
                {
                }
                SetPowered(ok);
                log.Log("cassette core: " + (ok ? "powered" : "no power") + " (" + usage + " credits/night)");
            }
            catch (Exception e)
            {
                SetPowered(false);
                log.Error("nightly power failed: " + e);
            }
        }

        // ---------- 电子元件（普通）交互 ----------

        public static bool CanTarget(GameItem electronic, GameItem target)
        {
            if (electronic == null || target == null)
            {
                return false;
            }
            if (target.identifier == Ids.Core)
            {
                return Network.Unlocked(2) && Level(target) < MaxLevel;
            }
            return Network.Unlocked(1) && IsRepairableCassette(target);
        }

        public static void OnTarget(GameItem electronic, GameItem target)
        {
            if (target == null || electronic == null)
            {
                return;
            }
            if (target.identifier == Ids.Core)
            {
                if (Network.Unlocked(2))
                {
                    UpgradeCore(target, electronic);
                }
            }
            else if (Network.Unlocked(1) && IsRepairableCassette(target))
            {
                // 双保险：CanTarget 已经判过解锁，这里再判一次，
                // 避免以后有人直接调用 OnTarget 绕过Ⅲ的解锁条件。
                Repair(target, electronic);
            }
        }

        private static void ConsumeOne(GameItem stack)
        {
            if (stack.unitCount > 1)
            {
                stack.unitCount--;
                return;
            }
            stack.Destroy();
        }

        private static void UpgradeCore(GameItem core, GameItem electronic)
        {
            int level = Level(core);
            if (level >= MaxLevel)
            {
                return;
            }
            int next = level + 1;
            core.ModifyTag(LevelTag, state => state.Enable().SetInt(next));
            ConsumeOne(electronic);
        }

        /// <summary>储藏区（storage 面板）里的损坏/严重损坏磁带才算可修复。</summary>
        public static bool IsRepairableCassette(GameItem item)
        {
            if (item == null || !item.IsTag("CASSETTE_TAG"))
            {
                return false;
            }
            if (PristineIdOf(item.identifier) == null)
            {
                return false;
            }
            GameInventory storage = StorageGrid;
            return storage != null && item.parentInventory == storage;
        }

        /// <summary>cassette3l / cassette8d → cassette3 / cassette8；已是完好则返回 null。</summary>
        public static string PristineIdOf(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 2)
            {
                return null;
            }
            char last = id[id.Length - 1];
            if (last != 'l' && last != 'd')
            {
                return null;
            }
            string baseId = id.Substring(0, id.Length - 1);
            return Array.IndexOf(Ids.PristineCassettes, baseId) >= 0 ? baseId : null;
        }

        private static void Repair(GameItem damaged, GameItem electronic)
        {
            if (!Network.Unlocked(1))
            {
                return;
            }
            string pristineId = PristineIdOf(damaged.identifier);
            if (pristineId == null)
            {
                return;
            }

            string damagedId = damaged.identifier;
            GameInventory storage = damaged.parentInventory;
            if (storage == null)
            {
                return;
            }

            // 先腾出位置：新磁带要占同一块 2x2 空间，旧的不移除会放不进去。
            damaged.Destroy();

            GameItem fresh = DirectoryMaster.Item(pristineId);
            if (fresh != null)
            {
                GeneralHelper.SetItemOwned(fresh);
                if (GraphUtils.TryAcceptAll(storage, fresh) == 0 && fresh.parentInventory != null)
                {
                    ConsumeOne(electronic);
                    return;
                }
                fresh.Destroy();
            }

            // 放不回去就把原来那盘还回来，别把玩家的磁带弄丢。
            GameItem back = DirectoryMaster.Item(damagedId);
            if (back != null)
            {
                GeneralHelper.SetItemOwned(back);
                GraphUtils.TryAcceptAll(storage, back);
            }
            TapeArtMod.Log?.Error("repair failed for " + damagedId + ", cassette restored");
        }
    }
}
