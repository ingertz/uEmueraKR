using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

public abstract class EmueraBehaviour : MonoBehaviour
{
    public static void Ready()
    {
        FontSize = Config.FontSize;
        FontColor = GenericUtils.ToUnityColor(Config.ForeColor);
    }
    public static int FontSize { get; private set; }
    public static Color FontColor { get; private set; }

    public enum Align
    {
        LEFT = 0,
        CENTER = 1,
        RIGHT = 2,
    }
    /// <summary>
    /// 下線・打ち消し線を引く区間。行頭からの位置と幅、そしてその区間の色を持つ
    /// </summary>
    public struct StyleSpan
    {
        public float x;
        public float w;
        public Color color;
        public bool under;
        public bool strike;
    }

    public class UnitDesc
    {
        //Text
        public string content
        {
            get { return resource_content; }
            set { resource_content = value; }
        }
        public string fontname
        {
            get { return resource_name; }
            set { resource_name = value; }
        }
        //internal
        string resource_content;
        string resource_name;

        public string code;
        public int generation;
        public int posx;
        public int posy;
        public bool absolute_posy;
        public int relative_posx;
        public int width;
        public int height;
        public Color color;
        public List<int> image_indices;
        /// <summary>&lt;font&gt;タグで参照されるフォント名。素のフォント以外の分</summary>
        public List<string> extra_fonts;
        internal List<MinorShift.Emuera.GameView.ConsoleDivPart> div_parts = new List<MinorShift.Emuera.GameView.ConsoleDivPart>();
        /// <summary>&lt;shape&gt;で描かれる矩形。HPゲージなどに使われる</summary>
        internal List<MinorShift.Emuera.GameView.ConsoleRectangleShapePart> shape_parts = null;
        public Color bgcolor;
        public bool has_bgcolor;
        /// <summary>下線・打ち消し線を引く区間。行の一部にだけ掛かる</summary>
        public List<StyleSpan> style_spans = null;

        public uint flags = 0;
        public bool isbutton
        {
            get { return (flags & 0x1) == 1; }
            set { flags = value ? (flags | 0x1) : (flags & (0xFFFFFFFF ^ 0x1U)); }
        }
        public bool underline
        {
            get { return ((flags >> 1) & 0x1) == 1; }
            set { flags = value ? (flags | (0x1U << 1)) : (flags & (0xFFFFFFFF ^ (0x1U << 1))); }
        }
        public bool strickout
        {
            get { return ((flags >> 2) & 0x1) == 1; }
            set { flags = value ? (flags | (0x1U << 2)) : (flags & (0xFFFFFFFF ^ (0x1U << 2))); }
        }
        public bool empty
        {
            get { return ((flags >> 3) & 0x1) == 1; }
            set { flags = value ? (flags | (0x1U << 3)) : (flags & (0xFFFFFFFF ^ (0x1U << 3))); }
        }
        public bool richedit
        {
            get { return ((flags >> 4) & 0x1) == 1; }
            set { flags = value ? (flags | (0x1U << 4)) : (flags & (0xFFFFFFFF ^ (0x1U << 4))); }
        }
        public bool monospaced
        {
            get { return ((flags >> 5) & 0x1) == 1; }
            set { flags = value ? (flags | (0x1U << 5)) : (flags & (0xFFFFFFFF ^ (0x1U << 5))); }
        }
    }
    public class LineDesc
    {
        public LineDesc(object display, float posy, float h)
        {
            display_line = (ConsoleDisplayLine)display;
            position_y = posy;
            height = h;
            extent_down = h;
        }

        /// <summary>
        /// この行が実際に描画で占める範囲。行高さは常にConfig.LineHeightだが、
        /// 画像やdivはその何倍も上下へはみ出すので別に持つ
        /// </summary>
        public float extent_up = 0;
        public float extent_down = 0;
        /// <summary>全行を通じての最大はみ出し量。走査の打ち切り判定に使う</summary>
        public static float max_extent_up = 0;
        public static float max_extent_down = 0;
        public object console_line { get { return display_line; } }
        ConsoleDisplayLine display_line = null;

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            var count = units.Count;
            for(var i=0; i<count; ++i)
                str.Append(units[i].content);
            return str.ToString();
        }
        public void Update()
        {
            units = new List<UnitDesc>();
            float ext_up = 0;
            float ext_down = height;

            var Buttons = display_line.Buttons;
            for(int i = 0; i < Buttons.Length; ++i)
            {
                var btn = Buttons[i];
                var ud = new UnitDesc();
                var fontname = FontUtils.default_fontname;
                var basefont_set = false;
                var btnlength = btn.StrArray.Length;
                var validstr = 0;
                var validlength = 0;
                var richedit = false;
                ud.width = 0;
                ud.color = FontColor;
                ud.monospaced = true;
                StringBuilder content = new StringBuilder();
                for(int si = 0; si < btnlength; ++si)
                {
                    var s = btn.StrArray[si];
                    if(string.IsNullOrEmpty(s.Str.Trim()))
                        continue;
                    if(validstr == 0)
                        validstr = 1;
                    else
                    {
                        validstr += 1;
                        break;
                    }
                }
                for(int si = 0; si < btnlength; ++si)
                {
                    var s = btn.StrArray[si];
                    if(s is MinorShift.Emuera.GameView.ConsoleImagePart)
                    {
                        var cp = s as MinorShift.Emuera.GameView.ConsoleImagePart;
                        if(cp.Image != null)
                        {
                            if(ud.image_indices == null)
                                ud.image_indices = new List<int>();
                            ud.image_indices.Add(si);
                            var r = cp.dest_rect;
                            if(-r.Top > ext_up) ext_up = -r.Top;
                            if(r.Top + r.Height > ext_down) ext_down = r.Top + r.Height;
                            continue;
                        }
                    }
                    if(s is MinorShift.Emuera.GameView.ConsoleDivPart)
                    {
                        var dp = s as MinorShift.Emuera.GameView.ConsoleDivPart;
                        ud.div_parts.Add(dp);
                        ud.width += dp.width;
                        validlength += 1; // Ensure it's not considered empty
                        int dh = dp.Height > 0
                            ? dp.Height
                            : (dp.Children != null
                                ? dp.Children.Length * MinorShift.Emuera.Config.LineHeight : 0);
                        if(-dp.PointY > ext_up) ext_up = -dp.PointY;
                        if(dp.PointY + dh > ext_down) ext_down = dp.PointY + dh;
                        continue;
                    }
                    if(s is MinorShift.Emuera.GameView.ConsoleRectangleShapePart)
                    {
                        //<shape>はテキストを持たないので、描画情報を別に集める
                        var sp = s as MinorShift.Emuera.GameView.ConsoleRectangleShapePart;
                        if(ud.shape_parts == null)
                            ud.shape_parts = new List<MinorShift.Emuera.GameView.ConsoleRectangleShapePart>();
                        ud.shape_parts.Add(sp);
                        //1つのユニットは1つのTextに連結されるため、
                        //テキスト側にも幅を確保しないと後続の文字が矩形へ重なる
                        if(sp.Width > 0)
                        {
                            int space_width = uEmuera.Utils.GetDisplayLength(" ", (float)FontSize);
                            if(space_width > 0)
                            {
                                int count = Mathf.Max(1, Mathf.RoundToInt((float)sp.Width / space_width));
                                content.Append(new string(' ', count));
                            }
                        }
                        ud.width += sp.Width;
                        validlength += 1; // 空行扱いにしない
                        continue;
                    }

                    if(s is MinorShift.Emuera.GameView.ConsoleSpacePart)
                    {
                        //PRINT_SPACE / <shape type='space'>。Strが空なので下の文字列処理では
                        //幅が捨てられ、後続の文字が行頭へ詰まって画像に重なっていた。
                        //本家と同じく空白分だけ後ろへずらす
                        if(s.Width > 0)
                        {
                            int space_width = uEmuera.Utils.GetDisplayLength(" ", (float)FontSize);
                            if(space_width > 0)
                            {
                                int count = Mathf.Max(1, Mathf.RoundToInt((float)s.Width / space_width));
                                content.Append(new string(' ', count));
                            }
                        }
                        ud.width += s.Width;
                        continue;
                    }

                    var str = s.Str;
                    //タブを取り除く。
                    //
                    //ERBには「PRINTPLAIN ┃<TAB>」のように引数の末尾へタブが
                    //紛れ込んでいる事がある。本家はGDIで描いていてタブ位置を
                    //設定していないため、タブは幅を持たない。
                    //TMPは次のタブ位置まで飛ばすので、そこだけ大きく空き、
                    //行が伸びて右側の枠が次の行へ押し出されていた。
                    //幅の計算側でもタブは0として数える(Utils.GetDisplayLength)
                    if(str.IndexOf('\t') >= 0)
                        str = str.Replace("\t", "");
                    validlength += str.Trim().Length;
                    if(string.IsNullOrEmpty(str))
                        continue;
                    var fontsize = (float)FontSize;
                    var fontstyle = uEmuera.Drawing.FontStyle.Regular;
                    var fontcolor = FontColor;

                    bool under_here = false;
                    bool strike_here = false;
                    string partfont = null;
                    if(s is MinorShift.Emuera.GameView.ConsoleStyledString)
                    {
                        var u = (MinorShift.Emuera.GameView.ConsoleStyledString)s;
                        fontsize = u.Font.Size;
                        fontstyle = u.Font.Style;
                        partfont = u.Font.FontFamily.Name;
                        //1つのユニットは1つのTMP_Textで描くので、素のフォントは1つしか持てない。
                        //最初に現れたものを基準にし、途中で変わる分は<font>タグで切り替える
                        if(!basefont_set)
                        {
                            basefont_set = true;
                            fontname = partfont;
                        }
                        ud.monospaced = u.Font.Monospaced;
                    }
                    if(s is MinorShift.Emuera.GameView.AConsoleColoredPart)
                    {
                        var u = (MinorShift.Emuera.GameView.AConsoleColoredPart)s;
                        fontcolor = GenericUtils.ToUnityColor(u.pColor);
                    }
                    if(fontstyle != uEmuera.Drawing.FontStyle.Regular)
                    {
                        if((fontstyle & uEmuera.Drawing.FontStyle.Bold) > 0)
                        {
                            str = string.Format("<b>{0}</b>", str);
                            richedit = true;
                        }
                        if((fontstyle & uEmuera.Drawing.FontStyle.Italic) > 0)
                        {
                            str = string.Format("<i>{0}</i>", str);
                            richedit = true;
                        }
                        under_here = (fontstyle & uEmuera.Drawing.FontStyle.Underline) > 0;
                        strike_here = (fontstyle & uEmuera.Drawing.FontStyle.Strikeout) > 0;
                    }
                    if(fontsize != FontSize)
                    {
                        str = string.Format("<size={0}>{1}</size>", (int)fontsize, str);
                        richedit = true;
                    }
                    if(fontcolor != FontColor)
                    {
                        if(validstr != 1)
                        {
                            str = string.Format("<color=#{0}>{1}</color>", GenericUtils.GetColorCode(fontcolor), str);
                            richedit = true;
                        }
                        else
                            ud.color = fontcolor;
                    }
                    //基準と違うフォントの区間はTMPのタグで囲む。
                    //TMPFontsがMaterialReferenceManagerへ登録済みなので名前で引ける
                    if(partfont != null && partfont != fontname)
                    {
                        str = string.Format("<font=\"{0}\">{1}</font>", partfont, str);
                        richedit = true;
                        //TMPは名前で登録済みのフォントしか解決できない。
                        //未登録だとタグがそのまま文字として出てしまうので、
                        //描画側(メインスレッド)で用意させるため名前を残す
                        if(ud.extra_fonts == null)
                            ud.extra_fonts = new List<string>();
                        if(!ud.extra_fonts.Contains(partfont))
                            ud.extra_fonts.Add(partfont);
                    }
                    content.Append(str);
                    var partwidth = uEmuera.Utils.GetDisplayLength(s.Str, partfont, fontsize);
                    //下線・打ち消し線は、その区間の上だけに引く。
                    //以前は行のどこか1箇所に指定があると行全体へ引いていたため、
                    //「FONTSTYLE 8」で一部だけ下線を付けている画面で、
                    //行の端まで長い線が伸びてしまっていた。色もその区間の物を使う
                    if(under_here || strike_here)
                    {
                        if(ud.style_spans == null)
                            ud.style_spans = new List<StyleSpan>();
                        ud.style_spans.Add(new StyleSpan
                        {
                            x = ud.width,
                            w = partwidth,
                            color = fontcolor,
                            under = under_here,
                            strike = strike_here,
                        });
                    }
                    ud.width += partwidth;
                }
                //空白だけのボタン(PRINTBUTTON " " * 80 など)も行を作らないと
                //GameObjectが存在せずクリックできなくなる
                ud.empty = (validlength == 0) && !btn.IsButton;
                if(ud.empty)
                    ud.content = null;
                else
                    ud.content = content.ToString();
                ud.isbutton = btn.IsButton;
                ud.generation = (int)btn.Generation;
                ud.code = btn.Inputs;
                ud.posx = btn.PointX;
                ud.posy = btn.PointY;
                ud.absolute_posy = btn.IsAbsolutePositionedY;
                ud.relative_posx = btn.RelativePointX;
                if(fontname != FontUtils.default_fontname)
                    ud.fontname = fontname;
                else
                    ud.fontname = null;
                ud.richedit = richedit;

                ud.bgcolor = GenericUtils.ToUnityColor(GlobalStatic.Console.bgColor);
                ud.has_bgcolor = GlobalStatic.Console.bgColor.A > 0;

                units.Add(ud);
            }

            extent_up = ext_up;
            extent_down = ext_down;
            if(ext_up > max_extent_up) max_extent_up = ext_up;
            if(ext_down > max_extent_down) max_extent_down = ext_down;
        }
        /// <summary>
        /// 对其方式
        /// </summary>
        public Align align { get { return (Align)display_line.Align; } }
        /// <summary>
        /// 行号
        /// </summary>
        public int LineNo { get { return display_line.LineNo; } }
        /// <summary>
        /// 是否为逻辑行
        /// </summary>
        public bool IsLogicalLine { get { return display_line.IsLogicalLine; } }
        /// <summary>
        /// 坐标Y
        /// </summary>
        public float position_y = 0.0f;
        /// <summary>
        /// 高度
        /// </summary>
        public float height = 0.0f;
        /// <summary>
        /// 子对象
        /// </summary>
        public List<UnitDesc> units = null;
    }

    public static void OnClick(UnityEngine.EventSystems.PointerEventData e)
    {
        //INPUTMOUSEKEY待ちの間はボタン入力ではなくマウスイベントを返す。
        //PressEnterKeyではこの待ちを解けない
        //INPUTMOUSEKEY待ち(RESULT:4にCBGのボタン番号を載せる)と、
        //行の上に重なったCBGのボタンはここで処理する
        if(CBGLayer.TryHandleTap(e))
            return;
        var obj = e.rawPointerPress;
        if(obj == null)
            return;
        var behaviour = obj.GetComponent<EmueraBehaviour>();
        if(behaviour == null)
        {
            //当たり判定用の子オブジェクトが押された場合は、それを持つ行まで遡る。
            //divが中身のボタンはテキストが空で、判定を子オブジェクトに任せている
            behaviour = obj.GetComponentInParent<EmueraBehaviour>();
        }
        if(behaviour == null)
        {
            EmueraThread.instance.Input("", false);
            return;
        }
        var unit_desc = behaviour.unit_desc;
        if(unit_desc == null)
        {
            EmueraThread.instance.Input("", false);
            return;
        }
        if(!unit_desc.isbutton)
            return;
        if(unit_desc.generation < EmueraContent.instance.button_generation)
            EmueraThread.instance.Input("", false);
        else
            EmueraThread.instance.Input(unit_desc.code, true);
    }

    public abstract void UpdateContent();

    public virtual void SetPosition(float x, float y)
    {
        var rt = (RectTransform)transform;
        rt.anchoredPosition = new Vector2(x, y);
    }
    public RectTransform rect_transform { get { return transform as RectTransform; } }

    public LineDesc line_desc = null;
    public int LineNo { get { return line_desc.LineNo; } }
    [HideInInspector]
    public int UnitIdx = -1;
    public UnitDesc unit_desc
    {
        get
        {
            if(line_desc == null || UnitIdx >= line_desc.units.Count)
                return null;
            return line_desc.units[UnitIdx];
        }
    }
    [HideInInspector]
    public float Width = 0;
    [HideInInspector]
    public float Height = 0;
    [HideInInspector]
    public float logic_y = 0.0f;
    [HideInInspector]
    public float logic_height = 0.0f;
#if UNITY_EDITOR
    public string code;
    public int generation;
#endif
}
