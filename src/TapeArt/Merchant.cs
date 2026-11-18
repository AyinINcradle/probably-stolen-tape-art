using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeArt
{
    /// <summary>磁带迷商人：上架随机品质磁带与艺术品灯牌，只收购完好品质的磁带。</summary>
    internal static class Merchant
    {
        public static void Register()
        {
            StoreClientListDict.storeClientDict[Ids.Merchant] = Create;
        }

        private static StoreClient Create()
        {
            StoreClient client = new StoreClient();
            client.identifier = Ids.Merchant;
            client.nonShootable = true;
            client.displayName = Loc.Get("merchant_name", "磁带迷商人");
            client.spriteName = SpriteDict.GetRetiredAquafarmerSpriteName();
            client.clientFaction = "FACTION_LOWER_LEVEL";
            StoreClientFactionSetup.InitLowerLevelCitizen(client);

            // 原版「既卖又买」的写法；收购按柜台议价，只看玩家摆上柜台的货。
            client.clientIntent = StoreClient.ClientIntent.SELLNBUY;

            // 只收完好品质磁带：cassette1..14 的精确 id。
            // 用标签 CASSETTE_TAG 会把损坏(l)和严重损坏(d)也算进去。
            foreach (string id in Ids.PristineCassettes)
            {
                client.clientBuyingIdList.Add(id);
            }

            client.mainDialogue.SetText(client.displayName, Loc.Get("merchant_line", "「磁带？我全收——完好的那种。」"))
                .SetEndAction(Stock);

            client.AddBasicDialog();
            client.CompleteClientCreation();
            return client;
        }

        /// <summary>上架：五盘随机品质磁带，外加一块艺术品灯牌（已拥有则不再卖）。</summary>
        private static void Stock()
        {
            if (PlayerStore.Instance == null)
            {
                return;
            }

            // 只卖损坏(l)与严重损坏(d)，不出完好磁带：
            // 他本身只收完好磁带，若也卖完好磁带就能买了再卖回去刷声望。
            List<string> pool = new List<string>(Ids.PristineCassettes);
            for (int i = 0; i < 5 && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                string baseId = pool[index];
                pool.RemoveAt(index);

                int roll = UnityEngine.Random.Range(0, 100);
                string id = roll < 60 ? baseId + "l" : baseId + "d";
                PlayerStore.Instance.AddDirectSellingItemToTable(id);
            }

            bool sellsSign = !Items.PlayerOwns(Ids.Sign);
            if (sellsSign)
            {
                PlayerStore.Instance.AddDirectSellingItemToTable(Ids.Sign);
            }
        }
    }
}
