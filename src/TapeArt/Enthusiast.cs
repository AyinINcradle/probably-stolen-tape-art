using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 磁带发烧友：灯牌招来的普通买家。
    /// 玩家把磁带卖给他即可（原版议价流程，没有额外对话），
    /// 卖出的磁带两天后原样回到前柜台——这就是「出租」。
    /// 他自己也卖 1-2 盘损坏/严重损坏的磁带，且不收自己正在卖的那几首曲目。
    /// </summary>
    internal static class Enthusiast
    {
        /// <summary>他今天在售的曲目（基名），这些曲目他不会收（不听重复的磁带）。</summary>
        private static readonly HashSet<string> soldTracks = new HashSet<string>();

        /// <summary>本次到访准备好的货：在客人开口时才真正摆上柜台。</summary>
        private static readonly List<string> pendingStock = new List<string>();

        public static void Register()
        {
            StoreClientListDict.storeClientDict[Ids.Enthusiast] = Create;
        }

        private static StoreClient Create()
        {
            StoreClient client = new StoreClient();
            client.identifier = Ids.Enthusiast;
            client.nonShootable = true;
            client.displayName = Loc.Get("enthusiast_name", "磁带发烧友");
            // 每次到访都是不同的下层区居民
            client.spriteName = SpriteDict.GetRandomLowerLevelSpriteName();
            StoreClientFactionSetup.InitLowerLevelCitizen(client);

            // 既能买也能卖：买 = 把我们的磁带「租」走，卖 = 他自己的存货
            client.clientIntent = StoreClient.ClientIntent.SELLNBUY;
            // 界面显示原版的「艺术品」兴趣标签，实际收购范围由 PatchInterestScope 限定为磁带
            client.clientBuyingTagList.Add("ART");
            client.useClientBudget = true;
            client.SetBudget(UnityEngine.Random.Range(Ids.EnthusiastBudgetMin, Ids.EnthusiastBudgetMax + 1));
            client.ApplyBudgetModifier();

            // 先把要卖的货「选好」，但不碰商店柜台——创建客人时动柜台可能抛异常。
            PreRollStock();

            // 收所有品质的磁带，但排除他正在卖的曲目
            foreach (string track in Ids.PristineCassettes)
            {
                if (soldTracks.Contains(track))
                {
                    continue;
                }
                client.clientBuyingIdList.Add(track);
                client.clientBuyingIdList.Add(track + "l");
                client.clientBuyingIdList.Add(track + "d");
            }

            client.mainDialogue.SetText(client.displayName, Loc.Get("enthusiast_line", "「这些老带子……你都卖？」"))
                .SetEndAction(PlaceStock);
            client.AddBasicDialog();
            client.CompleteClientCreation();
            TapeArtMod.Log?.Log("enthusiast created, budget=" + client.GetBudget() + " stock=" + pendingStock.Count);
            return client;
        }

        /// <summary>只决定要卖哪几盘（不摆上柜台）。</summary>
        private static void PreRollStock()
        {
            soldTracks.Clear();
            pendingStock.Clear();

            List<string> pool = new List<string>(Ids.PristineCassettes);
            int count = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                string track = pool[index];
                pool.RemoveAt(index);

                string id = UnityEngine.Random.Range(0, 100) < 55 ? track + "d" : track + "l";
                soldTracks.Add(track);
                pendingStock.Add(id);
            }
        }

        /// <summary>开口时把货摆上柜台。</summary>
        private static void PlaceStock()
        {
            if (PlayerStore.Instance == null)
            {
                return;
            }
            foreach (string id in pendingStock)
            {
                PlayerStore.Instance.AddDirectSellingItemToTable(id);
            }
            TapeArtMod.Log?.Log("enthusiast placed stock: " + string.Join(",", pendingStock.ToArray()));
        }

        /// <summary>他只收磁带，且不收自己正在卖的那几首曲目。</summary>
        internal static bool Accepts(GameItem item)
        {
            if (item == null || !item.IsTag("CASSETTE_TAG"))
            {
                return false;
            }
            return !soldTracks.Contains(BaseId(item.identifier));
        }

        internal static string BaseId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return id;
            }
            return id.EndsWith("l") || id.EndsWith("d") ? id.Substring(0, id.Length - 1) : id;
        }
        /// <summary>完好 20 / 损坏 10 / 严重损坏 5。</summary>
        internal static int TierPrice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }
            if (id.EndsWith("d"))
            {
                return Ids.RentCorrupted;
            }
            if (id.EndsWith("l"))
            {
                return Ids.RentDamaged;
            }
            return Ids.RentPristine;
        }
    }
}
