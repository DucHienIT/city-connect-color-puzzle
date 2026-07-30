using System;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>
    /// Helpers for building the whole UI from code (no prefabs): canvas, panels,
    /// legacy-Text labels and buttons. Visuals come from the GUI Pro-CasualGame kit
    /// via the UITheme asset in Resources/UI (LilitaOne font, sliced button/panel
    /// sprites, picto icons); everything degrades to the flat SpriteFactory look
    /// when the theme or a sprite is missing. Deliberately not TMP.
    /// </summary>
    public static class UIFactory
    {
        static UITheme theme;
        static bool themeSearched;
        public static UITheme Theme
        {
            get
            {
                if (!themeSearched)
                {
                    themeSearched = true;
                    theme = Resources.Load<UITheme>("UI/UITheme");
                }
                return theme;
            }
        }

        /// <summary>
        /// Inject the theme directly (scene-serialized reference in GameBootstrap).
        /// Luna exports only scene dependencies, so Resources.Load finds nothing there.
        /// </summary>
        public static void SetTheme(UITheme injected)
        {
            if (injected == null) return;
            theme = injected;
            themeSearched = true;
        }

        static Font fallbackFont;
        public static Font Font
        {
            get
            {
                if (Theme != null && Theme.font != null) return Theme.font;
                // Non-generic overload: Luna's bridged Resources has no generic version,
                // and even this one throws NotSupported there — degrade to null (no text)
                // rather than killing the boot.
                if (fallbackFont == null)
                    try { fallbackFont = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf"); }
                    catch { }
                return fallbackFont;
            }
        }

        public static readonly Color Accent = new Color(0.97f, 0.76f, 0.18f);
        public static readonly Color AccentDark = new Color(0.72f, 0.53f, 0.08f);
        public static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.17f, 0.98f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.24f, 0.34f);
        public static readonly Color TextColor = new Color(0.93f, 0.94f, 0.98f);
        public static readonly Color MutedText = new Color(0.60f, 0.64f, 0.74f);
        // Dark "ink" tones for text sitting directly on the light 3D city backdrop.
        public static readonly Color Ink = new Color(0.25f, 0.23f, 0.19f);
        public static readonly Color InkMuted = new Color(0.47f, 0.44f, 0.38f);
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

        /// <summary>Flat procedural image (dim layers, theme-less fallback panels).</summary>
        public static Image CreateImage(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? SpriteFactory.UIRounded : SpriteFactory.Square;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        /// <summary>Kit sprite image; 9-sliced automatically when the sprite has a border.</summary>
        public static Image CreateSpriteImage(Transform parent, string name, Sprite sprite, Color? tint = null)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = tint ?? Color.white;
            return img;
        }

        /// <summary>Non-interactive icon image keeping its aspect ratio.</summary>
        public static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 size, Color? tint = null)
        {
            var img = CreateSpriteImage(parent, name, sprite, tint);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            ((RectTransform)img.transform).sizeDelta = size;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int size,
            Color color, FontStyle style = FontStyle.Bold, bool shadow = true)
        {
            var rt = CreateRect(parent, name);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            // LilitaOne only ships a regular face; bold would be faux-rendered.
            text.fontStyle = Theme != null && Theme.font != null ? FontStyle.Normal : style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (shadow && Theme != null)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
                sh.effectDistance = new Vector2(0f, -3f);
            }
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size,
            Action onClick, Sprite sprite = null, int fontSize = 44)
        {
            if (sprite == null && Theme != null) sprite = Theme.buttonBlue;
            var img = sprite != null
                ? CreateSpriteImage(parent, name, sprite)
                : CreateImage(parent, name, ButtonColor);
            var rt = (RectTransform)img.transform;
            rt.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                onClick?.Invoke();
            });
            // Springy squash feedback on press (unscaled → works in the paused popup too).
            img.gameObject.AddComponent<UIPressEffect>();
            var text = CreateText(img.transform, "Label", label, fontSize, Color.white);
            Stretch((RectTransform)text.transform);
            return button;
        }

        /// <summary>
        /// Crisp readable outline for text sitting on a busy/light backdrop: two Outline
        /// components (a single one only covers the diagonals) plus a drop Shadow.
        /// </summary>
        public static void AddOutline(Text text, Color color, float thickness = 3f, bool drop = true)
        {
            var o1 = text.gameObject.AddComponent<Outline>();
            o1.effectColor = color;
            o1.effectDistance = new Vector2(thickness, -thickness);
            var o2 = text.gameObject.AddComponent<Outline>();
            o2.effectColor = color;
            o2.effectDistance = new Vector2(-thickness, thickness);
            if (drop)
            {
                var sh = text.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(color.r, color.g, color.b, 0.55f);
                sh.effectDistance = new Vector2(0f, -thickness - 1f);
            }
        }

        /// <summary>Square button showing only an icon (bottom action bar and the like).</summary>
        public static Button CreateIconButton(Transform parent, string name, Sprite bg, Sprite icon,
            float size, Action onClick)
        {
            var button = CreateButton(parent, name, "", new Vector2(size, size), onClick, bg);
            if (icon != null)
            {
                var img = CreateIcon(button.transform, "Icon", icon, Vector2.one * (size * 0.54f));
                Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * (size * 0.54f));
            }
            return button;
        }

        /// <summary>Adds a leading icon to a labeled button and nudges the label right.</summary>
        public static void AddButtonIcon(Button button, Sprite icon, float size)
        {
            if (icon == null) return;
            var img = CreateIcon(button.transform, "Icon", icon, Vector2.one * size);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(size * 0.85f + 14f, 4f);
            var label = (RectTransform)ButtonLabel(button).transform;
            label.offsetMin = new Vector2(size * 0.9f, 0f);
        }

        /// <summary>Finds the Text child created by CreateButton, for dynamic labels.</summary>
        public static Text ButtonLabel(Button button) => button.GetComponentInChildren<Text>();
    }
}
