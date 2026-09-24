using TMPro;
using UnityEngine;

namespace uEmuera
{
    /// <summary>
    /// TMPが組んだ文字を、エミュレータが座標計算に使ったのと同じ格子へ揃える。
    /// 旧Monospaced(BaseMeshEffect)のTMP版。
    ///
    /// 字形が無くて代替フォントが描いた文字は送り幅が僅かに違うため、
    /// 長い表では列がずれて波打って見える。設定フォント1つで全部描けるPCでは
    /// 起きず、代替が入る端末でだけ出ていた。
    ///
    /// 幅はUtils.GetCharWidthに任せる。GetDisplayLengthと同じ物なので
    /// 座標計算と描画が食い違わない。
    ///
    /// TMPは配置後にレイアウトや再有効化で組み直す事があり、
    /// その度に頂点を書き直さないと元に戻ってしまう。
    /// そのためTEXT_CHANGED_EVENTで組み直しの後に毎回かけ直す
    /// </summary>
    [DisallowMultipleComponent]
    public class TMPMonospaced : MonoBehaviour
    {
        public string fontname;
        public bool apply = true;

        TMP_Text text_;
        bool working_;

        TMP_Text text
        {
            get
            {
                if (text_ == null)
                    text_ = GetComponent<TMP_Text>();
                return text_;
            }
        }

        void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            if (text != null)
                text.SetVerticesDirty();
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        void OnTextChanged(Object obj)
        {
            //自分の分だけ。頂点更新から再入する事もあるので番をする
            if (working_ || !apply || text == null || !ReferenceEquals(obj, text_))
                return;
            working_ = true;
            try { Apply(); }
            catch (System.Exception e) { Debug.LogWarning("TMPMonospaced: " + e.Message); }
            finally { working_ = false; }
        }

        /// <summary>
        /// 横線だけセル幅ぴったりに伸ばして隣と繋げる。隙間が空くと枠線が途切れて見える。
        ///
        /// 縦線や継ぎ手まで対象にすると、横へ引き伸ばされて縦の画がそのまま太くなる。
        /// 罫線素片をまとめて扱ってはいけない
        /// </summary>
        static bool IsHorizontalRule(char c)
        {
            return c == '-' || c == '=' || c == '_' || c == '~'
                || c == '━' || c == '─' || c == '―' || c == '—'
                || c == '－' || c == '＝' || c == '～';
        }

        /// <summary>
        /// その文字を実際に描いたフォントの名前を返す。
        ///
        /// 行の基準フォントで全文字を測ってはいけない。
        /// ぉんFontは半角の文字コードへ全角のアイコンを割り当てており、
        /// ＭＳ ゴシックで測ると半角(8)、実際の描画は全角(16)になる。
        /// 8しか空けないので次の文字がアイコンへ食い込んで重なって見えていた
        /// </summary>
        string CellFont(TMP_FontAsset fa)
        {
            if (fa != null && FontProvider.IsMeasured(fa.name))
                return fa.name;
            return fontname;
        }

        void Apply()
        {
            var t = text;
            var info = t.textInfo;
            if (info == null || info.characterCount == 0)
                return;

            float size = t.fontSize;
            if (size <= 0f)
                return;

            int line = -1;
            float pen = 0f;
            float origin = 0f;
            bool dirty = false;

            int count = info.characterCount;
            for (int i = 0; i < count; ++i)
            {
                var ci = info.characterInfo[i];
                if (ci.lineNumber != line)
                {
                    //行頭は動かさない。そこからの積み上げで揃える
                    line = ci.lineNumber;
                    pen = 0f;
                    origin = ci.origin;
                }

                float cell = Utils.GetCharWidth(CellFont(ci.fontAsset), ci.character, size);
                if (!ci.isVisible)
                {
                    pen += cell;
                    continue;
                }

                var mesh = info.meshInfo[ci.materialReferenceIndex];
                var verts = mesh.vertices;
                int vi = ci.vertexIndex;
                if (verts == null || vi + 3 >= verts.Length)
                {
                    pen += cell;
                    continue;
                }

                float left = verts[vi].x;
                float glyph = verts[vi + 2].x - left;
                float target = origin + pen;

                float shift;
                float stretch = 0f;
                if (IsHorizontalRule(ci.character) && glyph > 0f && glyph < cell)
                {
                    //横線は隣と繋げたいので、インクをセル幅いっぱいへ広げる
                    shift = target - left;
                    stretch = cell - glyph;
                }
                else
                {
                    //インクの外形ではなく送り幅の箱を中央へ置く。
                    //'┌'のようにインクが中心から右へ寄る字形は、外形で中央を取ると
                    //縦の画が左へずれて角だけ飛び出して見える。
                    //字形が持つ本来の位置関係は送り幅基準なら保たれる
                    float advance = ci.xAdvance - ci.origin;
                    if (advance <= 0f)
                        advance = glyph;
                    shift = target + (cell - advance) * 0.5f - ci.origin;
                }

                if (shift != 0f || stretch != 0f)
                {
                    verts[vi + 0].x += shift;
                    verts[vi + 1].x += shift;
                    verts[vi + 2].x += shift + stretch;
                    verts[vi + 3].x += shift + stretch;
                    dirty = true;
                }
                pen += cell;
            }

            if (dirty)
                t.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
