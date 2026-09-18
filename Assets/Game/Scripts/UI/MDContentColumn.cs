using UnityEngine;

/// <summary>
/// Keeps a screen's content column inside the safe area, inset from the screen edges, and
/// capped so a tablet does not stretch a phone layout across its full width.
///
/// This recomputes whenever the rect changes rather than once at Awake. Deriving the width
/// from Screen.width/Screen.height in Awake looked correct at 1080x1920 and produced a
/// full-bleed, edge-cut layout at 1284x2778, because those values are not reliable that
/// early in the Editor. The parent's own rect always is.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MDContentColumn : MonoBehaviour
{
    public float SideMargin = MDUIKit.SideMargin;
    public float MaxWidth = MDUIKit.MaxColumnWidth;

    RectTransform _rt;
    bool _applying;

    void OnEnable()
    {
        _rt = (RectTransform)transform;
        Apply();
    }

    // Fires when this rect resizes, which includes every time the canvas does.
    void OnRectTransformDimensionsChange()
    {
        if (_rt != null) Apply();
    }

    void Apply()
    {
        if (_applying) return;                       // Apply() resizes us, which re-enters here
        var parent = _rt.parent as RectTransform;    // the full-canvas backdrop
        if (parent == null) return;

        float canvasW = parent.rect.width;
        float canvasH = parent.rect.height;
        if (canvasW < 1f || canvasH < 1f) return;

        // Safe area as fractions of the screen, read live so a notch or a rotation is picked up.
        float xMin = 0f, xMax = 1f, yMin = 0f, yMax = 1f;
        if (Screen.width > 0 && Screen.height > 0)
        {
            var safe = Screen.safeArea;
            if (safe.width > 0f && safe.height > 0f)
            {
                xMin = Mathf.Clamp01(safe.xMin / Screen.width);
                xMax = Mathf.Clamp01(safe.xMax / Screen.width);
                yMin = Mathf.Clamp01(safe.yMin / Screen.height);
                yMax = Mathf.Clamp01(safe.yMax / Screen.height);
            }
        }

        float safeWidth = canvasW * Mathf.Max(0.1f, xMax - xMin);
        float width = Mathf.Min(safeWidth - SideMargin * 2f, MaxWidth);
        if (width < 1f) return;

        float centreX = (xMin + xMax) * 0.5f;

        _applying = true;
        _rt.anchorMin = new Vector2(centreX, yMin);
        _rt.anchorMax = new Vector2(centreX, yMax);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.offsetMin = new Vector2(-width * 0.5f, 0f);
        _rt.offsetMax = new Vector2(width * 0.5f, 0f);
        _applying = false;
    }
}
