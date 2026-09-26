using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

/// <summary>
/// CBG(クライアント背景。CBGSETG/CBGSETSPRITE/CBGSETBUTTONSPRITE/CBGSETBMAPG)を描く層。
///
/// エミュレータ側は一覧を持っていたが、Unity側に描く処理が無く、
/// 命令は成功するのに画面には何も出ていなかった。
///
/// 本家(Emuera.NET/EE)のOnPaintと同じ配置にしている。
/// 座標は「描画領域の左下が原点、下向きが正」で、x,yは画像の左下の位置
/// (本家は左上を (x, y + 描画領域の高さ - 画像の高さ) に描く)。
/// zdepthが正なら文字の奥、負なら文字の手前に描き、値が大きいほど奥。
/// スクロールには付いて行かない(画面に固定)。
///
/// ボタンの判定は本家と同じくボタンマップ(CBGSETBMAPG)だけで行い、
/// 番号はINPUTMOUSEKEYのRESULT:4へ渡す。マップは左下に接するよう置いた物として扱う。
/// 通常のINPUT待ちではCBGを押しても入力にならない(本家も同じ)
/// </summary>
public class CBGLayer : MonoBehaviour
{
    public static CBGLayer instance { get; private set; }

    RectTransform back_;
    RectTransform front_;
    int version_ = -1;
    EmueraConsole console_ = null;

    struct Shown
    {
        public UnityEngine.UI.Image image;
        public EmueraImage.ImageInfo info;
        public EmueraConsole.CBGItem item;
    }
    readonly List<Shown> shown_ = new List<Shown>();
    MinorShift.Emuera.Content.GraphicsImage button_map_ = null;

    /// <summary>
    /// EmueraContentの子として奥と手前の2枚の層を作る
    /// </summary>
    public static void Create(RectTransform content)
    {
        if (instance != null)
            return;
        var obj = new GameObject("CBGLayer");
        obj.transform.SetParent(content, false);
        instance = obj.AddComponent<CBGLayer>();
        instance.back_ = MakeLayer("CBGBack", content);
        instance.front_ = MakeLayer("CBGFront", content);
        //奥の層は文字や行内画像より先に描く。背景色(Background)は独自のCanvasで最背面にいる
        instance.back_.SetAsFirstSibling();
        instance.front_.SetAsLastSibling();
    }

    static RectTransform MakeLayer(string name, RectTransform parent)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = Vector2.zero;   //左下を原点にする
        return rt;
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
        if (console == console_ && console.CBGVersion == version_)
            return;
        console_ = console;
        version_ = console.CBGVersion;
        Rebuild(console);
    }

    void Rebuild(EmueraConsole console)
    {
        ClearShown();
        var list = console.GetCBGSnapshot(out button_map_);
        //一覧は奥(zdepthが大きい)から手前の順。その順に子にすれば後の物ほど手前に描かれる
        for (int i = 0; i < list.Count; ++i)
        {
            var item = list[i];
            if (item.Img == null || !item.Img.IsCreated)
                continue;
            var parent = item.ZDepth > 0 ? back_ : front_;
            var image = EmueraContent.instance.PullImage();
            image.raycastTarget = false;   //当たり判定は自前で行う(ボタンマップは見えないので)
            var rt = (RectTransform)image.transform;
            rt.SetParent(parent, false);
            rt.SetAsLastSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;
            var r = ItemRect(item);
            rt.anchoredPosition = new Vector2(r.x, -r.y);
            rt.sizeDelta = new Vector2(r.width, r.height);

            var info = image.gameObject.GetComponent<EmueraImage.ImageInfo>();
            if (info == null)
                info = image.gameObject.AddComponent<EmueraImage.ImageInfo>();
            info.Load(item.Img);
            shown_.Add(new Shown { image = image, info = info, item = item });
        }
    }

    /// <summary>左下原点・下向き正の座標系での、画像の左上と大きさ</summary>
    static Rect ItemRect(EmueraConsole.CBGItem item)
    {
        var size = item.Img.DestBaseSize;
        float x = item.X;
        float y = item.Y - size.Height;   //x,yは画像の左下
        //csvで位置のずらしを明示したスプライトはその分だけずらす
        if (item.Img.HasExplicitPosition)
        {
            x += item.Img.DestBasePosition.X;
            y += item.Img.DestBasePosition.Y;
        }
        return new Rect(x, y, size.Width, size.Height);
    }

    void ClearShown()
    {
        for (int i = 0; i < shown_.Count; ++i)
        {
            var s = shown_[i];
            if (s.image == null)
                continue;
            //プールへ返す前に、行内画像が前提にしている左上基準へ戻す
            var rt = (RectTransform)s.image.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            if (s.info != null)
                s.info.Clear();   //参照を返してプールへ戻す
            else
                EmueraContent.instance.PushImage(s.image);
        }
        shown_.Clear();
        button_map_ = null;
    }

    /// <summary>
    /// 画面上の位置にあるCBGのボタン番号(ボタンマップの色)。無ければ-1
    /// </summary>
    public static int HitTest(Vector2 screen_position, Camera camera)
    {
        if (instance == null || instance.back_ == null)
            return -1;
        return instance.HitTestInternal(screen_position, camera);
    }

    int HitTestInternal(Vector2 screen_position, Camera camera)
    {
        if (button_map_ == null)
            return -1;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(back_, screen_position, camera, out local))
            return -1;
        //左下原点・下向き正へ
        float cx = local.x;
        float cy = -local.y;

        var map = button_map_;
        if (map != null && map.IsCreated)
        {
            int mx = (int)cx;
            int my = (int)(cy + map.Height);
            if (mx >= 0 && my >= 0 && mx < map.Width && my < map.Height)
            {
                var c = map.GGetColor(mx, my);
                if (c.A == 255)
                    return c.ToArgb() & 0xFFFFFF;
            }
        }
        return -1;
    }

    /// <summary>
    /// INPUTMOUSEKEY待ちならタップをマウス入力として渡してtrue。
    /// RESULT:4にはボタンマップの番号(無ければ-1)を載せる
    /// </summary>
    internal static bool TryHandleTap(UnityEngine.EventSystems.PointerEventData e,
        MinorShift.Emuera.GameView.ConsoleButtonString tapped = null)
    {
        var console = GlobalStatic.Console;
        if (console == null || e == null || !console.IsWaitingPrimitive)
            return false;
        int button = HitTest(e.position, e.pressEventCamera);
        EmueraThread.instance.InputMouse(
            (int)e.position.x,
            (int)(Screen.height - e.position.y),//左上基準へ
            0x100000,//MouseButtons.Left
            button,
            tapped);
        return true;
    }
}
