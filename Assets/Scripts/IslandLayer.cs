using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

/// <summary>
/// HTML_PRINT_ISLANDの表示層。
///
/// 本家(Emuera.NET)はISLANDのHTMLを通常の行と同じConsoleDisplayLineへ変換し、
/// ログにもスクロールにも関係なく、描画領域の上端から1行(LineHeight)ずつ、
/// 他の全ての描画(文字・CBG)の上に重ねて描く。押しても反応しない。
///
/// ここでも行の描画は通常の行と同じ部品(EmueraLine/EmueraImage)を使い、
/// スクロールしない専用の入れ物に置く。部品は通常の行のプールとは分けて持つ。
/// (EmueraLineは作られた時の親に背景色の矩形をぶら下げるため、混ぜると
/// 通常の行の背景が最前面に出てしまう)
/// </summary>
public class IslandLayer : MonoBehaviour
{
    public static IslandLayer instance { get; private set; }

    RectTransform text_root_;
    RectTransform image_root_;
    int version_ = -1;
    EmueraConsole console_ = null;

    readonly List<EmueraLine> lines_used_ = new List<EmueraLine>();
    readonly Stack<EmueraLine> lines_free_ = new Stack<EmueraLine>();
    readonly List<EmueraImage> images_used_ = new List<EmueraImage>();
    readonly Stack<EmueraImage> images_free_ = new Stack<EmueraImage>();

    public static void Create(EmueraContent content)
    {
        if (instance != null || content == null)
            return;
        var obj = new GameObject("IslandLayer");
        obj.transform.SetParent(content.transform, false);
        instance = obj.AddComponent<IslandLayer>();
        //通常の行の入れ物と同じ配置にしておけば、同じ座標計算がそのまま使える
        instance.image_root_ = CopyLayout(content.image_content, "IslandImages");
        instance.text_root_ = CopyLayout(content.text_content, "IslandText");
        //最前面(CBGの手前の層よりも上)
        instance.image_root_.SetAsLastSibling();
        instance.text_root_.SetAsLastSibling();
    }

    static RectTransform CopyLayout(RectTransform src, string name)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.SetParent(src.parent, false);
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = src.sizeDelta;
        rt.localScale = Vector3.one;
        //本家のISLANDは押しても反応しない。下の行へのタップも遮らない
        var group = obj.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return rt;
    }

    void Update()
    {
        var console = GlobalStatic.Console;
        if (console == null)
        {
            if (console_ != null)
                ReleaseAll();
            console_ = null;
            version_ = -1;
            return;
        }
        if (console == console_ && console.IslandVersion == version_)
            return;
        console_ = console;
        version_ = console.IslandVersion;
        Rebuild(console);
    }

    void Rebuild(EmueraConsole console)
    {
        ReleaseAll();
        var lines = console.GetIslandSnapshot();
        float line_height = Config.LineHeight;
        for (int i = 0; i < lines.Length; ++i)
        {
            if (lines[i] == null)
                continue;
            //上端から1行ずつ下へ(本家は y = 0, LineHeight, 2*LineHeight ...)
            var ld = new EmueraBehaviour.LineDesc(lines[i], i * line_height, line_height);
            ld.Update();
            if (ld.units == null)
                continue;
            for (int li = 0; li < ld.units.Count; ++li)
            {
                var unit = ld.units[li];
                if (!unit.empty)
                {
                    var lc = GetLine();
                    lc.line_desc = ld;
                    lc.UnitIdx = li;
                    lc.Width = unit.width;
                    lc.UpdateContent();
                    lc.SetPosition(unit.posx, -lc.logic_y);
                    lines_used_.Add(lc);
                }
                if (unit.image_indices != null && unit.image_indices.Count > 0)
                {
                    var ic = GetImage();
                    ic.line_desc = ld;
                    ic.UnitIdx = li;
                    ic.Width = unit.width;
                    ic.UpdateContent();
                    ic.SetPosition(unit.posx, -ic.logic_y);
                    images_used_.Add(ic);
                }
            }
        }
    }

    EmueraLine GetLine()
    {
        EmueraLine line;
        if (lines_free_.Count > 0)
            line = lines_free_.Pop();
        else
        {
            var obj = GameObject.Instantiate(EmueraContent.instance.template_text.gameObject);
            line = obj.GetComponent<EmueraLine>();
            line.transform.SetParent(text_root_, false);
        }
        line.transform.localScale = Vector3.one;
        line.gameObject.SetActive(true);
        return line;
    }

    EmueraImage GetImage()
    {
        EmueraImage image;
        if (images_free_.Count > 0)
            image = images_free_.Pop();
        else
        {
            var obj = GameObject.Instantiate(EmueraContent.instance.template_images.gameObject);
            image = obj.GetComponent<EmueraImage>();
            image.transform.SetParent(image_root_, false);
        }
        image.transform.localScale = Vector3.one;
        image.gameObject.SetActive(true);
        return image;
    }

    void ReleaseAll()
    {
        for (int i = 0; i < lines_used_.Count; ++i)
        {
            var l = lines_used_[i];
            if (l == null)
                continue;
            l.Clear();
            l.gameObject.SetActive(false);
            lines_free_.Push(l);
        }
        lines_used_.Clear();
        for (int i = 0; i < images_used_.Count; ++i)
        {
            var img = images_used_[i];
            if (img == null)
                continue;
            img.Clear();
            img.gameObject.SetActive(false);
            images_free_.Push(img);
        }
        images_used_.Clear();
    }
}
