using System;
using UnityEngine;

namespace TapeArt
{
    internal static class Ids
    {
        public const string ModId = "tape_art";
        public const string Sign = "tape_art_sign";
        public const string Core = "tape_art_core";
        public const string Merchant = "tape_art_merchant";

        public const string Upgrade1 = "TAPE_ART_I";
        public const string Upgrade2 = "TAPE_ART_II";
        public const string Upgrade3 = "TAPE_ART_III";

        public const string SignSprite = "tape_art_sign";
        public const string CoreSprite = "tape_art_core";

        /// <summary>原版词条【磁带迷】的 id。</summary>
        public const string TapeheadPerk = "tapehead";

        public const string DataLastVisitDay = "last_merchant_day";

        public static readonly string[] PristineCassettes =
        {
            "cassette1", "cassette2", "cassette3", "cassette4", "cassette5", "cassette6", "cassette7",
            "cassette8", "cassette9", "cassette10", "cassette11", "cassette12", "cassette13", "cassette14"
        };
    }

    /// <summary>物品注册：艺术品灯牌 / 磁带播放机核心。</summary>
    internal static class Items
    {
        public static void Register(ModItemDirectory dir)
        {
            dir.Add(Ids.Sign, CreateSign);
            dir.Add(Ids.Core, CreateCore);
        }

        /// <summary>自检：目录里到底有没有这两个物品。</summary>
        public static bool InCatalog(string id)
        {
            try
            {
                return DirectoryMaster.Has<GameItem>(id);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>照 ToolDirectory.CreateLuxurySign 的写法：IMPORTANT_TAG + 展示柜特殊物品类型。</summary>
        public static GameItem CreateSign()
        {
            GameItem item = ItemDirectory.CreateEmptyItem(Ids.Sign);
            item.name = Loc.Get("item_sign_name", "艺术品灯牌");
            item.shortDescription = "放进展示柜，招来想出手" + TypeHelper.GetTypeDisplayName("ART") + "的客人。";
            item.flavorText = Loc.Get("item_sign_flavor", "「放点音乐，客人自己会来。」");
            item.unitValue = 300L;
            item.EnableTag("IMPORTANT_TAG");
            item.SetSpriteAndShapeFromMod(Ids.ModId, Ids.SignSprite);
            ShowcaseHelper.InitShowcaseSpecialItem(item, "ART");
            return item;
        }

        /// <summary>
        /// 照 VendingMachineHelper.VendingMachineCore：同一个图集、同一张 core 精灵图，风格与原版模块一致。
        /// 原版核心只有名字，具体数值都放在提示框里（见 TapeArtMod.OnTooltip），所以这里不写长描述。
        /// </summary>
        public static GameItem CreateCore()
        {
            GameItem item = ItemDirectory.CreateEmptyItem(Ids.Core);
            item.name = Loc.Get("item_core_name", "磁带播放机核心");
            item.shortDescription = Loc.Get("item_core_desc", "装进磁带播放机核心槽的芯片。");
            item.flavorText = Loc.Get("item_core_flavor", "「整条街都该听见你的品味。」");
            item.unitValue = 350L;
            item.SetSpriteAndShape("Items/items_module", "core");
            return item;
        }

        public static bool PlayerOwns(string identifier)
        {
            if (EmporiumEntry.Instance == null)
            {
                return false;
            }
            foreach (GameItem item in EmporiumEntry.Instance.GetAllOwnedItems())
            {
                if (item != null && item.identifier == identifier)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
