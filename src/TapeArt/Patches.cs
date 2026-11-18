using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TapeArt
{
    /// <summary>给磁带播放机追加电池槽与芯片槽。</summary>
    [HarmonyPatch(typeof(MachineCassettePlayer), nameof(MachineCassettePlayer.CassettePlayer))]
    internal static class PatchCassettePlayerPanels
    {
        private static void Postfix(GameItem __result)
        {
            try
            {
                // 未解锁Ⅱ时保持原版模样，解锁后才多出核心槽。
                if (Network.Unlocked(2))
                {
                    Core.InstallPanels(__result);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[TapeArt] panel install failed: " + e);
            }
        }
    }

    /// <summary>吸引力：核心通电时，播放区每盘完好磁带 +20。</summary>
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.GetCurrentStoreAttractiveness))]
    internal static class PatchAttractiveness
    {
        private static void Postfix(ref int __result)
        {
            try
            {
                __result += Core.AttractionBonus();
            }
            catch (Exception)
            {
                // 主菜单阶段没有商店，忽略
            }
        }
    }

    /// <summary>让电子元件（普通）可以升级核心、修复储藏区的磁带。</summary>
    [HarmonyPatch(typeof(MaterialDirectory), "Electronic")]
    internal static class PatchElectronic
    {
        private static void Postfix(GameItem __result)
        {
            if (__result == null)
            {
                return;
            }
            try
            {
                Func<GameItem, GameItem, bool> origMay = __result.mayThisTargetItemFunc;
                Func<GameItem, GameItem, bool> origCan = __result.canThisTargetItemFunc;
                Action<GameItem, GameItem> origOn = __result.onThisTargetItemFunc;

                __result.mayThisTargetItemFunc = (s, t) =>
                    (origMay != null && origMay(s, t)) || Core.CanTarget(s, t);
                __result.canThisTargetItemFunc = (s, t) =>
                    (origCan != null && origCan(s, t)) || Core.CanTarget(s, t);
                __result.onThisTargetItemFunc = (s, t) =>
                {
                    origOn?.Invoke(s, t);
                    if (Core.CanTarget(s, t))
                    {
                        Core.OnTarget(s, t);
                    }
                };
            }
            catch (Exception e)
            {
                Debug.LogError("[TapeArt] electronic patch failed: " + e);
            }
        }
    }

    /// <summary>
    /// 艺术品灯牌（展示柜或悬挂广告牌）会招来磁带发烧友。
    /// 用 ShowcaseHelper.IsShowcaseContainItemById 判断：该接口会被 Proper Signboards 之类的
    /// 模组改写成「展示柜 + 悬挂广告牌」的合并视图，直接调用即可自动兼容。
    /// </summary>
    [HarmonyPatch(typeof(ShowcaseHelper), nameof(ShowcaseHelper.GetDisplayCasePrediction))]
    internal static class PatchSignPrediction
    {
        internal const int Chance = 50;

        private static void Postfix(ref List<(string, int, string)> __result)
        {
            try
            {
                if (__result == null || EmporiumEntry.Instance == null)
                {
                    return;
                }
                if (__result.Any(entry => entry.Item1 == Ids.Enthusiast))
                {
                    return;
                }
                if (SignDisplayed())
                {
                    __result.Add((Ids.Enthusiast, Chance, Ids.Sign));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[TapeArt] sign prediction patch failed: " + e);
            }
        }

        /// <summary>灯牌是否摆在可招客的位置（兼容悬挂广告牌模组）。</summary>
        internal static bool SignDisplayed()
        {
            try
            {
                if (ShowcaseHelper.IsShowcaseContainItemById(Ids.Sign))
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }

            // 兜底：悬挂广告牌类模组可能把灯牌从展示柜移走，原版接口就读不到。
            // 只要玩家拥有灯牌、且它不在背包里，就认为已经摆在店里的某个展示位置。
            return OwnedSignPlaced();
        }

        private static bool OwnedSignPlaced()
        {
            if (EmporiumEntry.Instance == null)
            {
                return false;
            }
            foreach (GameItem item in EmporiumEntry.Instance.GetAllOwnedItems())
            {
                if (item == null || item.identifier != Ids.Sign)
                {
                    continue;
                }
                GameInventory parent = item.parentInventory;
                if (parent != null && parent != EmporiumEntry.Instance.invElement)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
