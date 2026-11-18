using System;
using HarmonyLib;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 两位模组 NPC 的「商品意向」显示为原版的艺术品标签，
    /// 但实际收购范围用这里的补丁锁死：发烧友只收磁带，商人只收完好的那几盘。
    /// </summary>
    [HarmonyPatch(typeof(StoreClient), nameof(StoreClient.IsClientBuyingThisItem), new[] { typeof(GameItem), typeof(bool) })]
    internal static class PatchInterestScope
    {
        private static void Postfix(StoreClient __instance, GameItem gameItem, ref bool __result)
        {
            try
            {
                if (!__result || __instance == null || gameItem == null)
                {
                    return;
                }
                if (__instance.identifier == Ids.Enthusiast)
                {
                    __result = Enthusiast.Accepts(gameItem);
                }
                else if (__instance.identifier == Ids.Merchant)
                {
                    __result = __instance.clientBuyingIdList.Contains(gameItem.identifier);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] interest scope failed: " + e.Message);
            }
        }
    }

    /// <summary>
    /// 磁带发烧友按品质付租金：严重损坏 5 / 损坏 10 / 完好 20。
    /// 直接改价，让议价界面显示的数字与实际进账一致。
    /// </summary>
    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetNegociatedValue))]
    internal static class PatchRentalPrice
    {
        private static void Postfix(GameItem __instance, ref long __result)
        {
            try
            {
                if (__instance == null || PlayerStore.Instance == null || PlayerStore.Instance.currentClientInstance == null)
                {
                    return;
                }
                StoreClient blueprint = PlayerStore.Instance.currentClientInstance.GetClientBlueprint();
                if (blueprint == null || blueprint.identifier != Ids.Enthusiast)
                {
                    return;
                }
                if (!__instance.IsTag("CASSETTE_TAG"))
                {
                    return;
                }

                int price = Enthusiast.TierPrice(__instance.identifier);
                if (price > 0)
                {
                    __result = price;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] rental price failed: " + e.Message);
            }
        }
    }
}
