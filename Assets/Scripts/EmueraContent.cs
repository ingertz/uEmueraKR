using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

public class EmueraContent : MonoBehaviour
{
    public static EmueraContent instance { get { return instance_; } }
    static EmueraContent instance_ = null;

    public string default_fontname;
    public TMPro.TMP_Text template_text;
    public Image template_block;
    public RectTransform template_images;
    public RectTransform image_content;
    public RectTransform text_content;
    public RectTransform cache_images;
    public OptionWindow option_window;

    Camera main_camere;
    Image background;
    uEmuera.Drawing.Color background_color;

    public RectTransform rect_transform { get { return (RectTransform)transform; } }
    RectMask2D mask2d;

    void Awake()
    {
        FontUtils.SetDefaultFont(default_fontname);
        main_camere = GameObject.FindObjectOfType<Camera>();
    }

    void Start()
    {
        instance_ = this;
        
        var oldBg = GetComponent<Image>();
        if (oldBg != null)
        {
            oldBg.enabled = true;
            oldBg.color = Color.clear;
            oldBg.raycastTarget = true;
        }

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.SetAsFirstSibling();
        var bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        background = bgObj.AddComponent<Image>();
        
        var canvas = bgObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = -30000;
        bgObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            int dragThreshold = Mathf.Max(35, (int)(Screen.dpi * 0.1f));
            UnityEngine.EventSystems.EventSystem.current.pixelDragThreshold = dragThreshold;
        }

        mask2d = GetComponent<RectMask2D>();

        GenericUtils.SetListenerOnBeginDrag(gameObject, OnBeginDrag);
        GenericUtils.SetListenerOnDrag(gameObject, OnDrag);
        GenericUtils.SetListenerOnEndDrag(gameObject, OnEndDrag);
        GenericUtils.SetListenerOnClick(gameObject, OnClick);

        if (image_content != null)
        {
            image_content.SetAsFirstSibling();
        }
        //CBGの層。奥の層は行内画像よりさらに奥へ置くので、image_contentの後で作る
        CBGLayer.Create(rect_transform);
        //SETBGIMAGEの層。CBGの奥の層よりさらに奥(最背面)へ置くので、CBGの後で作る
        BGImageLayer.Create(rect_transform);
        //HTML_PRINT_ISLANDの層。CBGより後に作って最前面に置く
        IslandLayer.Create(this);

        SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                    PlayerPrefs.GetInt("IntentBox_R", 0),
                    PlayerPrefs.GetInt("IntentBox_T", 0),
                    PlayerPrefs.GetInt("IntentBox_B", 0));
    }

    /// <summary>
    /// 下端に常に空ける余白。
    /// 最終行が画面の縁にぴったり着くと、そこにあるボタンが押しにくい
    /// </summary>
    public const int kBottomGap = 8;

    public void SetIntentBox(int left, int right, int top, int bottom)
    {
        //切り取りは利用者が余白を指定した時だけ。
        //常に有効にすると、行からはみ出す画像まで切れてしまう
        mask2d.enabled = (left != 0 || right != 0 || top != 0 || bottom != 0);

        var b = bottom + kBottomGap;
        //引き伸ばしアンカーなのでsizeDeltaは親との差分。
        //位置は余白の差の半分だけ動かすと、指定した側が狭くなる
        rect_transform.anchoredPosition = new Vector2((left - right) / 2.0f, (b - top) / 2.0f);
        rect_transform.sizeDelta = new Vector2(-right - left, -top - b);
        SetDirty();
    }

    int GetLineNoIndex(int lineno)
    {
        int high = end_index - 1;
        int low = begin_index;
        int mid = 0;
        int found = -1;

        while(low <= high)
        {
            mid = (low + high) / 2;
            int k = console_lines_[mid % max_log_count].LineNo;
            if(k > lineno)
                high = mid - 1;
            else if(k < lineno)
                low = mid + 1;
            else
            {
                found = mid;
                break;
            }
        }

        if(found < 0)
            return -1;
        return found;
    }

    int GetLineNoIndexForPosY(float y)
    {
        int high = end_index - 1;
        int low = begin_index;
        int mid = 0;
        while(low <= high)
        {
            mid = (low + high) / 2;
            var l = console_lines_[mid % max_log_count];
            float top = l.position_y;
            float bottom = l.position_y + l.height;
            if(y < top)
                high = mid - 1;
            else if(y > bottom)
                low = mid + 1;
            else
                return mid;
        }
        if(high <= begin_index)
            return begin_index;
        else if(low >= end_index - 1)
            return end_index - 1;
        return -1;
    }

    int GetPrevLineNoIndex(int index)
    {
        if(index > max_index || index < 0)
            return -1;

        var lineno = 0;
        var cindex = index;
        var zero = begin_index;

        if(!console_lines_[index % max_log_count].IsLogicalLine)
        {
            lineno = console_lines_[index % max_log_count].LineNo;
            for(; cindex >= zero; --cindex)
            {
                if(console_lines_[cindex % max_log_count].LineNo != lineno)
                    break;
            }
            return cindex + 1;
        }
        else
            return cindex;
    }

    int GetNextLineNoIndex(int index)
    {
        if(index > max_index || index < 0)
            return -1;

        var lineno = console_lines_[index % max_log_count].LineNo;
        var cindex = index;
        for(; cindex < max_index; ++cindex)
        {
            if(console_lines_[cindex % max_log_count].LineNo != lineno)
                break;
        }
        return cindex - 1;
    }

    public void Update()
    {
        if(!dirty && drag_delta == Vector2.zero)
            return;
        dirty = false;
        RenderThrottle.Wake();

        float display_width = DISPLAY_WIDTH;
        float display_height = DISPLAY_HEIGHT;

        if(drag_delta != Vector2.zero)
        {
            float t = drag_delta.magnitude;
            drag_delta *= (Mathf.Max(0, t - 300.0f * Time.deltaTime) / t);
            local_position = GetLimitPosition(local_position + drag_delta,
                                            display_width, display_height);
            if((local_position.x <= display_width - content_width && drag_delta.x < 0) ||
                (local_position.x >= 0 && drag_delta.x > 0))
                drag_delta.x = 0;
            if((local_position.y >= DrawnHeight - display_height && drag_delta.y > 0) ||
                (local_position.y <= offset_height && drag_delta.y < 0))
                drag_delta.y = 0;
        }

        var pos = local_position + (drag_curr_position - drag_begin_position);
        pos = GetLimitPosition(pos, display_width, display_height);

        int remove_count = 0;
        int count = display_lines_.Count;
        int max_line_no = -1;
        int min_line_no = int.MaxValue;
        for(int i = 0; i < count - remove_count; ++i)
        {
            var line = display_lines_[i];
            if(line.logic_y > pos.y + display_height ||
                line.logic_y + line.logic_height < pos.y)
            {
                display_lines_[i] = display_lines_[count - remove_count - 1];
                PushLine(line);
                ++remove_count;
                --i;
            }
            else
            {
                line.SetPosition(pos.x + line.unit_desc.posx, pos.y - line.logic_y);
                max_line_no = System.Math.Max(line.LineNo, max_line_no);
                min_line_no = System.Math.Min(line.LineNo, min_line_no);
            }
        }
        if(remove_count > 0)
            display_lines_.RemoveRange(count - remove_count, remove_count);

        List<EmueraImage> image_removelist = null;
        var display_iter = display_images_.GetEnumerator();
        while(display_iter.MoveNext())
        {
            var image = display_iter.Current.Value;
            if(image.logic_y > pos.y + display_height ||
                image.logic_y + image.logic_height < pos.y)
            {
                if(image_removelist == null)
                    image_removelist = new List<EmueraImage>();
                image_removelist.Add(image);
            }
            else
                image.SetPosition(pos.x + image.unit_desc.posx, pos.y - image.logic_y);
        }
        if(image_removelist != null)
        {
            var listcount = image_removelist.Count;
            EmueraImage image = null;
            for(int i=0; i<listcount; ++i)
            {
                image = image_removelist[i];
                PushImageContainer(image);
                display_images_.Remove(image.LineNo * 1000 + image.UnitIdx);
            }
        }

        var index = GetLineNoIndex(min_line_no - 1);
        index = GetPrevLineNoIndex(index);
        if(index >= 0)
        {
            UpdateLine(pos, display_height, index, -1);
        }
        index = GetLineNoIndex(max_line_no + 1);
        index = GetNextLineNoIndex(index);
        if(index >= 0)
        {
            UpdateLine(pos, display_height, index, +1);
        }
        if(display_lines_.Count == 0 &&
            console_lines_ != null && console_lines_.Count > 0)
        {
            index = GetLineNoIndexForPosY(pos.y);
            UpdateLine(pos, display_height, index, -1);
            UpdateLine(pos, display_height, index + 1, +1);
        }
    }
    void UpdateLine(Vector2 local, float display_height, int index, int delta)
    {
        var zero = begin_index;
        while(zero <= index && index < end_index)
        {
            var l = console_lines_[index % max_log_count];
            //行の高さは常にConfig.LineHeightだが、画像やdivを含む行はそれより遥かに
            //上下へはみ出して描かれる。画面外の行に当たった時点で打ち切ると、
            //その手前にある背の高い行(画像行など)を取りこぼす。
            //最大はみ出し量ぶんは走査を続け、掛からない行だけを読み飛ばす
            if(l.position_y - EmueraBehaviour.LineDesc.max_extent_up > local.y + display_height ||
                l.position_y + EmueraBehaviour.LineDesc.max_extent_down < local.y)
                break;
            if(l.position_y - l.extent_up > local.y + display_height ||
                l.position_y + l.extent_down < local.y)
            {
                index += delta;
                continue;
            }

            for(int li = 0; li < l.units.Count; ++li)
            {
                var unit = l.units[li];
                if(!unit.empty)
                {
                    var lc = PullLine();
                    lc.line_desc = l;
                    lc.UnitIdx = li;
                    lc.Width = unit.width;
                    lc.UpdateContent();
                    lc.SetPosition(unit.posx + local.x, local.y - lc.logic_y);
                    display_lines_.Add(lc);
                }
                if(unit.image_indices != null && unit.image_indices.Count > 0)
                {
                    var hash = l.LineNo * 1000 + li;
                    EmueraImage ic = null;
                    if(!display_images_.TryGetValue(hash, out ic))
                    {
                        ic = PullImageContainer();
                        display_images_.Add(l.LineNo * 1000 + li, ic);
                    }
                    else
                        ic.Clear();
                    ic.line_desc = l;
                    ic.UnitIdx = li;
                    ic.Width = unit.width;
                    ic.UpdateContent();
                    ic.SetPosition(unit.posx + local.x, local.y - ic.logic_y);
                }
            }
            index += delta;
        }
    }
    Vector2 GetLimitPosition(Vector2 local,
        float display_width, float display_height)
    {
        if(content_width > display_width)
        {
            //左右移动
            if(local.x > 0)
                local.x = 0;
            else if(local.x < display_width - content_width)
                local.x = display_width - content_width;
        }
        else
            local.x = 0;

        var valid_height = content_height - offset_height;
        if(offset_height > 0 && valid_height < display_height)
        {
            local.y = offset_height;
        }
        else
        {
            //はみ出す分まで含めた下端で止める。でないと最後の行が枠の外に残る
            var display_delta = DrawnHeight - display_height;
            if(DrawnHeight <= display_height)
                local.y = display_delta;
            else if(local.y > display_delta)
                local.y = display_delta;
            else if(local.y < offset_height)
                local.y = offset_height;
        }

        return local;
    }
    public void SetDirty()
    {
        dirty = true;
        RenderThrottle.Wake();
        //ToBottom();
    }

    bool dirty = false;
    uint last_click_tic = 0;
    void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        drag_begin_position = e.position;
        drag_curr_position = e.position;
        drag_delta = Vector3.zero;
    }
    void OnDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        dirty = true;
        drag_curr_position = e.position;
        drag_delta = Vector3.zero;
    }
    void OnEndDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        dirty = true;
        float dragDistance = (e.position - drag_begin_position).magnitude;
        if (dragDistance < 45f)
        {
            drag_begin_position = Vector2.zero;
            drag_curr_position = Vector2.zero;
            drag_delta = Vector2.zero;
            OnClick(e);
            return;
        }

        float display_width = DISPLAY_WIDTH;
        float display_height = DISPLAY_HEIGHT;
        local_position = GetLimitPosition(
            local_position + (e.position - drag_begin_position),
            display_width, display_height);

        drag_delta = e.position - drag_curr_position;
        drag_begin_position = Vector2.zero;
        drag_curr_position = Vector2.zero;
    }
    void OnClick(UnityEngine.EventSystems.PointerEventData e)
    {
        //CBGのボタン(画像・ボタンマップ)とINPUTMOUSEKEY待ちは先に処理する
        if(CBGLayer.TryHandleTap(e))
            return;
        var nowtick = MinorShift._Library.WinmmTimer.TickCount;
        var skipflag = (nowtick - last_click_tic < 200);
        EmueraThread.instance.Input("", false, skipflag);
        last_click_tic = nowtick;
    }
    Vector2 drag_begin_position = Vector2.zero;
    Vector2 drag_curr_position = Vector2.zero;
    Vector2 drag_delta = Vector2.zero;

    public void SetBackgroundColor(uEmuera.Drawing.Color color)
    {
        if(background_color == color)
            return;
        background.color = GenericUtils.ToUnityColor(color);
        background_color = color;
        main_camere.backgroundColor = background.color;
    }
    public void Ready()
    {
        EmueraBehaviour.Ready();
        option_window.Ready();
        option_window.gameObject.SetActive(true);

        background.color = GenericUtils.ToUnityColor(Config.BackColor);
        background_color = Config.BackColor;
        content_width = Config.WindowX;

        FontUtils.SetDefaultFont(Config.FontName);

        template_text.color = EmueraBehaviour.FontColor;
        //設定フォント -> ＭＳ ゴシック -> ハングルの順に字形を補う。
        //ＭＳ ゴシックにハングルが無いなど、1つのフォントでは足りない事が多い
        var basefont = uEmuera.TMPFonts.GetOrDefault(Config.FontName);
        if(basefont != null)
        {
            uEmuera.TMPFonts.SetFallback(basefont, "ＭＳ ゴシック", "ＭＳ Ｐゴシック");
            //以後に作るフォントの行送りをこれへ揃える。
            //<font=～>で差し込まれる物が行の高さを押し上げないように
            uEmuera.TMPFonts.SetReference(basefont);
            template_text.font = basefont;
            Debug.Log("EmueraContent: 본문 폰트 [" + Config.FontName + "] -> [" + basefont.name + "]");
        }
        template_text.fontSize = EmueraBehaviour.FontSize;
        template_text.rectTransform.sizeDelta =
            new Vector2(template_text.rectTransform.sizeDelta.x, 0);
        template_text.gameObject.SetActive(false);
        UpdateLineOverhang();

        console_lines_ = new List<EmueraBehaviour.LineDesc>(max_log_count);
        while(console_lines_.Count < max_log_count)
            console_lines_.Add(null);
        invalid_count = max_log_count;
    }
    public void SetNoReady() { ready_ = false; }

    /// <summary>
    /// 行の箱は、行送りが行の高さより大きいフォントでは下へはみ出す。
    ///
    /// エミュレータはConfig.LineHeightで座標を組むが、実際に置かれる行の箱は
    /// ContentSizeFitterがフォントの行送りで決めるため、その差だけ背が高い。
    /// 一番下の行だけはこの差が表示領域の外へ出てしまい、半分ほど切れて見えていた。
    ///
    /// 描画位置は動かさない。動かすと行間が変わり、以前直した行の重なりが戻る。
    /// 巻き取れる範囲をこの差だけ広げて、最後の行が枠の中へ入るようにする
    /// </summary>
    void UpdateLineOverhang()
    {
        line_overhang_ = 0f;
        if(template_text == null || template_text.font == null)
            return;
        var fi = template_text.font.faceInfo;
        if(fi.pointSize <= 0)
            return;
        float box = fi.lineHeight * template_text.fontSize / fi.pointSize;
        line_overhang_ = Mathf.Max(0f, box - Config.LineHeight);
    }
    float line_overhang_ = 0f;

    /// <summary>座標計算で使う、実際に描かれる内容の下端</summary>
    float DrawnHeight { get { return content_height + line_overhang_; } }
    bool ready_ = false;

    public void Clear()
    {
        invalid_count = max_log_count;
        max_index = 0;
        for(int i = 0; i < console_lines_.Count; ++i)
            console_lines_[i] = null;

        for(int i = 0; i < display_lines_.Count; ++i)
        {
            PushLine(display_lines_[i]);
        }

        display_lines_.Clear();

        var iter = cache_lines_.GetEnumerator();
        while(iter.MoveNext())
            GameObject.Destroy(iter.Current.gameObject);
        var iter2 = cache_images_.GetEnumerator();
        while(iter2.MoveNext())
            GameObject.Destroy(iter2.Current.gameObject);

        cache_lines_.Clear();
        cache_images_.Clear();

        content_height = 0;
        offset_height = 0;
        local_position = Vector2.zero;
        drag_delta = Vector2.zero;
        dirty = true;
    }
    public void AddLine(object line, bool roll_to_bottom = false)
    {
        if(line == null)
            return;
        if(!ready_)
        {
            Ready();
            ready_ = true;
        }
        var ld = new EmueraBehaviour.LineDesc(line, content_height, Config.LineHeight);

        console_lines_[max_index % max_log_count] = ld;
        if(invalid_count > 0)
            invalid_count -= 1;
        //添加偏移高
        if(valid_count >= max_log_count)
            offset_height += Config.LineHeight;
        max_index += 1;

        ld.Update();
        
        //添加容器高
        content_height += Config.LineHeight;
        if(roll_to_bottom)
        {
            local_position.y = DrawnHeight - rect_transform.rect.height;
            drag_delta.y = 0;
        }
        else
        {
            drag_delta.y += Config.LineHeight * 1.5f;
        }
        dirty = true;
    }
    public object GetLine(int index)
    {
        if(index < begin_index || index >= end_index)
            return null;
        return console_lines_[index % max_log_count].console_line;
    }
    public int GetLineCount()
    {
        return valid_count;
    }
    public int GetMinLineNo()
    {
        return begin_index;
    }
    public int GetMaxLineNo()
    {
        if(valid_count == 0)
            return -1;
        return max_index;
    }
    public void RemoveLine(int count)
    {
        if(!ready_)
        {
            Ready();
            ready_ = true;
        }
        if(count > valid_count)
            count = valid_count;
        if(count == 0)
            return;

        var lineno = console_lines_[(max_index - count) % max_log_count].LineNo;
        display_lines_.Sort((l, r) =>
        {
            return (l.LineNo * 10 + l.UnitIdx) -
                (r.LineNo * 10 + r.UnitIdx);
        });

        var i = 0;
        for(; i < display_lines_.Count; ++i)
        {
            if(display_lines_[i].LineNo >= lineno)
                break;
        }
        List<int> imageremove = new List<int>();

        var iter = display_images_.GetEnumerator();
        while(iter.MoveNext())
        {
            var image = iter.Current;
            if(image.Key / 1000 >= lineno)
            {
                PushImageContainer(image.Value);
                imageremove.Add(image.Key);
            }
        }
        var remove = imageremove.Count;
        for(var j=0; j<remove; ++j)
        {
            display_images_.Remove(imageremove[j]);
        }

        remove = 0;
        for(; i < display_lines_.Count; ++i, ++remove)
        {
            PushLine(display_lines_[i]);
        }
        if(remove > 0)
        {
            if(remove >= display_lines_.Count)
                display_lines_.Clear();
            else
                display_lines_.RemoveRange(display_lines_.Count - remove, remove);
        }

        var eidx = end_index - 1;
        var bidx = end_index - count ;
        for(i = eidx; i >= bidx; --i)
            console_lines_[i % max_log_count] = null;

        content_height -= Config.LineHeight * count;
        if(count >= valid_count)
        {
            offset_height = 0;
            content_height = 0;
        }
        else if(max_index > max_log_count)
        {
            if(max_index - count <= max_log_count)
            {
                //offset_height -= Config.LineHeight * (max_index - max_log_count);
                //offset_height = Config.LineHeight * (max_index - max_log_count);
                var l = console_lines_[max_index - count - 1];
                offset_height = l.position_y + l.height;
            }
        }

        invalid_count += count;
        max_index -= count;
        dirty = true;
    }
    public void ToBottom()
    {
        local_position.y = DrawnHeight - rect_transform.rect.height;
        drag_delta = Vector2.zero;
        dirty = true;
        Update();
    }
    public void ShowIsInProcess(bool value)
    {
        option_window.inprogress.SetActive(value);
    }
    public void SetLastButtonGeneration(int generation)
    {
        last_button_generation = generation;

        var quick_buttons = option_window.quick_buttons;
        if(quick_buttons.IsShow)
        {
            quick_buttons.Clear();
            if(last_button_generation < 0)
                return;

            quick_codes_.Clear();
            for(int i = end_index - 1; i >= begin_index; --i)
            {
                var cl = console_lines_[i%max_log_count];
                if(cl.units == null)
                    continue;
                quick_entries_.Clear();
                bool go_on = CollectQuickEntries(cl.console_line as ConsoleDisplayLine, cl.units, 0);
                FlushQuickEntries(quick_buttons);
                if(!go_on)
                    return;
            }
        }
    }

    struct QuickEntry
    {
        public int y;
        public string content;
        public Color color;
        public string code;
        /// <summary>文字を持たないボタンの見出し代わりの画像</summary>
        public MinorShift.Emuera.Content.ASprite picture;
    }
    readonly List<QuickEntry> quick_entries_ = new List<QuickEntry>();
    /// <summary>もう並べた入力コード。同じ物を何組も出さないための控え</summary>
    readonly HashSet<string> quick_codes_ = new HashSet<string>();

    /// <summary>
    /// 行の中のボタンを集める。古い世代のボタンに当たったらfalseを返して走査を打ち切る。
    ///
    /// divの中も辿る。コマンド欄をHTML_PRINTの&lt;div&gt;で組み立てるゲームがあり、
    /// 行のButtonsを見るだけでは中のボタンに届かない。
    /// yは本体の画面での縦位置。ボタン欄で行を分ける手掛かりに使う
    /// </summary>
    bool CollectQuickEntries(ConsoleDisplayLine line, List<EmueraBehaviour.UnitDesc> units, int y)
    {
        for(int j = 0; j < units.Count; ++j)
        {
            var cu = units[j];
            if(cu.isbutton)
            {
                if(cu.generation != last_button_generation)
                    return false;
                if(cu.empty)
                    continue;
                //同じ画面を何度も刷り直すゲームがあり、同じ入力のボタンが世代を跨がずに
                //何組も残る。押した時の結果は同じなので一番新しい物だけ並べる
                if(!string.IsNullOrEmpty(cu.code) && !quick_codes_.Add(cu.code))
                    continue;
                //ボタンごとdivで包む書き方がある(パーティ欄など)。
                //その場合ボタン自身は文字を持たないので中から見出しを拾う
                var label = cu.content;
                if(string.IsNullOrEmpty(label) || label.Trim().Length == 0)
                    label = FirstTextInDivs(cu.div_parts);
                MinorShift.Emuera.Content.ASprite picture = null;
                if(string.IsNullOrEmpty(label) || label.Trim().Length == 0)
                {
                    //文字が一つも無いボタン。顔グラだけを押し場所にしている作りがあり、
                    //見出しが作れず落としていた。中の画像をそのまま見出しにする
                    picture = UnitImage(line, j, cu);
                    if(picture == null)
                        picture = FirstImageInDivs(cu.div_parts);
                    if(picture == null)
                        continue;
                    label = "";
                }
                quick_entries_.Add(new QuickEntry
                {
                    y = y,
                    content = label.Length > 0 ? Shorten(label) : label,
                    color = cu.color,
                    code = cu.code,
                    picture = picture,
                });
                //ボタンは一つの押し場所。中を辿ると同じ物が二重に並ぶ
                continue;
            }
            if(cu.div_parts != null)
            {
                for(int d = 0; d < cu.div_parts.Count; ++d)
                {
                    var dp = cu.div_parts[d];
                    if(dp == null || dp.Children == null)
                        continue;
                    for(int c = 0; c < dp.Children.Length; ++c)
                    {
                        var child = dp.Children[c];
                        if(child == null)
                            continue;
                        var ld = new EmueraBehaviour.LineDesc(child, 0, 0);
                        ld.Update();
                        if(!CollectQuickEntries(child, ld.units, y + dp.PointY + c * Config.LineHeight))
                            return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>ボタン欄の枠に収まらない見出しを詰める</summary>
    static string Shorten(string s)
    {
        s = s.Trim();
        //改行までを見出しにする。複数行のステータス欄が丸ごと入るのを防ぐ。
        //
        //連続空白では切らないこと。「愛撫[ 12]」のように数字を右へ寄せる
        //書き方が普通にあり、そこで切ると「愛撫[」になってしまう
        int cut = s.IndexOfAny(kBreaks);
        if(cut > 0)
            s = s.Substring(0, cut).TrimEnd();
        s = StripTags(s);
        s = CollapseSpaces(s);
        return s.Length > kQuickLabelMax ? s.Substring(0, kQuickLabelMax) + "…" : s;
    }
    static readonly char[] kBreaks = { '\n', '\r' };
    //これを超えるのは大抵ステータス欄の中身。字は枠に合わせて縮むので余裕を持たせる
    const int kQuickLabelMax = 32;

    /// <summary>
    /// TMPの書式タグを取り除く。
    ///
    /// 本文用のUnitDescは色や太さをタグで持っており、それを描くTMP_Textでは
    /// 書式として消えるが、ボタン欄の見出しは書式を使わないTextなので
    /// 「&lt;color=#660022ff&gt;」が文字のまま出てしまう。
    /// 色はボタン側で別に持っているので落として構わない。
    /// 知っているタグ名だけを対象にして、本文中の「&lt;注意&gt;」等は残す
    /// </summary>
    static string StripTags(string s)
    {
        if(s.IndexOf('<') < 0)
            return s;
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while(i < s.Length)
        {
            if(s[i] == '<')
            {
                int end = s.IndexOf('>', i + 1);
                if(end > i && IsKnownTag(s, i + 1, end))
                {
                    i = end + 1;
                    continue;
                }
            }
            sb.Append(s[i]);
            i += 1;
        }
        return sb.ToString();
    }
    static readonly string[] kKnownTags = { "b", "i", "u", "s", "size", "color", "font" };
    static bool IsKnownTag(string s, int start, int end)
    {
        if(start < end && s[start] == '/')
            start += 1;
        int n = start;
        while(n < end && s[n] != '=' && s[n] != ' ')
            n += 1;
        if(n == start)
            return false;
        for(int k = 0; k < kKnownTags.Length; ++k)
            if(string.Compare(s, start, kKnownTags[k], 0, n - start, System.StringComparison.OrdinalIgnoreCase) == 0
               && kKnownTags[k].Length == n - start)
                return true;
        return false;
    }

    /// <summary>
    /// 桁を揃える為に並べられた空白を一つに詰める。
    ///
    /// 「%地名, 18, LEFT% 片道{時間, 3}」のように幅を指定して書かれた行は、
    /// 等幅で並べる本体の画面では列が揃うが、ボタンを一つずつ並べるボタン欄では
    /// 空白が枠を食うだけで意味が無い。
    /// 一つ目の空白は元のまま残すので「愛撫[ 12]」のような書き方は崩れない
    /// </summary>
    static string CollapseSpaces(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        bool prev_space = false;
        for(int i = 0; i < s.Length; ++i)
        {
            var c = s[i];
            if(c == ' ' || c == '　' || c == '\t')
            {
                if(!prev_space)
                    sb.Append(c);
                prev_space = true;
                continue;
            }
            prev_space = false;
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>そのユニットが直接持っている画像。無ければnull</summary>
    static MinorShift.Emuera.Content.ASprite UnitImage(ConsoleDisplayLine line, int index,
                                                       EmueraBehaviour.UnitDesc cu)
    {
        if(cu.image_indices == null || cu.image_indices.Count == 0)
            return null;
        if(line == null || line.Buttons == null || index < 0 || index >= line.Buttons.Length)
            return null;
        var parts = line.Buttons[index].StrArray;
        if(parts == null)
            return null;
        for(int i = 0; i < cu.image_indices.Count; ++i)
        {
            int si = cu.image_indices[i];
            if(si < 0 || si >= parts.Length)
                continue;
            var ip = parts[si] as ConsoleImagePart;
            if(ip != null && ip.Image != null)
                return ip.Image;
        }
        return null;
    }

    /// <summary>divの中から見出しに使えそうな最初の画像を取り出す</summary>
    MinorShift.Emuera.Content.ASprite FirstImageInDivs(
        List<MinorShift.Emuera.GameView.ConsoleDivPart> parts)
    {
        if(parts == null)
            return null;
        for(int d = 0; d < parts.Count; ++d)
        {
            var dp = parts[d];
            if(dp == null || dp.Children == null)
                continue;
            for(int c = 0; c < dp.Children.Length; ++c)
            {
                var child = dp.Children[c];
                if(child == null)
                    continue;
                var ld = new EmueraBehaviour.LineDesc(child, 0, 0);
                ld.Update();
                for(int u = 0; u < ld.units.Count; ++u)
                {
                    var img = UnitImage(child, u, ld.units[u]);
                    if(img != null)
                        return img;
                    img = FirstImageInDivs(ld.units[u].div_parts);
                    if(img != null)
                        return img;
                }
            }
        }
        return null;
    }

    /// <summary>divの中から見出しに使えそうな最初の文字列を取り出す</summary>
    string FirstTextInDivs(List<MinorShift.Emuera.GameView.ConsoleDivPart> parts)
    {
        if(parts == null)
            return null;
        for(int d = 0; d < parts.Count; ++d)
        {
            var dp = parts[d];
            if(dp == null || dp.Children == null)
                continue;
            for(int c = 0; c < dp.Children.Length; ++c)
            {
                var child = dp.Children[c];
                if(child == null)
                    continue;
                var ld = new EmueraBehaviour.LineDesc(child, 0, 0);
                ld.Update();
                for(int u = 0; u < ld.units.Count; ++u)
                {
                    var t = ld.units[u].content;
                    if(!string.IsNullOrEmpty(t) && t.Trim().Length > 0)
                        return t;
                    t = FirstTextInDivs(ld.units[u].div_parts);
                    if(!string.IsNullOrEmpty(t))
                        return t;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 集めたボタンをボタン欄へ積む。縦位置が違う物は別の行にする
    /// </summary>
    void FlushQuickEntries(QuickButtons quick_buttons)
    {
        if(quick_entries_.Count == 0)
            return;
        //下にある物ほど手前へ。ボタン欄は新しい行から積むので本体と上下が揃う。
        //同じ高さの中では元の並び順を崩さないよう挿入ソートで入れ替える
        for(int i = 1; i < quick_entries_.Count; ++i)
        {
            var e = quick_entries_[i];
            int k = i - 1;
            while(k >= 0 && quick_entries_[k].y < e.y)
            {
                quick_entries_[k + 1] = quick_entries_[k];
                k -= 1;
            }
            quick_entries_[k + 1] = e;
        }
        int last_y = quick_entries_[0].y;
        for(int i = 0; i < quick_entries_.Count; ++i)
        {
            var e = quick_entries_[i];
            if(e.y != last_y)
            {
                quick_buttons.ShiftLine();
                last_y = e.y;
            }
            quick_buttons.AddButton(e.content, e.color, e.code, e.picture);
        }
        quick_buttons.ShiftLine();
        quick_entries_.Clear();
    }

    public int button_generation { get { return last_button_generation; } }
#if UNITY_EDITOR
    public
#endif
    int last_button_generation = 0;
    int max_index = 0;
    int invalid_count = 0;
    int begin_index
    {
        get
        {
            return System.Math.Max(0, max_index - valid_count);
        }
    }
    int end_index
    {
        get
        {
            return max_index;
        }
    }
    int valid_count
    {
        get
        {
            return max_log_count - invalid_count;
        }
    }
    public int max_log_count { get { return MinorShift.Emuera.Config.MaxLog; } }
    List<EmueraBehaviour.LineDesc> console_lines_;

    //RectTransform parent
    //{
    //    get
    //    {
    //        if(parent_ == null)
    //        {
    //            parent_ = transform.parent as RectTransform;
    //            while(parent_.parent != null)
    //            {
    //                parent_ = parent_.parent as RectTransform; ;
    //            }
    //        }
    //        return parent_;
    //    }
    //}
    //RectTransform parent_;
    //float DISPLAY_WIDTH { get { return parent.sizeDelta.x; } }
    //float DISPLAY_HEIGHT { get { return parent.sizeDelta.y; } }
    float DISPLAY_WIDTH { get { return rect_transform.rect.width; } }
    float DISPLAY_HEIGHT { get { return rect_transform.rect.height; } }

    /// <summary>
    /// 偏移高
    /// </summary>
    float offset_height = 0;
    /// <summary>
    /// 内容宽
    /// </summary>
    float content_width = 0;
    /// <summary>
    /// 内容高
    /// </summary>
    float content_height = 0;
    /// <summary>出力済み内容の総高さ。absolute配置divの縦の基準に使う</summary>
    public float ContentHeight { get { return content_height; } }
    /// <summary>
    /// 当前移动点
    /// </summary>
    Vector2 local_position = Vector2.zero;

    List<EmueraLine> display_lines_ = new List<EmueraLine>();
    Dictionary<int, EmueraImage> display_images_ = new Dictionary<int, EmueraImage>();
    /// <summary>
    /// 获取文本显示控件
    /// </summary>
    /// <returns></returns>
    public EmueraLine PullLine()
    {
        EmueraLine line = null;
        if(cache_lines_.Count > 0)
            line = cache_lines_.Dequeue();
        else
        {
            var obj = GameObject.Instantiate(template_text.gameObject);
            line = obj.GetComponent<EmueraLine>();
        }
        line.transform.SetParent(text_content, false);
        line.transform.localScale = Vector3.one;
        line.gameObject.SetActive(true);
        //line.size_fitter.enabled = true;
        //line.monospaced.enabled = true;
        //line.gameObject.SetActive(true);  
        return line;
    }
    /// <summary>
    /// 交还文本显示控件
    /// </summary>
    /// <param name="line"></param>
    public void PushLine(EmueraLine line)
    {
        line.Clear();
        //line.gameObject.SetActive(false);

        //line.size_fitter.enabled = false;
        //line.monospaced.enabled = false;
        //line.text.text = string.Empty;
        //line.rect_transform.sizeDelta = Vector2.zero;
        line.rect_transform.position = new Vector3(-10000, 0, 0);

#if UNITY_EDITOR
        line.gameObject.name = "unused";
#endif
        cache_lines_.Enqueue(line);
    }
    Queue<EmueraLine> cache_lines_ = new Queue<EmueraLine>();

    /// <summary>
    /// 获取图片显示控件
    /// </summary>
    /// <returns></returns>
    public EmueraImage PullImageContainer()
    {
        EmueraImage image = null;
        if(cache_image_containers_.Count > 0)
            image = cache_image_containers_.Pop();
        else
        {
            var obj = GameObject.Instantiate(template_images.gameObject);
            image = obj.GetComponent<EmueraImage>(); 
        }
        image.transform.SetParent(image_content);
        image.transform.localScale = Vector3.one;
        image.gameObject.SetActive(true);
        return image;
    }
    /// <summary>
    /// 交还图片显示控件
    /// </summary>
    /// <param name="image"></param>
    public void PushImageContainer(EmueraImage image)
    {
        image.Clear();
        image.gameObject.SetActive(false);
#if UNITY_EDITOR
        image.gameObject.name = "unused";
#endif
        image.transform.SetParent(cache_images);
        cache_image_containers_.Push(image);
    }
    Stack<EmueraImage> cache_image_containers_ = new Stack<EmueraImage>();

    public Image PullImage()
    {
        Image image = null;
        if(cache_images_.Count > 0)
            image = cache_images_.Pop();
        else
        {
            var obj = new GameObject();
            image = obj.AddComponent<Image>();
            image.transform.SetParent(cache_images);
            var rt = image.transform as RectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;
        }
        image.gameObject.SetActive(true);
        return image;
    }
    public void PushImage(Image image)
    {
        image.gameObject.SetActive(false);
#if UNITY_EDITOR
        image.gameObject.name = "unused";
#endif
        image.sprite = null;
        image.transform.SetParent(cache_images);
        cache_images_.Push(image);
    }
    Stack<Image> cache_images_ = new Stack<Image>();
}
