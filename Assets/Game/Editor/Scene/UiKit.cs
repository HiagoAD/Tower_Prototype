using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Generic uGUI element factories for scene builders. Carries the display font as explicit state
    /// (null keeps the built-in font) instead of a static, so several builders can use different kits.
    /// </summary>
    public sealed class UiKit
    {
        public UiKit(Font displayFont)
        {
            DisplayFont = displayFont;
        }

        /// <summary>Kenney Future for titles, numbers, buttons and prompts; small event-card text keeps the built-in font, whose lowercase stays legible at that size.</summary>
        public Font DisplayFont { get; }

        public static Sprite UiSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static Sprite KnobSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        public void UseDisplayFont(Text text)
        {
            if (DisplayFont != null)
            {
                text.font = DisplayFont;
            }
        }

        public static GameObject BuildPanel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static void AddOutline(Text text, Color color, float distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>Non-interactive by default (raycastTarget = false) so decoration never swallows climb touches.</summary>
        public static Image AddImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Defaults to a centered anchor/pivot at the default 1000x150 size. Pass explicit
        /// anchors/pivot/size for anything that needs to sit at a screen edge instead of drifting off
        /// it in a taller aspect. Never a raycast target.
        /// </summary>
        public static Text AddText(Transform parent, string name, string content, int size, TextAnchor anchor, Vector2 anchoredPos,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta ?? new Vector2(1000f, 150f);
            rect.anchoredPosition = anchoredPos;
            return text;
        }

        /// <summary>A stock Unity button (default UISprite skin), as the reference's menus use.</summary>
        public Button AddButton(Transform parent, string name, string label, Vector2 anchoredPos, Color background, Color textColor,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            Vector2 size = sizeDelta ?? new Vector2(400f, 140f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var image = go.GetComponent<Image>();
            image.sprite = UiSprite;
            image.type = Image.Type.Sliced;
            image.color = background;

            Text text = AddText(go.transform, "Label", label, 50, TextAnchor.MiddleCenter, Vector2.zero);
            UseDisplayFont(text);
            text.rectTransform.sizeDelta = size;
            text.color = textColor;

            return go.GetComponent<Button>();
        }
    }
}
