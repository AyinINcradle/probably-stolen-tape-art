using System;
using HarmonyLib;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 旧存档在创建这一局时就把升级价格复制进了运行时条目，
    /// 只改定义或读档时改一次都可能被存档覆盖，所以显示价格时再兜一次底。
    /// </summary>
    [HarmonyPatch(typeof(NetworkUpgrade), nameof(NetworkUpgrade.GetCost))]
    internal static class PatchUpgradeCost
    {
        private static bool logged;

        private static void Postfix(NetworkUpgrade __instance, ref int __result)
        {
            try
            {
                if (__instance == null || __instance.id != Ids.Upgrade2)
                {
                    return;
                }
                if (Network.Unlocked(2) || __result >= Network.Cost2)
                {
                    return;
                }

                __result = Network.Cost2;
                if (!logged)
                {
                    logged = true;
                    TapeArtMod.Log?.Error("tier II cost forced to " + Network.Cost2);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
