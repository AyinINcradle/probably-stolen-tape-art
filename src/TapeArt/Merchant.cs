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
            client.spriteName = SpriteName();
            client.clientFaction = "FACTION_LOWER_LEVEL";
            StoreClientFactionSetup.InitLowerLevelCitizen(client);

            // 原版「既卖又买」的写法；收购按柜台议价，只看玩家摆上柜台的货。
            client.clientIntent = StoreClient.ClientIntent.SELLNBUY;
            // 界面显示原版的「艺术品」兴趣标签，实际只收完好的那几盘
            client.clientBuyingTagList.Add("ART");

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

        /// <summary>商人当前使用的立绘；发烧友也用它，保持「同行供货商」的形象。</summary>
        public static string SpriteName()
        {
            return PickCollectorSprite();
        }

        /// <summary>
        /// 商人的固定形象：从拾荒者立绘池里挑一次就存进模组数据，之后每次到访都是同一个人
        /// （翻旧货的气质，比退休水培农场主更像个磁带收藏家）。
        /// </summary>
        private static string PickCollectorSprite()
        {
            string cached = null;
            try
            {
                cached = ModHelper.GetModData(Ids.ModId, "merchant_sprite", "");
            }
            catch (Exception)
            {
            }
            if (!string.IsNullOrEmpty(cached))
            {
                return cached;
            }

            string picked = SpriteDict.GetRandomScavSpriteName();
            try
            {
                ModHelper.SetModData(Ids.ModId, "merchant_sprite", picked);
            }
            catch (Exception)
            {
            }
            return picked;
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

            // 顺带补一点电子元件（普通），玩家修磁带要用。
            int parts = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < parts; i++)
            {
                PlayerStore.Instance.AddDirectSellingItemToTable(Ids.ElectronicPart);
            }
        }
    }
}
