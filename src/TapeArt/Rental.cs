using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 租借记录：把玩家租出去的磁带 id 连同到期日记在模组数据里
    /// （格式 "cassette5|3;cassette9l|3"，日 = 归还的那天），
    /// 次日清晨按记录重新生成并放回前柜台（卷帘门外侧）。
    /// </summary>
    internal static class Rental
    {
        private static List<(string id, int dueDay)> Pending()
        {
            List<(string, int)> list = new List<(string, int)>();
            string raw = null;
            try
            {
                raw = ModHelper.GetModData(Ids.ModId, Ids.DataRental, "");
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(raw))
            {
                return list;
            }

            foreach (string part in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }
                string[] kv = part.Split('|');
                if (kv.Length != 2)
                {
                    continue;
                }
                int due;
                if (int.TryParse(kv[1], out due))
                {
                    list.Add((kv[0], due));
                }
            }
            return list;
        }

        private static void Save(List<(string id, int dueDay)> list)
        {
            List<string> parts = new List<string>();
            foreach (var entry in list)
            {
                parts.Add(entry.id + "|" + entry.dueDay);
            }
            try
            {
                ModHelper.SetModData(Ids.ModId, Ids.DataRental, string.Join(";", parts.ToArray()));
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("rental save failed: " + e);
            }
        }

        /// <summary>借出一盘，两天后归还。</summary>
        public static void Add(string identifier)
        {
            List<(string, int)> list = Pending();
            int today = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
            list.Add((identifier, today + 2));
            Save(list);
        }

        /// <summary>把到期的磁带放回前柜台（玩家自己的东西）。</summary>
        public static void ReturnDue()
        {
            if (EmporiumEntry.Instance == null)
            {
                return;
            }

            int today = StoreStation.Instance != null ? StoreStation.Instance.dayCounter : 0;
            List<(string, int)> list = Pending();
            List<(string, int)> remain = new List<(string, int)>();
            int returned = 0;

            foreach (var entry in list)
            {
                if (entry.Item2 > today)
                {
                    remain.Add(entry);
                    continue;
                }

                GameItem item = ItemSpawner.Spawn(entry.Item1);
                if (item == null)
                {
                    continue;
                }
                GeneralHelper.SetItemOwned(item);

                bool placed = PlayerStore.Instance != null
                              && PlayerStore.Instance.TryAddDirectSellingItemToTable(item, isOwend: true);
                if (!placed)
                {
                    ModHelper.MoveItemToInventory(item);
                }
                if (item.parentInventory == null)
                {
                    ModHelper.MoveItemToFrontCounter(item);
                }
                if (item.parentInventory == null)
                {
                    item.Destroy();
                    TapeArtMod.Log?.Error("rental return lost: " + entry.Item1);
                    continue;
                }
                returned++;
            }

            if (returned > 0 || remain.Count != list.Count)
            {
                Save(remain);
                TapeArtMod.Log?.Log("rental return: " + returned + " tape(s) back on the front counter");
            }
        }
    }
}
