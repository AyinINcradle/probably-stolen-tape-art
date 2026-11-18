#if TAPE_ART_DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TapeArt
{
    /// <summary>
    /// 调试专用：把游戏里已加载的 NPC 立绘导出成 PNG，方便挑选商人/客人形象。
    /// 只写进 Mods\tape_art_debug\sprite_dump\，发布版不含此文件。
    /// </summary>
    internal static class DebugSprites
    {
        public static void Export()
        {
            try
            {
                SpriteDict dict = SpriteDict.Instance;
                if (dict == null || dict.spriteDictionary == null)
                {
                    TapeArtMod.Log?.Error("sprite export: SpriteDict not ready");
                    return;
                }

                string root = Path.Combine(Application.dataPath, "..");
                string outDir = Path.Combine(root, "Mods", "tape_art_debug", "sprite_dump");
                System.IO.Directory.CreateDirectory(outDir);

                int saved = 0;
                List<string> keys = new List<string>();
                foreach (KeyValuePair<string, Sprite> pair in dict.spriteDictionary)
                {
                    string key = pair.Key;
                    if (key == null || pair.Value == null)
                    {
                        continue;
                    }
                    if (!LooksLikePortrait(key))
                    {
                        continue;
                    }
                    keys.Add(key);
                    if (Save(pair.Value, Path.Combine(outDir, key + ".png")))
                    {
                        saved++;
                    }
                }

                System.IO.File.WriteAllLines(Path.Combine(outDir, "_keys.txt"), keys.ToArray());
                TapeArtMod.Log?.Error("sprite export: " + saved + " png(s) -> " + outDir);
            }
            catch (Exception e)
            {
                TapeArtMod.Log?.Error("sprite export failed: " + e);
            }
        }

        private static bool LooksLikePortrait(string key)
        {
            string k = key.ToLowerInvariant();
            return k.Contains("lower") || k.Contains("upper") || k.Contains("retired")
                   || k.StartsWith("male") || k.StartsWith("female") || k.Contains("scav");
        }

        private static bool Save(Sprite sprite, string path)
        {
            Texture2D atlas = sprite.texture;
            if (atlas == null)
            {
                return false;
            }

            RenderTexture rt = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(atlas, rt);
                RenderTexture.active = rt;

                Texture2D readable = new Texture2D(atlas.width, atlas.height, TextureFormat.ARGB32, false);
                readable.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                readable.Apply();

                Rect r = sprite.rect;
                int w = Mathf.Max(1, (int)r.width);
                int h = Mathf.Max(1, (int)r.height);
                int x = Mathf.Clamp((int)r.x, 0, Mathf.Max(0, atlas.width - w));
                int y = Mathf.Clamp((int)r.y, 0, Mathf.Max(0, atlas.height - h));

                Texture2D crop = new Texture2D(w, h, TextureFormat.ARGB32, false);
                crop.SetPixels(readable.GetPixels(x, y, w, h));
                crop.Apply();

                byte[] png = crop.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, png);

                UnityEngine.Object.Destroy(readable);
                UnityEngine.Object.Destroy(crop);
                return png != null && png.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
#endif
