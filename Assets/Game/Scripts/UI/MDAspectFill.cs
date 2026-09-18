using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales a background image to cover its parent, cropping the overflow instead of
/// squashing it. The equivalent of CSS `object-fit: cover`.
///
/// The background art is portrait but not the same aspect as every target device, so a
/// plain stretch visibly distorts the furniture. Recomputes on resize, so it is correct on
/// every Game view preset and on device.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class MDAspectFill : MonoBehaviour
{
    RectTransform _rt;
    Image _image;
    bool _applying;

    void OnEnable()
    {
        _rt = (RectTransform)transform;
        _image = GetComponent<Image>();
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        if (_rt != null) Apply();
    }

    void Apply()
    {
        if (_applying) return;
        var parent = _rt.parent as RectTransform;
        if (parent == null || _image == null || _image.sprite == null) return;

        float pw = parent.rect.width, ph = parent.rect.height;
        if (pw < 1f || ph < 1f) return;

        var size = _image.sprite.rect.size;
        if (size.x < 1f || size.y < 1f) return;

        // Cover: scale by whichever axis needs the most, so neither axis is left short.
        float scale = Mathf.Max(pw / size.x, ph / size.y);

        _applying = true;
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = size * scale;
        _rt.anchoredPosition = Vector2.zero;
        _applying = false;
    }
}
