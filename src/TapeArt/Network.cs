using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeArt
{
    /// <summary>王尔德人脉：磁带艺术 Ⅰ/Ⅱ/Ⅲ。</summary>
    internal static class Network
    {
        public const int Cost2 = 540;

        private const string Tab = null; // null = 「基础」页，注册顺序排在供货商之后
        private const string Card = "Tape Art";

        /// <summary>
        /// 升级图标优先直接用游戏里磁带的物品精灵图（图集/名称），取不到才退回模组自带的芯片图。
        /// CustomUIManager.GetSprite 支持 "图集/名称"、游戏 UI 精灵键和 "modId:key" 三种写法。
        /// </summary>
        private static string PickIconKey()
        {
            string[] candidates =
            {
                "items_amenities/cassette5",
                "items_amenities/cassette1",
                "Items/items_amenities/cassette5"
            };
            try
            {
                CustomUIManager ui = CustomUIManager.Instance;
                if (ui != null)
                {
                    foreach (string key in candidates)
                    {
                        if (ui.GetSprite(key, quiet: true) != null)
                        {
                            return key;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return "tape_art:tape_art_icon";
        }

        public static void Register()
        {
            string icon = PickIconKey();
            ModHelper.AddNetworkUpgrade(
                Ids.ModId, Ids.Upgrade1, 225, "Tape Art",
                () => Loc.Get("up1_title", "磁带艺术Ⅰ"),
                () => Loc.Get("up1_desc", "王尔德会为你引荐一位磁带迷商人，对方会每周到访一次，出售损坏的磁带和艺术品灯牌，并收购完好品质的磁带。立即获得两盘完好磁带，同时可以用电子元件（普通）修复磁带播放机里的损坏磁带。（解锁磁带艺术内容。）"),
                OnUnlockOne,
                tab: Tab, iconKey: icon,
                subtitleLabel: () => Loc.Get("card_tape_art", "磁带艺术"));

            ModHelper.AddNetworkUpgrade(
                Ids.ModId, Ids.Upgrade2, Cost2, "Tape Art",
                () => Loc.Get("up2_title", "磁带艺术Ⅱ"),
                () => Loc.Get("up2_desc", "磁带播放机会装上一枚核心，播放区里的磁带会让店铺更吸引顾客，每晚则要为此支付电费——费用可以用电子元件降低。"),
                OnUnlockTwo,
                tab: Tab, iconKey: icon,
                subtitleLabel: () => Loc.Get("card_tape_art", "磁带艺术"), prerequisites: new[] { Ids.Upgrade1 });
        }

        public static bool Unlocked(int tier)
        {
            string id = tier <= 1 ? Ids.Upgrade1 : Ids.Upgrade2;
            try
            {
                return ModHelper.IsNetworkUpgradeUnlocked(id);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void OnUnlockOne()
        {
            GrantOnTransition();
        }

        /// <summary>Ⅰ 的购买钩子（onUnlock 之后触发），同样只在「本轮新解锁」时发放。</summary>
        public static void OnTierOneUnlocked()
        {
            GrantOnTransition();
        }

        /// <summary>本局是否已经发过 Ⅰ 的赠品（内存态，不写存档）。</summary>
        private static bool tierOneGrantedThisRun;

        /// <summary>新开一局时调用，清空内存态。</summary>
        public static void BeginNewRun()
        {
            tierOneGrantedThisRun = false;
        }

        /// <summary>
        /// Ⅰ 的赠品：只在「本局由未解锁变成已解锁」时发一次。
        /// 读档时把基线设为当前解锁状态，因此读档本身不会补发，也不会出现存档脏标记。
        /// </summary>
        private static void GrantOnTransition()
        {
            if (tierOneGrantedThisRun || !Unlocked(1))
            {
                return;
            }

            int placed = GiveRandomPristine(2);
            if (placed < 2)
            {
                TapeArtMod.Log?.Error("tier I reward incomplete: placed " + placed + "/2");
                return;
            }

            tierOneGrantedThisRun = true;
            QueueMerchant(1);
        }

        /// <summary>调试用：允许再发一次 Ⅰ 的赠品。</summary>
        public static bool ResetTierOneReward()
        {
            tierOneGrantedThisRun = false;
            return true;
        }

        /// <summary>
        /// 旧存档里的升级条目在存档创建时就复制了价格，改代码不会影响已存在的这一局，
        /// 所以每次读档时把Ⅱ的价格强制改到当前设定（已购买的跳过）。
        /// </summary>
        public static void ApplyRuntimeCosts()
        {
            try
            {
                if (Unlocked(2))
                {
                    return;
                }
                NetworkUpgrade upgrade = ModHelper.GetNetworkUpgrade(Ids.Upgrade2);
                if (upgrade != null && upgrade.cost != Cost2)
                {
                    upgrade.cost = Cost2;
                }
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("apply costs failed: " + e);
            }
        }
        /// <summary>读档时调用：记录基线（已解锁视为发生过），并确保Ⅱ的核心在播放机里。</summary>
        public static void OnRunLoaded()
        {
            ApplyRuntimeCosts();
            tierOneGrantedThisRun = Unlocked(1);

            if (Unlocked(2))
            {
                Core.EnsurePanels();
                Core.InstallCore();
            }
        }
        private static void OnUnlockTwo()
        {
            // Ⅱ 解锁：先给播放机加上核心槽（此前窗口与原版完全一致），再把核心装进去，装好后不可拆。
            Core.EnsurePanels();
            Core.InstallCore();
        }


        /// <summary>发放 n 盘完好磁带，返回真正放进背包（或柜台）的数量。</summary>
        public static int GiveRandomPristine(int count)
        {
            if (EmporiumEntry.Instance == null)
            {
                TapeArtMod.Log?.Error("give tapes: store not loaded");
                return 0;
            }

            List<string> pool = new List<string>();
            foreach (string id in Ids.PristineCassettes)
            {
                if (!Items.PlayerOwns(id))
                {
                    pool.Add(id);
                }
            }
            if (pool.Count == 0)
            {
                pool.AddRange(Ids.PristineCassettes);
            }

            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                if (pool.Count == 0)
                {
                    pool.AddRange(Ids.PristineCassettes);
                }
                string picked = pool[UnityEngine.Random.Range(0, pool.Count)];
                pool.Remove(picked);

                // 照 NetworkUpgradeList 里送种植设备的写法：放进后柜台（工作台），
                // 玩家在店里一眼就能看到，而不是塞进背包。
                GameItem item = ModHelper.SpawnItem(picked);
                if (item == null)
                {
                    TapeArtMod.Log?.Error("give tapes: spawn failed for " + picked);
                    continue;
                }
                GeneralHelper.SetItemOwned(item);
                GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.backInvinvElement, item);
                bool onCounter = item.parentInventory != null;

                if (!onCounter)
                {
                    ModHelper.MoveItemToInventory(item);
                }
                if (item.parentInventory == null)
                {
                    TapeArtMod.Log?.Error("give tapes: no room for " + picked);
                    item.Destroy();
                    continue;
                }

                placed++;
            }
            return placed;
        }

        public static void QueueMerchant(int dayOffset)
        {
            if (PlayerStore.Instance == null)
            {
                return;
            }
            PlayerStore.Instance.QueueFuturClient(Ids.Merchant, dayOffset);
            ModHelper.SetModDataInt(Ids.ModId, Ids.DataLastVisitDay, CurrentDay());
        }

        public static int CurrentDay()
        {
            try
            {
                return StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 每早检查：补齐没装上的核心；按原版「退休供应商」的节奏排商人
        /// （解锁后的第二天必到，此后每周固定一天，可被「今日不营业」挡掉）。
        /// </summary>
        public static void OnMorning(ModLog log)
        {
            if (PlayerStore.Instance == null)
            {
                return;
            }

            // 上次解锁Ⅱ时若没能把核心装进槽（例如那一刻窗口还没校验），每天补试一次。
            Core.RetryInstall();

            if (!Unlocked(1))
            {
                return;
            }

            int day = CurrentDay();
            // 与水培农场主同日：dayCounter % 7 == 2
            if (day % 7 != 2)
            {
                return;
            }
            if (OutOfStockSignHelper.IsTurnedAway(Ids.Merchant))
            {
                log.Log("Tape-hoarder merchant skipped (turned away today)");
                return;
            }

            QueueMerchant(0);
            log.Log("Tape-hoarder merchant queued for day " + day);
        }
    }
}
