using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera;

public class EmueraLine : EmueraBehaviour
{
    void Awake()
    {
        GenericUtils.SetListenerOnClick(gameObject, OnClick);
        click_handler_ = GetComponent<GenericUtils.PointerClickListener>();
    }

    public TMPro.TMP_Text text
    {
        get
        {
            if (text_ == null)
            {
                text_ = GetComponent<TMPro.TMP_Text>();
                text_.maskable = false;
            }
            return text_;
        }
    }
    TMPro.TMP_Text text_ = null;
    public UnityEngine.UI.ContentSizeFitter size_fitter
    {
        get
        {
            if(size_fitter_ == null)
            {
                size_fitter_ = GetComponent<UnityEngine.UI.ContentSizeFitter>();
                size_fitter_.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            }
            return size_fitter_;
        }
    }
    UnityEngine.UI.ContentSizeFitter size_fitter_ = null;
    GenericUtils.PointerClickListener click_handler_ = null;
    uEmuera.TMPMonospaced monospaced_ = null;

#if UNITY_STANDALONE
    public UnityEngine.UI.Button button
    {
        get
        {
            if (button_ == null)
            {
                button_ = GetComponent<UnityEngine.UI.Button>();
                if (button_ == null)
                    button_ = gameObject.AddComponent<UnityEngine.UI.Button>();
                var colors = button_.colors;
                colors.highlightedColor = Config.FocusColor.ToUnityColor();
                button_.colors = colors;
            }
            return button_;
        }
    }
    UnityEngine.UI.Button button_ = null;
#endif

    /// <summary>
    /// 更新内容
    /// </summary>
    public override void UpdateContent()
    {
        var ud = unit_desc;

        //<font>タグで参照されるフォントは、TMPへ登録されていないと
        //タグが解決できずそのまま文字として表示される。
        //本文を渡す前にここで用意しておく(メインスレッド)
        if(ud.extra_fonts != null)
        {
            for(int i = 0; i < ud.extra_fonts.Count; ++i)
            {
                if(uEmuera.TMPFonts.Get(ud.extra_fonts[i]) != null)
                    continue;
                //端末にもゲームのfont/にも無いフォント(Times New Romanなど)。
                //タグを残すと「<font="Times New Roman">」がそのまま文字で出るので、
                //タグだけ外して本文のフォント(と連鎖)で描かせる。
                //区間は入れ子にならないので、開きタグから最初の閉じタグまでを外せばよい
                if(ud.content != null)
                {
                    var open = "<font=\"" + ud.extra_fonts[i] + "\">";
                    ud.content = System.Text.RegularExpressions.Regex.Replace(ud.content,
                        System.Text.RegularExpressions.Regex.Escape(open) + "(.*?)</font>",
                        "$1", System.Text.RegularExpressions.RegexOptions.Singleline);
                }
                ud.extra_fonts.RemoveAt(i);
                --i;
            }
        }

        text.text = ud.content;
        if (text.text == ">> " || text.text == ">>")
        {
            text.text = "";
        }
        //text.alignment = (TextAnchor)line_desc.align;
        //if((int)text.alignment > 0)
        //    ud.posx = 0;
        text.color = ud.color;
        text.richText = ud.richedit;

        if(ud.isbutton && ud.generation >= EmueraContent.instance.button_generation)
        {
            click_handler_.enabled = true;
            text.raycastTarget = true;
#if UNITY_STANDALONE
            button.enabled = true;
#endif
#if UNITY_EDITOR
            code = ud.code;
            generation = ud.generation;
#endif
        }
        else
        {
            click_handler_.enabled = false;
            text.raycastTarget = false;
#if UNITY_STANDALONE
            button.enabled = false;
#endif
        }

        //フォントはTMP_FontAssetで指定する。ゲームフォルダのフォントも扱えるよう
        //TMPFontsが名前から解決する
        var fontname = ud.fontname;
        if(string.IsNullOrEmpty(fontname))
            fontname = MinorShift.Emuera.Config.FontName;
        var font = uEmuera.TMPFonts.GetOrDefault(fontname);
        if(font != null && text.font != font)
            text.font = font;

        var consoleline = line_desc.console_line as MinorShift.Emuera.GameView.ConsoleDisplayLine;
        var cb = consoleline.Buttons[UnitIdx];
        int miny = int.MaxValue;
        for(int i = 0; i < cb.StrArray.Length; ++i)
        {
            miny = System.Math.Min(miny, cb.StrArray[i].Top);
        }
        if (miny == int.MaxValue) miny = 0;

        logic_y = line_desc.position_y + miny;
        if (ud.absolute_posy) logic_y += ud.posy;
        logic_height = line_desc.height;


        if (cb.Depth != 0)
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = -cb.Depth;
            var graphicRaycaster = gameObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (graphicRaycaster == null) gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas != null) { canvas.overrideSorting = false; canvas.sortingOrder = 0; }
        }

        var sizefitter = false;
        if(ud.isbutton || line_desc.units.Count > 1)
            sizefitter = true;
        else
            sizefitter = false;
        //行は使い回されるのでverticalFitは毎回戻す。下のdiv判定で変更されることがある
        size_fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        if(sizefitter)
        {
            size_fitter.horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            size_fitter.horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            text_.rectTransform.sizeDelta = new Vector2(Width, 0);
        }

        //中身がdivだけのボタンはテキストが空で当たり判定が潰れてしまう。
        //div全体を覆うよう矩形を広げる。描画を親に任せている行(skip_div_render_)でも必要
        //空白だけのテキストも字形を持たないので同じ問題が起きる(PRINTBUTTON " " * 80 など)
        bool blank_button = ud.isbutton
            && ud.generation >= EmueraContent.instance.button_generation
            && string.IsNullOrEmpty((text.text ?? "").Trim());
        if(blank_button)
        {
            int left = int.MaxValue, right = int.MinValue;
            int top = int.MaxValue, bottom = int.MinValue;
            if(ud.div_parts != null)
            {
                for(int i = 0; i < ud.div_parts.Count; ++i)
                {
                    var dp = ud.div_parts[i];
                    if(dp == null)
                        continue;
                    //width/heightが指定されていないdivは内容から大きさを求める。
                    //dp.WidthはテキストのながれのWidth(常に0)なのでContentWidthを使う
                    int w = dp.width > 0 ? dp.width : dp.ContentWidth;
                    int h = dp.Height > 0
                        ? dp.Height
                        : (dp.Children != null
                            ? dp.Children.Length * MinorShift.Emuera.Config.LineHeight : 0);
                    if(w <= 0 || h <= 0)
                        continue;
                    int x0 = dp.PointX - ud.posx;
                    if(x0 < left) left = x0;
                    if(x0 + w > right) right = x0 + w;
                    if(dp.PointY < top) top = dp.PointY;
                    if(dp.PointY + h > bottom) bottom = dp.PointY + h;
                }
            }
            if(left > right && Width > 0)
            {
                //divを持たない空白ボタンは1行分の領域を当たり判定にする
                left = 0; right = (int)Width;
                top = 0; bottom = MinorShift.Emuera.Config.LineHeight;
            }
            if(left <= right && top <= bottom)
            {
                size_fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                size_fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                text_.rectTransform.sizeDelta = new Vector2(right - left, bottom - top);
                SetHitArea(left, -top, right - left, bottom - top);
            }
            else
                HideHitArea();
        }
        else
            HideHitArea();

        //下線・打ち消し線は指定された区間の上だけへ引く。
        //以前は行のどこかに指定があると行頭から行末まで引いていたため、
        //一部だけ下線を付ける画面(FONTSTYLE 8)で長い線が伸びていた。
        //色もその区間の色を使う。行の基準色では見た目が変わってしまう
        if(unit_desc.style_spans != null)
        {
            for(int i = 0; i < unit_desc.style_spans.Count; ++i)
            {
                var sp = unit_desc.style_spans[i];
                if(sp.w <= 0f)
                    continue;
                if(sp.under)
                    AddDivRect(sp.x, -text.fontSize - 1, sp.w, 1, sp.color);
                if(sp.strike)
                    AddDivRect(sp.x, -text.fontSize / 2.0f, sp.w, 1, sp.color);
            }
        }

        if (unit_desc.has_bgcolor && unit_desc.bgcolor.a > 0f && unit_desc.bgcolor.a < 1f)
        {
            if (bgcolor_ == null)
            {
                var obj = GameObject.Instantiate(EmueraContent.instance.template_block.gameObject);
#if UNITY_EDITOR
                obj.name = "bgcolor";
#endif
                bgcolor_ = obj.GetComponent<RectTransform>();
                bgcolor_.transform.SetParent(this.transform.parent, false);
                bgcolor_.anchorMin = new Vector2(0, 1);
                bgcolor_.anchorMax = new Vector2(0, 1);
                bgcolor_.pivot = new Vector2(0, 1);
                bgcolor_.localScale = Vector3.one;
            }
            bgcolor_.transform.SetSiblingIndex(this.transform.GetSiblingIndex());
            bgcolor_.sizeDelta = new Vector2(Width, MinorShift.Emuera.Config.LineHeight);
            bgcolor_.GetComponent<UnityEngine.UI.Image>().color = unit_desc.bgcolor;
            bgcolor_.gameObject.SetActive(true);
        }
        else if (bgcolor_ != null)
        {
            bgcolor_.gameObject.SetActive(false);
        }

        //<shape>の矩形(HPゲージなど)。テキストを持たないので個別に描く
        if(ud.shape_parts != null && ud.shape_parts.Count > 0)
        {
            for(int i = 0; i < ud.shape_parts.Count; ++i)
            {
                var sp = ud.shape_parts[i];
                if(sp == null || !sp.pVisible)
                    continue;
                var r = sp.pRect;
                if(r.Width <= 0 || r.Height <= 0)
                    continue;
                AddDivRect(sp.PointX - ud.posx + r.X, -(r.Y) + miny,
                    r.Width, r.Height, GenericUtils.ToUnityColor(sp.pColor));
            }
        }

        // div processing
        // divの中身として作られた行なら、入れ子のdivは呼び出し元(RenderDivPart)が
        // 積み上げたyBaseで描画する。ここで描くと二重描画になる
        if (!skip_div_render_ && ud.div_parts != null && ud.div_parts.Count > 0)
        {
            //子はこのEmueraLineの子オブジェクトになる。
            //この行自体が既にud.posxへ置かれているので、その分を引かないと二重にずれる
            foreach (var dp in ud.div_parts)
                RenderDivPart(dp, 0f, miny, 0, ud.posx);
        }

        //代替フォントが描いた文字は送り幅が僅かに違い、長い表では列がずれる。
        //エミュレータが座標計算に使ったのと同じ幅で格子へ揃え直す。
        //TMPが組み直す度にかけ直す必要があるのでコンポーネントに任せる
        if(monospaced_ == null)
            monospaced_ = gameObject.GetComponent<uEmuera.TMPMonospaced>()
                ?? gameObject.AddComponent<uEmuera.TMPMonospaced>();
        monospaced_.fontname = fontname;
        monospaced_.apply = ud.monospaced;
        text.SetVerticesDirty();

#if UNITY_EDITOR
        gameObject.name = string.Format("line:{0}:{1}", LineNo, UnitIdx);
#endif
        gameObject.SetActive(true);
    }

    /// <summary>
    /// divとその中身を描画する。中身にdivが含まれていれば再帰する。
    /// yBase は外側のdivから積み上がった縦オフセット
    /// </summary>
    /// <summary>
    /// この行がdivの中身として描画されている間はtrue。
    /// 入れ子のdivを二重に描かないための目印
    /// </summary>
    internal bool skip_div_render_ = false;

    /// <summary>
    /// 中身がdivだけのボタンは、テキストが空で何も描画されないため
    /// GraphicRaycasterがdepth==-1として無視してしまう。
    /// ほぼ透明(alpha 1/255)な実体のあるGraphicを敷いてクリックを拾う。
    /// alphaを完全な0にすると描画自体が省かれ、同じく拾えなくなる
    /// </summary>
    void SetHitArea(float x, float y, float w, float h)
    {
        if(hit_area_ == null)
        {
            var obj = GameObject.Instantiate(EmueraContent.instance.template_block.gameObject);
            obj.name = "hit_area";//ビルドでもログで判別できるよう常に付ける
            hit_area_ = obj.GetComponent<RectTransform>();
            hit_area_.SetParent(this.transform, false);
            hit_area_.anchorMin = new Vector2(0, 1);
            hit_area_.anchorMax = new Vector2(0, 1);
            hit_area_.pivot = new Vector2(0, 1);
            hit_area_.localScale = Vector3.one;
            hit_area_img_ = hit_area_.GetComponent<UnityEngine.UI.Image>();
            hit_area_img_.raycastTarget = true;
            //EmueraContentのRectMask2Dに刈られるとcullが立ち、レイキャスト対象から外れる。
            //テキストと同じくマスクの対象外にする
            hit_area_img_.maskable = false;
        }
        if(hit_area_img_ != null)
        {
            hit_area_img_.enabled = true;//テンプレート側で無効になっていることがある
            //alphaを完全な0にすると描画が省かれ、depth==-1でレイキャスト対象から外れる
            hit_area_img_.color = new UnityEngine.Color(0f, 0f, 0f, 1f / 255f);
        }
        hit_area_.SetAsFirstSibling();//文字より後ろに置いて表示を邪魔しない
        hit_area_.anchoredPosition = new Vector2(x, y);
        hit_area_.sizeDelta = new Vector2(w, h);
        hit_area_.gameObject.SetActive(true);
    }

    void HideHitArea()
    {
        if(hit_area_ != null)
            hit_area_.gameObject.SetActive(false);
    }

    /// <summary>div用の矩形を1枚作る。背景と枠線で共用</summary>
    void AddDivRect(float x, float y, float w, float h, UnityEngine.Color color)
    {
        var obj = GameObject.Instantiate(EmueraContent.instance.template_block.gameObject);
#if UNITY_EDITOR
        obj.name = "div";
#endif
        var rt = obj.GetComponent<RectTransform>();
        rt.transform.SetParent(this.transform, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        var img = rt.GetComponent<UnityEngine.UI.Image>();
        img.enabled = true;//テンプレート側で無効になっていることがある
        img.color = color;
        img.raycastTarget = false;//背景・枠線はクリックを奪わない
        img.maskable = false;//テキストと同様、RectMask2Dの対象外にする
        rt.gameObject.SetActive(true);
        div_bgs_.Add(rt);
    }

    /// <summary>枠線の色。未指定なら文字色を使う</summary>
    static UnityEngine.Color DivBorderColor(MinorShift.Emuera.GameView.ConsoleDivPart dp, int side)
    {
        if (dp.borderColors != null && dp.borderColors[side].A > 0)
        {
            var c = dp.borderColors[side];
            return new UnityEngine.Color(c.r, c.g, c.b, c.a);
        }
        return GenericUtils.ToUnityColor(MinorShift.Emuera.Config.ForeColor);
    }

    void RenderDivPart(MinorShift.Emuera.GameView.ConsoleDivPart dp, float yBase, int miny, int depth, int hostX)
    {
        if (dp == null || dp.Children == null)
            return;



        int margin_left = dp.margin != null ? dp.margin[0] : 0;
        int margin_top = dp.margin != null ? dp.margin[1] : 0;
        int margin_right = dp.margin != null ? dp.margin[2] : 0;
        int margin_bottom = dp.margin != null ? dp.margin[3] : 0;

        //display='absolute-*'は現在行ではなく画面端が縦の基準になる。
        //この行にぶら下げて描くので、行の位置(logic_y)との差を打ち消しておく
        float base_y = yBase + dp.PointY;
        if(depth == 0 && dp.Anchor != MinorShift.Emuera.GameView.DivAnchor.Relative)
        {
            float anchor = dp.Anchor == MinorShift.Emuera.GameView.DivAnchor.AbsoluteBottom
                ? EmueraContent.instance.ContentHeight : 0f;
            base_y = (anchor + dp.PointY) - logic_y + miny;
        }

        //PointXは既にxOffsetを取り込んだ絶対位置なので足し直さない
        float box_x = dp.PointX + margin_left - hostX;
        float box_y = -(base_y + margin_top) + miny;
        float box_w = dp.width - margin_left - margin_right;
        float box_h = dp.Height - margin_top - margin_bottom;

        if (dp.backgroundColor.A > 0)
        {
            AddDivRect(box_x, box_y, box_w, box_h, new UnityEngine.Color(
                dp.backgroundColor.r, dp.backgroundColor.g, dp.backgroundColor.b, dp.backgroundColor.a));
        }

        //枠線。border_width/border_colorで指定された分を箱の内側に描く
        if (dp.border != null && box_w > 0 && box_h > 0)
        {
            int bl = dp.border[0], bt = dp.border[1], br = dp.border[2], bb = dp.border[3];
            if (bl > 0) AddDivRect(box_x, box_y, bl, box_h, DivBorderColor(dp, 0));
            if (bt > 0) AddDivRect(box_x, box_y, box_w, bt, DivBorderColor(dp, 1));
            if (br > 0) AddDivRect(box_x + box_w - br, box_y, br, box_h, DivBorderColor(dp, 2));
            if (bb > 0) AddDivRect(box_x, box_y - (box_h - bb), box_w, bb, DivBorderColor(dp, 3));
        }

        for (int chi = 0; chi < dp.Children.Length; chi++)
        {
            var childDisplayLine = dp.Children[chi];
            if (childDisplayLine == null)
                continue;
            var ld = new LineDesc(childDisplayLine, 0, 0);
            ld.Update();

            float current_y = base_y + dp.yOffset + (chi * MinorShift.Emuera.Config.LineHeight);

            for (int li = 0; li < ld.units.Count; ++li)
            {
                var childUnit = ld.units[li];
                EmueraLine child_line = null;
                if (!childUnit.empty)
                {
                    var lc = EmueraContent.instance.PullLine();
                    child_line = lc;
                    lc.line_desc = ld;
                    lc.UnitIdx = li;
                    lc.Width = childUnit.width;
                    lc.skip_div_render_ = true;//入れ子のdivは下で自分が描画する
                    lc.UpdateContent();

                    lc.transform.SetParent(this.transform, false);
                    lc.SetPosition(childUnit.posx + margin_left - hostX, -lc.logic_y - current_y + miny);


                    div_lines_.Add(lc);
                }
                if (childUnit.image_indices != null && childUnit.image_indices.Count > 0)
                {
                    EmueraImage ic = EmueraContent.instance.PullImageContainer();
                    ic.line_desc = ld;
                    ic.UnitIdx = li;
                    ic.UpdateContent();

                    ic.transform.SetParent(this.transform, false);
                    //<img>のyposはUpdateContentがlogic_yへ畳み込んでいる。
                    //上の行と同じくlogic_yを使わないと、ypos指定が全て捨てられて重なる
                    ic.SetPosition(childUnit.posx - hostX, -ic.logic_y - current_y + miny);


                    div_images_.Add(ic);
                }
                //入れ子のdivを同じ手順で描画する
                if (childUnit.div_parts != null && childUnit.div_parts.Count > 0)
                {
                    foreach (var nested in childUnit.div_parts)
                        RenderDivPart(nested, current_y, miny, depth + 1, hostX);
                }
            }
        }
    }

    public void Clear()
    {
        skip_div_render_ = false;
        foreach (var l in div_lines_) EmueraContent.instance.PushLine(l);
        div_lines_.Clear();
        foreach (var img in div_images_) EmueraContent.instance.PushImageContainer(img);
        div_images_.Clear();

        foreach (var bg in div_bgs_)
        {
            if (bg != null) GameObject.Destroy(bg.gameObject);
        }
        div_bgs_.Clear();

        line_desc = null;
        UnitIdx = -1;
        text.text = "";
        if(bgcolor_ != null)
            bgcolor_.gameObject.SetActive(false);
        HideHitArea();
    }
    public override void SetPosition(float x, float y)
    {
        base.SetPosition(x, y);
        if (bgcolor_ != null) bgcolor_.anchoredPosition = new Vector2(x, y);
    }
    RectTransform hit_area_ = null;
    UnityEngine.UI.Image hit_area_img_ = null;
    RectTransform bgcolor_ = null;
    List<RectTransform> div_bgs_ = new List<RectTransform>();
    List<EmueraLine> div_lines_ = new List<EmueraLine>();
    List<EmueraImage> div_images_ = new List<EmueraImage>();
}
