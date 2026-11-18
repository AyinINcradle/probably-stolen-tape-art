using System;
using HarmonyLib;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 卖给磁带发烧友的磁带视为「出租」：两天后原样回到前柜台。
    /// 走原版成交流程，玩家像卖东西一样把磁带摆上柜台即可。
    /// </summary>
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.SellItem))]
    internal static class PatchSellToEnthusiast
    {
        private static void Postfix(GameItem item)
        {
            try
            {
                if (item == null || PlayerStore.Instance == null || PlayerStore.Instance.currentClientInstance == null)
                {
                    return;
                }
                StoreClient blueprint = PlayerStore.Instance.currentClientInstance.GetClientBlueprint();
                if (blueprint == null || blueprint.identifier != Ids.Enthusiast)
                {
                    return;
                }
                if (!item.IsTag("CASSETTE_TAG"))
                {
                    return;
                }

                Rental.Add(item.identifier);
                TapeArtMod.Log?.Log("enthusiast rented: " + item.identifier);
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("rental hook failed: " + e);
            }
        }
    }
}
