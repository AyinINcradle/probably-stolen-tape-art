using System;

namespace TapeArt
{
    /// <summary>
    /// 本地化读取。找不到条目时返回内置中文原文，绝不会把 key 直接显示给玩家。
    /// </summary>
    internal static class Loc
    {
        public static string Get(string key, string fallback)
        {
            try
            {
                string s = ModHelper.GetLocalized(Ids.ModId, key);
                if (string.IsNullOrEmpty(s) || s == key)
                {
                    return fallback;
                }
                return s;
            }
            catch (Exception)
            {
                return fallback;
            }
        }
    }
}
