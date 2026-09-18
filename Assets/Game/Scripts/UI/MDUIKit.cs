using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Code-built uGUI helpers. All screens are constructed from these at runtime, so
/// there is no scene-authored hierarchy to keep in sync.
/// Placeholder art only: flat colours stand in for the brass/wood/leather set (GDD 5).
/// </summary>
public static class MDUIKit
{
    // --- Palette (GDD 5: warm brass, wood, leather, aged paper. No fintech blue/black.) ---
    public static readonly Color Wood = new Color(0.14f, 0.09f, 0.05f);
    public static readonly Color WoodLight = new Color(0.25f, 0.16f, 0.09f);
    public static readonly Color Brass = new Color(0.80f, 0.62f, 0.26f);
    public static readonly Color BrassDark = new Color(0.42f, 0.31f, 0.12f);
    public static readonly Color Paper = new Color(0.91f, 0.86f, 0.73f);
    public static readonly Color PaperDark = new Color(0.78f, 0.71f, 0.57f);
    public static readonly Color Ink = new Color(0.15f, 0.11f, 0.07f);
    public static readonly Color Leather = new Color(0.31f, 0.17f, 0.10f);
    public static readonly Color Good = new Color(0.22f, 0.55f, 0.25f);
    public static readonly Color Bad = new Color(0.70f, 0.22f, 0.19f);
    public static readonly Color Muted = new Color(0.46f, 0.41f, 0.34f);

    // --- Layout reference frame ---
    public const float RefW = 1080f;
    public const float RefH = 1920f;

    /// <summary>
    /// Widest the content column is allowed to get. An iPad gives us ~1440 reference units
    /// of width; stretching a phone layout across all of it looks broken, so the column is
    /// capped and centred and the extra width becomes desk on either side.
    /// </summary>
    public const float MaxColumnWidth = 1120f;

    /// <summary>
    /// Breathing room down each side of the content column. Without it the column is
    /// exactly screen-wide on a phone, so cards sit flush against the edge and read as
    /// cut off against a rounded display.
    /// </summary>
    public const float SideMargin = 28f;

    // --- Type ---
    //
    // Three tiers, picked automatically from the size and weight a caller asks for:
    //   Display - Anton, a heavy condensed grotesque. Titles, company names, headlines.
    //             Reads like a newspaper masthead on a brass plaque.
    //   Label   - Oswald Bold, same condensed family feel. Buttons, headers, figures.
    //   Body    - LiberationSans, regular weight. Paragraphs, where readability wins.
    //
    // Anton and Oswald Bold are already heavy faces, so FontStyle.Bold is NOT re-applied
    // to them - faux bold on top of a bold face smears it.
    const int DisplaySizeThreshold = 38;

    /// <summary>
    /// Global type scale. Everything was reading too small on device, and this lifts the
    /// whole game at once rather than touching ~90 call sites.
    /// </summary>
    public const float TextScale = 1.18f;

    /// <summary>
    /// How far auto-sizing is allowed to shrink a label, as a fraction of its target size.
    /// Kept high so a tight row produces slightly smaller text rather than tiny text.
    /// </summary>
    /// Still well above the old 0.45: combined with TextScale, the smallest a label can
    /// render is ~0.68 of its original design size, where before it was 0.45.
    const float MinTextFraction = 0.58f;

    /// <summary>Warm cream for text sitting on dark wood, brass or leather.</summary>
    public static readonly Color OnDark = new Color(0.98f, 0.93f, 0.80f);

    static TMP_FontAsset _display, _labelFont, _body;

    public static TMP_FontAsset DisplayFont => _display != null ? _display : (_display = LoadFont("Anton SDF"));
    public static TMP_FontAsset LabelFont => _labelFont != null ? _labelFont : (_labelFont = LoadFont("Oswald Bold SDF"));
    public static TMP_FontAsset BodyFont => _body != null ? _body : (_body = LoadFont("LiberationSans SDF"));

    static TMP_FontAsset LoadFont(string assetName)
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/" + assetName);
        if (font == null)
        {
            Debug.LogWarning($"[MDUIKit] Font '{assetName}' not found, falling back to the TMP default.");
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    static TMP_FontAsset FontFor(int size, FontStyle style, out bool alreadyBold)
    {
        if (size >= DisplaySizeThreshold) { alreadyBold = true; return DisplayFont; }
        if (style == FontStyle.Bold || style == FontStyle.BoldAndItalic) { alreadyBold = true; return LabelFont; }
        alreadyBold = false;
        return BodyFont;
    }

    public static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);      // portrait (GDD 1)
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Match height, not a 0.5 blend. These screens are vertically dense and
        // horizontally flexible, so pinning the vertical budget to exactly RefH means the
        // stacked rows below always fit, on a 4:3 iPad and on a 20:9 Android alike.
        // Width is then the variable, and every row is a horizontal layout group.
        scaler.matchWidthOrHeight = 1f;

        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        new GameObject("EventSystem", typeof(EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    // --- Primitives ---

    public static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static Image Panel(Transform parent, string name, Color color)
    {
        var rt = Rect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    /// <summary>
    /// Applies a skin sprite to a panel. When the sprite is missing the panel keeps its
    /// flat placeholder colour, so the game still renders if art is renamed or removed.
    /// Sprites are tinted white, otherwise the placeholder colour would stain the artwork.
    /// </summary>
    public static Image Skin(Image img, Sprite sprite, bool sliced = true)
    {
        if (img == null || sprite == null) return img;
        img.sprite = sprite;
        img.color = Color.white;
        // Sliced needs a border on the sprite; without one Unity draws nothing, so fall
        // back to a plain stretch for art that was never given 9-slice borders.
        bool hasBorder = sprite.border.sqrMagnitude > 0f;
        img.type = sliced && hasBorder ? Image.Type.Sliced : Image.Type.Simple;
        img.preserveAspect = false;
        return img;
    }

    public static Image Panel(Transform parent, string name, Color color, Sprite sprite, bool sliced = true)
        => Skin(Panel(parent, name, color), sprite, sliced);

    /// <summary>Brass-edged card. Returns the inner paper surface to parent content into.</summary>
    public static RectTransform Card(Transform parent, string name, Color fill, int edge = 5)
    {
        var outer = Panel(parent, name, BrassDark);
        var inner = Panel(outer.transform, "Fill", fill);
        Fill(inner.rectTransform, edge);
        return inner.rectTransform;
    }

    /// <summary>
    /// A card backed by a framed sprite. The frame art already draws its own border, so
    /// content is inset by <paramref name="pad"/> instead of a second brass edge being
    /// drawn under it. Falls back to the flat <see cref="Card"/> when the sprite is missing.
    /// </summary>
    /// <param name="padTop">
    /// Separate top inset. The framed panel art carries a dark wood title plate across its
    /// top 95px, and 9-slicing keeps that plate the same height at any card size. Content
    /// laid out from the top would otherwise land dark-on-dark on it.
    /// </param>
    public static RectTransform SkinnedCard(Transform parent, string name, Sprite sprite,
                                            Color fallbackFill, float pad = 34f, float padTop = -1f)
    {
        if (sprite == null) return Card(parent, name, fallbackFill);

        var outer = Panel(parent, name, Color.white);
        Skin(outer, sprite);
        var inner = Rect(outer.transform, "Fill");
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(pad, pad);
        inner.offsetMax = new Vector2(-pad, -(padTop >= 0f ? padTop : pad));
        return inner;
    }

    /// <summary>Height of the wood title plate on the framed panel art, in reference units.</summary>
    public const float PanelPlateHeight = 96f;

    /// <summary>
    /// <paramref name="size"/> is a ceiling, not a fixed size. Auto-sizing is on by default
    /// so a label can only ever shrink to stay inside its own rect - it never spills over
    /// the row beneath it on a narrow phone, and it never renders larger than designed on a
    /// roomy iPad. Pass <paramref name="autoFit"/> false only for text that must not resize.
    ///
    /// TextAnchor and FontStyle are kept in the signature, rather than TMP's own enums, so
    /// that the ~90 call sites across the game did not all have to change when this moved
    /// from UI.Text to TextMeshPro.
    /// </summary>
    public static TMP_Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter,
                                 FontStyle style = FontStyle.Normal, bool autoFit = true)
    {
        var rt = Rect(parent, "Label");
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();

        // The tier is chosen from the size the caller asked for, so the thresholds keep
        // their meaning, but the text renders at the scaled-up size.
        t.font = FontFor(size, style, out bool alreadyBold);
        int scaled = Mathf.RoundToInt(size * TextScale);

        t.text = text;
        t.fontSize = scaled;
        t.color = color;
        t.alignment = ToTMP(anchor);
        // Body text is the only tier whose face is not already heavy, so it gets real bold
        // applied. That keeps small paragraph text legible over busy background art.
        t.fontStyle = ToTMP(style, alreadyBold) | (alreadyBold ? FontStyles.Normal : FontStyles.Bold);
        // Truncate rather than overflow: overflowing text is what actually causes the
        // overlap between stacked rows when a device is narrower than the reference.
        t.overflowMode = TextOverflowModes.Truncate;
        t.raycastTarget = false;

        if (autoFit)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = scaled;
            t.fontSizeMin = Mathf.Max(16f, scaled * MinTextFraction);
        }
        return t;
    }

    static TextAlignmentOptions ToTMP(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    static FontStyles ToTMP(FontStyle style, bool fontIsAlreadyBold)
    {
        var result = FontStyles.Normal;
        if (!fontIsAlreadyBold && (style == FontStyle.Bold || style == FontStyle.BoldAndItalic))
            result |= FontStyles.Bold;
        if (style == FontStyle.Italic || style == FontStyle.BoldAndItalic)
            result |= FontStyles.Italic;
        return result;
    }

    public static Button Btn(Transform parent, string label, Color bg, Color fg, int size, Action onClick)
    {
        var rt = Rect(parent, "Btn_" + label);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;

        // Every button is the wood-and-brass plaque. It is 9-sliced, so it keeps its brass
        // end caps whatever shape the layout gives it. The requested colour survives as a
        // tint, which is how BUY stays green and SELL stays red.
        if (MDSkin.Plaque != null)
        {
            img.sprite = MDSkin.Plaque;
            img.type = Image.Type.Sliced;
            img.color = Color.Lerp(Color.white, bg, 0.55f);
        }

        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        cb.disabledColor = new Color(0.55f, 0.52f, 0.48f, 0.55f);
        cb.fadeDuration = 0.06f;
        b.colors = cb;

        // Buttons always read as labels, whatever size they are asked for.
        // The plaque is dark wood, so the caller's colour (chosen back when buttons were
        // flat brass) is overridden with cream - dark-on-dark was unreadable.
        var t = Label(rt, label, size, MDSkin.Plaque != null ? OnDark : fg,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        if (size >= DisplaySizeThreshold) t.font = LabelFont;

        // Inset clear of the plaque's brass end caps. Those stay 75px wide whatever the
        // button's size, because the sprite is 9-sliced, so the inset is fixed too.
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(MDSkin.Plaque != null ? 46f : 10f, 12f);
        trt.offsetMax = new Vector2(MDSkin.Plaque != null ? -46f : -10f, -12f);

        // Every button in the game is built here, so the click sound only needs wiring once.
        b.onClick.AddListener(MDAudio.PlayClick);
        if (onClick != null) b.onClick.AddListener(() => onClick());
        return b;
    }

    /// <summary>The label of a button built by <see cref="Btn"/>, for live relabelling.</summary>
    public static TMP_Text BtnLabel(Button b) => b.GetComponentInChildren<TMP_Text>();

    // --- Marks and pips ---
    //
    // None of the bundled fonts carry the star, tick or cross glyphs, and TMP will not fall
    // back to an OS font the way the legacy dynamic font did. These are drawn as plain
    // rects instead, which is also where the brass star art is meant to go later (GDD 9).

    /// <summary>Star rating as brass pips. Placeholder diamonds until the star art lands.</summary>
    public static RectTransform StarRow(Transform parent, int filled, int total, float pip = 34f)
    {
        var row = Rect(parent, "Stars");
        var stack = HStack(row, pip * 0.55f, 0);
        stack.childForceExpandWidth = false;
        stack.childForceExpandHeight = false;
        stack.childControlWidth = false;
        stack.childControlHeight = false;

        for (int i = 0; i < total; i++)
        {
            var star = Panel(row, i < filled ? "StarOn" : "StarOff", i < filled ? Brass : BrassDark);
            star.rectTransform.sizeDelta = new Vector2(pip, pip);

            if (MDSkin.Star != null)
            {
                star.sprite = MDSkin.Star;
                star.preserveAspect = true;
                star.color = i < filled ? Color.white : new Color(0.35f, 0.30f, 0.24f, 0.9f);
            }
            else
            {
                star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);   // square -> diamond
            }
        }
        return row;
    }

    /// <summary>
    /// A line of text with a status pip in front of it, standing in for a tick or a cross.
    /// Returns the row, which is what the caller needs to size inside a layout group.
    /// </summary>
    public static RectTransform MarkedRow(Transform parent, bool on, string text, int size,
                                          Color textColor, Color onColor, Color offColor)
    {
        var row = Rect(parent, "Marked");
        var stack = HStack(row, 14f, 0, TextAnchor.MiddleLeft);
        stack.childForceExpandWidth = false;
        // Without this the pip is stretched to the full row height and reads as a bar.
        stack.childForceExpandHeight = false;
        stack.childControlHeight = false;

        var pip = Panel(row, "Pip", on ? onColor : offColor);
        pip.rectTransform.sizeDelta = new Vector2(size * 0.75f, size * 0.75f);
        Size(pip, size * 0.75f, size * 0.75f, 0f, 0f);

        var label = Label(row, text, size, textColor, TextAnchor.MiddleLeft);
        Size(label, -1f, -1f, 1f, 1f);
        return row;
    }

    // --- Layout ---

    public static RectTransform Fill(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
        return rt;
    }

    public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, int pad = 0,
                                             TextAnchor align = TextAnchor.UpperCenter)
    {
        var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        g.spacing = spacing;
        g.padding = new RectOffset(pad, pad, pad, pad);
        g.childAlignment = align;
        g.childForceExpandWidth = true;
        g.childForceExpandHeight = false;
        g.childControlWidth = true;
        g.childControlHeight = true;
        return g;
    }

    /// <param name="padV">
    /// Vertical padding, when it should differ from the horizontal. A bar needs a wide
    /// inset to clear its brass end caps but almost none top to bottom - applying the
    /// horizontal figure to all four sides crushes the row's usable height.
    /// </param>
    public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, int pad = 0,
                                               TextAnchor align = TextAnchor.MiddleCenter,
                                               int padV = -1)
    {
        var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        g.spacing = spacing;
        int v = padV >= 0 ? padV : pad;
        g.padding = new RectOffset(pad, pad, v, v);
        g.childAlignment = align;
        g.childForceExpandWidth = true;
        g.childForceExpandHeight = true;
        g.childControlWidth = true;
        g.childControlHeight = true;
        return g;
    }

    /// <summary>
    /// Attach layout sizing to anything living inside a layout group.
    ///
    /// Giving a preferred size also pins the matching flexible size to 0 unless one is
    /// passed explicitly. Without that, a row that is itself a layout group reports a
    /// flexible size of 1 (because HStack force-expands its own children), so every fixed
    /// row would quietly compete for the leftover space meant for the one flexible panel.
    /// </summary>
    public static T Size<T>(T c, float h = -1f, float w = -1f, float flexW = -1f, float flexH = -1f)
        where T : Component
    {
        var le = c.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();

        if (h >= 0f)
        {
            le.preferredHeight = h;
            if (flexH < 0f) le.flexibleHeight = 0f;
        }
        if (w >= 0f)
        {
            le.preferredWidth = w;
            if (flexW < 0f) le.flexibleWidth = 0f;
        }
        if (flexW >= 0f) le.flexibleWidth = flexW;
        if (flexH >= 0f) le.flexibleHeight = flexH;
        return c;
    }

    /// <summary>Scrollable content area. Returns the content rect to parent children into.</summary>
    public static RectTransform Scroll(Transform parent, string name, bool horizontal, float spacing, int pad = 0)
    {
        var viewport = Panel(parent, name, new Color(0f, 0f, 0f, 0f));
        // The viewport must fill its parent. Without this it keeps a default zero-size
        // rect, the RectMask2D clips everything to nothing, and the scroll area renders
        // empty - which is exactly what the Evidence list and the carousel were doing.
        Fill(viewport.rectTransform);
        var sr = viewport.gameObject.AddComponent<ScrollRect>();
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = Rect(viewport.transform, "Content");
        if (horizontal)
        {
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            HStack(content, spacing, pad);
        }
        else
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            VStack(content, spacing, pad);
        }

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;

        sr.content = content;
        sr.viewport = viewport.rectTransform;
        sr.horizontal = horizontal;
        sr.vertical = !horizontal;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.08f;
        sr.inertia = true;
        sr.decelerationRate = 0.12f;
        sr.scrollSensitivity = 30f;
        return content;
    }

    /// <summary>
    /// Reference-space width this device gives us. The scaler matches height, so the
    /// vertical budget is always RefH and only the width varies: roughly 860 units on a
    /// tall 20:9 Android, 1080 on a 16:9 phone, 1440 on a 4:3 iPad.
    /// </summary>
    public static float ReferenceWidth()
    {
        if (Screen.height <= 0) return RefW;
        return Screen.width * RefH / Screen.height;
    }

    /// <summary>
    /// Full-screen wood backdrop plus the content column every screen builds into.
    /// <see cref="MDContentColumn"/> owns the sizing and keeps it correct as the canvas
    /// changes, so nothing here depends on Screen values being right at Awake.
    /// </summary>
    public static RectTransform Screen_(Canvas canvas, string name)
    {
        // Wood underneath, so any part of the screen the photo does not reach still reads
        // as the desk rather than as a gap.
        var backdrop = Panel(canvas.transform, name + "_BG", Wood);
        Fill(backdrop.rectTransform);

        if (MDSkin.Background != null)
        {
            // The cover-scaled photo is deliberately larger than the screen, so clip it.
            backdrop.gameObject.AddComponent<RectMask2D>();

            var photo = Panel(backdrop.transform, "Backdrop", Color.white);
            Fill(photo.rectTransform);
            photo.sprite = MDSkin.Background;
            // The source art is portrait but not the same aspect as every device, so it is
            // cropped to fill rather than letterboxed or squashed.
            photo.type = Image.Type.Simple;
            photo.preserveAspect = false;
            photo.gameObject.AddComponent<MDAspectFill>();

            // Darken it hard. The study photo is bright around the windows, and cream text
            // sitting directly on it was unreadable on the menu.
            var veil = Panel(backdrop.transform, "Veil", new Color(0.08f, 0.05f, 0.02f, 0.78f));
            Fill(veil.rectTransform);
            veil.raycastTarget = false;
        }

        var column = Rect(backdrop.transform, name);
        column.gameObject.AddComponent<MDContentColumn>();
        return column;
    }

    /// <summary>
    /// Empties a container before it is rebuilt. Children are detached first because
    /// Destroy is deferred to end of frame, and a layout group would otherwise lay out
    /// the doomed children alongside the new ones for a frame.
    /// </summary>
    public static void ClearChildren(RectTransform rt)
    {
        for (int i = rt.childCount - 1; i >= 0; i--)
        {
            var child = rt.GetChild(i);
            child.SetParent(null, false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    public static string Money(float v) => (v < 0f ? "-$" : "$") + Mathf.Abs(v).ToString("N0");
    public static string Money2(float v) => (v < 0f ? "-$" : "$") + Mathf.Abs(v).ToString("N2");
    public static string Pct(float p) => (p >= 0f ? "+" : "") + p.ToString("0.0") + "%";
    public static Color PnLColor(float v) => v > 0.0001f ? Good : v < -0.0001f ? Bad : Muted;
}
