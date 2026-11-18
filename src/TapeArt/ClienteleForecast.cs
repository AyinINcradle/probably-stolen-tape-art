using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 「顾客群体」界面：把灯牌招来的磁带发烧友补进列表。
    /// 判断同样走 ShowcaseHelper.IsShowcaseContainItemById，兼容悬挂广告牌类模组。
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

                string name = Loc.Get("enthusiast_name", "磁带发烧友");
                if (tmp.text != null && tmp.text.Contains(name))
                {
                    return; // 原版渲染路径已经列出了这一条
                }

                if (!PatchSignPrediction.SignDisplayed())
                {
                    return;
                }

                tmp.text = (tmp.text ?? string.Empty) + "- " + name + " (" + PatchSignPrediction.Chance + "%)\n";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] clientele row failed: " + e.Message);
            }
        }
    }
}
