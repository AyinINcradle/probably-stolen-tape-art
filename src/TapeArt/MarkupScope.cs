using System;
using HarmonyLib;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 原版会把客人的「供货商加价」词条挂到任何放进议价柜台的物品上，
    /// 于是玩家卖自己的磁带给他时也被加了价。这里只拦「商人 + 加价词条 + 玩家自己的物品」，
    /// 保证他的零售有加价、回收玩家的磁带没有。
    /// </summary>
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.AddItemFeature))]
    internal static class PatchSupplierMarkupScope
    {
        private const string MarkupFeature = "premiumBuy15";

        private static bool Prefix(GameItem __instance, ItemFeature itemFeature)
        {
            try
            {
                if (__instance == null || itemFeature == null || itemFeature.identifier != MarkupFeature)
                {
                    return true;
                }
                if (PlayerStore.Instance == null || PlayerStore.Instance.currentClientInstance == null)
                {
                    return true;
                }
                StoreClient blueprint = PlayerStore.Instance.currentClientInstance.GetClientBlueprint();
                if (blueprint == null || blueprint.identifier != Ids.Merchant)
                {
                    return true;
                }
                if (!GeneralHelper.IsItemOwned(__instance))
                {
                    return true;
                }

                // 玩家自己的物品：不加零售加价
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] markup scope failed: " + e.Message);
                return true;
            }
        }
    }
}
