using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapeArt
{
    /// <summary>
    /// 日历夜间报告：磁带播放机的吸引力单独画成淡紫色段，标签写「磁带」。
    /// 采用 v0.17.1 里「能正常显示」的做法：整份克隆投影仪色条（含它的 TMP 标签），
    /// 只改颜色和文字；不用自动缩放，另外把标签框放宽居中，避免被窄色条裁掉。
    /// </summary>
    [HarmonyPatch(typeof(AdvCalendarUIManager), nameof(AdvCalendarUIManager.UpdateAttractivenessSubsection))]
    internal static class PatchCalendarAttractiveness
    {
        private const string MeterName = "tape_art_meter";
        private const float LabelWidth = 220f;

        private static readonly Color TapeColor = new Color32(198, 148, 255, 255);

        private static void Postfix(AdvCalendarUIManager __instance)
        {
            try
            {
                if (__instance == null || __instance.projectorMeter == null || __instance.baseMeter == null)
                {
                    return;
                }

                AppendCostLine(__instance);

                RectTransform meter = EnsureMeter(__instance);
                if (meter == null)
                {
                    return;
                }

                int bonus = Core.AttractionBonus();
                if (bonus <= 0)
                {
                    meter.gameObject.SetActive(false);
                    return;
                }

                int total = Mathf.Clamp(PlayerStore.Instance.GetCurrentStoreAttractiveness(), 0, 1000);
                int projector = Mathf.Min(PlayerStore.Instance.projectorAttractivenessBonus, total);
                int rest = Mathf.Max(0, total - projector);
                int tapeWidth = Mathf.Min(bonus, rest);
                if (tapeWidth <= 0)
                {
                    meter.gameObject.SetActive(false);
                    return;
                }

                float height = __instance.projectorMeter.rectTransform.sizeDelta.y;
                __instance.baseMeter.rectTransform.sizeDelta = new Vector2(rest - tapeWidth, __instance.baseMeter.rectTransform.sizeDelta.y);
                meter.sizeDelta = new Vector2(tapeWidth, height);
                ApplyLabel(__instance, meter, height);
                meter.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TapeArt] calendar attractiveness meter failed: " + e.Message);
            }
        }

        /// <summary>在夜间报告的吸引力文字下方补一行：磁带播放机每晚扣多少信用点。</summary>
        private static void AppendCostLine(AdvCalendarUIManager ui)
        {
            if (!Network.Unlocked(2))
            {
                return;
            }

            TextMeshProUGUI tmp = ui.storeAttractivenessTMP;
            if (tmp == null || tmp.text == null)
            {
                return;
            }

            string template = Loc.Get("calendar_tape_cost", "磁带播放机电费：每晚 {0} 信用点");
            string line = template.Replace("{0}", Core.Usage().ToString());
            if (tmp.text.Contains(line))
            {
                return;
            }

            tmp.text = tmp.text + "\n" + line;
        }

        /// <summary>标签：固定宽框、居中、不裁切，文字就是「磁带」。</summary>
        private static void ApplyLabel(AdvCalendarUIManager ui, RectTransform meter, float height)
        {
            string text = Loc.Get("calendar_tape_label", "磁带");
            TextMeshProUGUI template = ui.storeAttractivenessTMP;
            foreach (TMP_Text label in meter.GetComponentsInChildren<TMP_Text>(true))
            {
                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(LabelWidth, Mathf.Max(10f, height));

                // 关键：投影仪那段的标签用的是西文字体资源，写中文会变成空白。
                // 换成日历里能显示中文的那份字体（storeAttractivenessTMP）。
                if (template != null && template.font != null)
                {
                    label.font = template.font;
                    label.fontSharedMaterial = template.fontSharedMaterial;
                }

                label.enableAutoSizing = false;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.gameObject.SetActive(true);
                if (label.text != text)
                {
                    label.text = text;
                }
            }
        }

        /// <summary>整份克隆投影仪色条（含 TMP 标签），只换颜色与文字。</summary>
        private static RectTransform EnsureMeter(AdvCalendarUIManager ui)
        {
            Transform parent = ui.projectorMeter.transform.parent;
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(MeterName);
            if (existing != null)
            {
                return existing as RectTransform;
            }

            GameObject clone = UnityEngine.Object.Instantiate(ui.projectorMeter.gameObject, parent, false);
            clone.name = MeterName;
            clone.transform.SetSiblingIndex(ui.projectorMeter.transform.GetSiblingIndex() + 1);

            Image image = clone.GetComponent<Image>();
            if (image != null)
            {
                image.color = TapeColor;
                image.raycastTarget = false;
            }

            TMP_Text[] labels = clone.GetComponentsInChildren<TMP_Text>(true);
            string before = labels.Length > 0 ? labels[0].text : "<none>";
            float fontSize = labels.Length > 0 ? labels[0].fontSize : 0f;

            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }
                if (behaviour.GetType().Name.Contains("Localizer"))
                {
                    behaviour.enabled = false;
                    UnityEngine.Object.Destroy(behaviour);
                }
            }

            foreach (TMP_Text label in labels)
            {
                label.text = Loc.Get("calendar_tape_label", "磁带");
                label.gameObject.SetActive(true);
            }

            clone.SetActive(true);

            return clone.transform as RectTransform;
        }
    }
}
