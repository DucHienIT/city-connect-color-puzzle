using System;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>
    /// Helpers for building the whole UI from code (no prefabs): canvas, panels,
    /// legacy-Text labels and buttons, plus the shared color scheme.
    /// </summary>
    public static class UIFactory
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static readonly Color Accent = new Color(0.97f, 0.76f, 0.18f);
        public static readonly Color AccentDark = new Color(0.72f, 0.53f, 0.08f);
        public static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.17f, 0.98f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.24f, 0.34f);
        public static readonly Color TextColor = new Color(0.93f, 0.94f, 0.98f);
        public static readonly Color MutedText = new Color(0.60f, 0.64f, 0.74f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Full-stretch empty rect used as a screen root.</summary>
        public static RectTransform CreateScreen(Transform parent, string name)
        {
            var rt = CreateRect(parent, name);
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        public static Image CreateImage(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? SpriteFactory.UIRounded : SpriteFactory.Square;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int size,
            Color color, FontStyle style = FontStyle.Bold)
        {
            var rt = CreateRect(parent, name);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size,
            Action onClick, Color? bg = null, int fontSize = 44)
        {
            var img = CreateImage(parent, name, bg ?? ButtonColor);
            var rt = (RectTransform)img.transform;
            rt.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                onClick?.Invoke();
            });
            var text = CreateText(img.transform, "Label", label, fontSize, TextColor);
            Stretch((RectTransform)text.transform);
            return button;
        }

        /// <summary>Finds the Text child created by CreateButton, for dynamic labels.</summary>
        public static Text ButtonLabel(Button button) => button.GetComponentInChildren<Text>();
    }
}
