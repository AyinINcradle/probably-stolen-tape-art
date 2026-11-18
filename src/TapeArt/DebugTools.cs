#if TAPE_ART_DEBUG
using System;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 开发期的 F9 测试按钮。默认不编译进发布版；
    /// 需要调试时用 -p:DefineConstants=TAPE_ART_DEBUG 重新构建即可恢复。
    /// </summary>
    internal static class DebugTools
    {
        public static void Register()
        {
            try
            {
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPECIAL, "Tape Art: Self Test", SelfTest);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPAWN, "Give Art Sign", GiveSign);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPAWN, "Give 3 Pristine Tapes", GiveTapes);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_COMMAND, "Unlock Tape Art I-II", UnlockAll, tall: true);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_COMMAND, "Queue Tape Merchant", QueueMerchant);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_COMMAND, "Reinstall Core Now", ReinstallCore);
                ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_COMMAND, "Reset Tier I Reward", ResetTierOne);
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("debug functions failed: " + e);
            }
        }

        public static void Unregister()
        {
            try
            {
                ModHelper.RemoveDebugFunctions(Ids.ModId);
            }
            catch (Exception)
            {
            }
        }

        private static void SelfTest()
        {
            TapeArtMod.Log?.Log("self test: panels = [" + Core.DescribePanels() + "]");
            TapeArtMod.Log?.Log("self test: storage=" + (Core.StorageGrid != null)
                                + " cassette=" + (Core.CassetteGrid != null)
                                + " coreBay=" + (Core.ModuleGrid != null)
                                + " core=" + (Core.FindCore() != null));
            TapeArtMod.Log?.Log("self test: usage=" + Core.Usage() + "/night, powered=" + Core.IsPowered()
                                + ", attraction=+" + Core.AttractionBonus());
            TapeArtMod.Log?.Log("self test: I=" + Network.Unlocked(1) + " II=" + Network.Unlocked(2));
        }

        private static void GiveSign()
        {
            GameItem sign = ModHelper.SpawnItem(Ids.Sign);
            if (sign != null)
            {
                ModHelper.MoveItemToInventory(sign);
            }
        }

        private static void GiveTapes()
        {
            Network.GiveRandomPristine(3);
        }

        private static void UnlockAll()
        {
            foreach (string id in new[] { Ids.Upgrade1, Ids.Upgrade2 })
            {
                NetworkUpgrade upgrade = ModHelper.GetNetworkUpgrade(id);
                if (upgrade != null && !ModHelper.IsNetworkUpgradeUnlocked(id))
                {
                    upgrade.Unlock();
                }
            }
            Core.InstallCore();
        }

        private static void QueueMerchant()
        {
            Network.QueueMerchant(0);
        }

        private static void ReinstallCore()
        {
            bool ok = Core.InstallCore();
            TapeArtMod.Log?.Log("debug: reinstall core -> " + ok);
        }

        private static void ResetTierOne()
        {
            Network.ResetTierOneReward();
            int placed = Network.GiveRandomPristine(2);
            TapeArtMod.Log?.Log("debug: tier I reward re-granted, placed " + placed + "/2");
        }
    }
}
#endif
