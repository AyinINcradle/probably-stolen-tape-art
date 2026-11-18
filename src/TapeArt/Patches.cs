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

    /// <summary>艺术品灯牌放进展示柜后，往当天的刷客预测里加入磁带迷商人。</summary>
    [HarmonyPatch(typeof(ShowcaseHelper), nameof(ShowcaseHelper.GetDisplayCasePrediction))]
    internal static class PatchSignPrediction
    {
        private const int Chance = 50;

        private static void Postfix(ref List<(string, int, string)> __result)
        {
            try
            {
                if (__result == null || EmporiumEntry.Instance == null || EmporiumEntry.Instance.showcaseElement == null)
                {
                    return;
                }
                if (__result.Any(entry => entry.Item1 == Ids.Merchant))
                {
                    return;
                }

                bool hasSign = false;
                foreach (GameItem child in EmporiumEntry.Instance.showcaseElement.childItems)
                {
                    if (child != null && child.identifier == Ids.Sign && GeneralHelper.IsItemOwned(child))
                    {
                        hasSign = true;
                        break;
                    }
                }
                if (hasSign)
                {
                    __result.Add((Ids.Merchant, Chance, Ids.Sign));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[TapeArt] sign prediction patch failed: " + e);
            }
        }
    }
}
