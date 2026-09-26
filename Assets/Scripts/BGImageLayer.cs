using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

/// <summary>
/// SETBGIMAGEの背景画像を描く層。
///
/// 本家(EE)のBakeBackgroundと同じ配置にしている。
/// 深さの大きい物から順に、描画領域の幅に合わせて拡大し、
/// それで高さが足りなければ高さに合わせる(隙間なく覆う)。上端を揃え、左右は中央に置く。
/// 文字やCBGよりも奥、背景色の手前に描き、スクロールには付いて行かない。押しても反応しない
/// </summary>
public class BGImageLayer : MonoBehaviour
{
    public static BGImageLayer instance { get; private set; }

    RectTransform root_;
    int version_ = -1;
    Vector2 size_ = Vector2.zero;
    EmueraConsole console_ = null;

    struct Shown
    {
        public UnityEngine.UI.Image image;
        public EmueraImage.ImageInfo info;
    }
    readonly List<Shown> shown_ = new List<Shown>();

    public static void Create(RectTransform content)
    {
        if (instance != null)
            return;
        var obj = new GameObject("BGImageLayer");
        obj.transform.SetParent(content, false);
        instance = obj.AddComponent<BGImageLayer>();

        var layer = new GameObject("BGImages");
        var rt = layer.AddComponent<RectTransform>();
        rt.SetParent(content, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0, 1);   //左上を原点にする
        var group = layer.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        //最背面(CBGの奥の層よりも奥)
        rt.SetAsFirstSibling();
        instance.root_ = rt;
    }

    void Update()
    {
        var console = GlobalStatic.Console;
        if (console == null)
        {
            if (console_ != null || shown_.Count > 0)
                ClearShown();
            console_ = null;
            version_ = -1;
            return;
        }
        var size = root_.rect.size;
        if (console == console_ && console.BGImageVersion == version_ && size == size_)
            return;
        console_ = console;
        version_ = console.BGImageVersion;
        size_ = size;
        Rebuild(console);
    }

    void Rebuild(EmueraConsole console)
    {
        RenderThrottle.Wake();
        ClearShown();
        var list = console.GetBackgroundSnapshot();
        float areaW = size_.x;
        float areaH = size_.y;
        if (areaW <= 0 || areaH <= 0)
            return;
        //一覧は奥(深さが大きい)から手前の順。その順に子にすれば後の物ほど手前に描かれる
        for (int i = 0; i < list.Length; ++i)
        {
            var spr = list[i].Sprite as MinorShift.Emuera.Content.ASpriteSingle;
            if (spr == null || !spr.IsCreated)
                continue;
            //本家は元画像全体の大きさで倍率を決める
            float srcW = spr.DestBaseSize.Width;
            float srcH = spr.DestBaseSize.Height;
            var bmp = spr.Bitmap;
            if (bmp != null && bmp.Width > 0 && bmp.Height > 0)
            {
                srcW = bmp.Width;
                srcH = bmp.Height;
            }
            if (srcW <= 0 || srcH <= 0)
                continue;
            float scaleW = areaW / srcW;
            float scaleH = areaH / srcH;
            bool cropHorizontally = srcH * scaleW < areaH;
            float scale = cropHorizontally ? scaleH : scaleW;
            float w = srcW * scale;
            float h = srcH * scale;
            float x = (int)((areaW - w) / 2);

            var image = EmueraContent.instance.PullImage();
            image.raycastTarget = false;
            var rt = (RectTransform)image.transform;
            rt.SetParent(root_, false);
            rt.SetAsLastSibling();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(x, 0);
            rt.sizeDelta = new Vector2(w, h);

            var info = image.gameObject.GetComponent<EmueraImage.ImageInfo>();
            if (info == null)
                info = image.gameObject.AddComponent<EmueraImage.ImageInfo>();
            info.alpha = Mathf.Clamp01(list[i].Opacity);
            info.Load(spr);
            shown_.Add(new Shown { image = image, info = info });
        }
    }

    void ClearShown()
    {
        for (int i = 0; i < shown_.Count; ++i)
        {
            var s = shown_[i];
            if (s.image == null)
                continue;
            if (s.info != null)
                s.info.Clear();   //参照を返してプールへ戻す
            else
                EmueraContent.instance.PushImage(s.image);
        }
        shown_.Clear();
    }
}
