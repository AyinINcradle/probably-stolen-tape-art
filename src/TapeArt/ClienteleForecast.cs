using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 「顾客群体」界面：原版按 ShowcaseHelper.GetDisplayCasePrediction() 逐条渲染。
    /// 装了「更合理的广告牌」这类会改写展示柜来源的模组后，我们这条预测可能被过滤掉，
    /// 所以这里做一次兜底：如果列表里没有磁带迷商人，就自己补一行。
    /// </summary>
    [HarmonyPatch(typeof(ClienteleUIManager), nameof(ClienteleUIManager.RefreshUI))]
    internal static class PatchClienteleForecast
    {
        private static void Postfix(ClienteleUIManager __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                TextMeshProUGUI tmp = __instance.normalCustomerTMP;
                if (tmp == null)
                {
                    return;
                }

                string name = Loc.Get("merchant_name", "磁带迷商人");
                if (tmp.text != null && tmp.text.Contains(name))
                {
                    return; // 原版渲染路径已经列出了这一条
                }

                if (!SignIsDisplayed())
                {
                    return;
                }

                int chance = SignChance();
                string line = "- " + name + " (" + chance + "%)\n";
                tmp.text = (tmp.text ?? string.Empty) + line;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] clientele row failed: " + e.Message);
            }
        }

        private static int SignChance()
        {
            // 与 PatchSignPrediction 里写死的概率保持一致
            return 50;
        }

        /// <summary>展示柜里是否有我们自己拥有的艺术品灯牌。</summary>
        internal static bool SignIsDisplayed()
        {
            if (EmporiumEntry.Instance == null || EmporiumEntry.Instance.showcaseElement == null)
            {
                return false;
            }
            foreach (GameItem child in EmporiumEntry.Instance.showcaseElement.childItems)
            {
                if (child != null && child.identifier == Ids.Sign && GeneralHelper.IsItemOwned(child))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
