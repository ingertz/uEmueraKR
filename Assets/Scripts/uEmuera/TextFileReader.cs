using System.IO;
using System.Text;

namespace uEmuera
{
    /// <summary>
    /// ERB/CSVなどの本文を、書かれている文字コードを判別して読む。
    ///
    /// 判定は BOM → 妥当なUTF-8 → レガシー の順。
    /// 今動いているゲームはすべてUTF-8なので2段目で決まり、挙動は変わらない。
    /// 3段目に落ちるのは、これまで文字化けして読めなかったファイルだけ。
    /// </summary>
    public static class TextFileReader
    {
        //不正なバイト列で例外を投げる復号器。判別と復号を一度で済ませるために使う
        static readonly UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// UTF-8として矛盾なく解釈できるか。
        /// 冗長な符号化や範囲外も弾くので、Shift-JISの日本語がたまたま
        /// 通ってしまうことはほぼ無い
        /// </summary>
        public static bool IsValidUtf8(byte[] b, int length)
        {
            int i = 0;
            while (i < length)
            {
                byte c = b[i];
                if (c < 0x80) { ++i; continue; }

                int following;
                int cp;
                if (c >= 0xC2 && c <= 0xDF) { following = 1; cp = c & 0x1F; }
                else if (c >= 0xE0 && c <= 0xEF) { following = 2; cp = c & 0x0F; }
                else if (c >= 0xF0 && c <= 0xF4) { following = 3; cp = c & 0x07; }
                else return false;   //0xC0,0xC1,0xF5-0xFF や、いきなりの継続バイト

                //末尾で切れているものは不正とはしない
                if (i + following >= length)
                    return true;

                for (int k = 1; k <= following; ++k)
                {
                    byte t = b[i + k];
                    if (t < 0x80 || t > 0xBF)
                        return false;
                    cp = (cp << 6) | (t & 0x3F);
                }
                //冗長な符号化とサロゲート単独を弾く。
                //これがあるのでShift-JISの日本語がUTF-8として通ることはまず無い
                if (following == 2 && cp < 0x800) return false;
                if (following == 3 && (cp < 0x10000 || cp > 0x10FFFF)) return false;
                if (cp >= 0xD800 && cp <= 0xDFFF) return false;

                i += following + 1;
            }
            return true;
        }

        /// <summary>
        /// レガシー扱いになった時に使う文字コード。
        /// 今はCP932(Shift-JIS)のみ内蔵。era系の元データはほぼこれ
        /// </summary>
        static string DecodeLegacy(byte[] bytes)
        {
            return Cp932.GetString(bytes);
        }

        /// <summary>ファイル全体を文字列として読む。読めなければnull</summary>
        public static string ReadAllText(string path)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch { return null; }
            return Decode(bytes);
        }

        public static string Decode(byte[] bytes)
        {
            if (bytes == null)
                return null;
            int n = bytes.Length;
            if (n == 0)
                return string.Empty;

            //BOMがあるものはそれに従う
            if (n >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, n - 3);
            if (n >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, n - 2);
            if (n >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, n - 2);

            //検査してから復号すると全体を二度なめる事になる。
            //ERBが1万ファイル・280万行あるゲームでは、この一度分が数秒に効く。
            //まず厳密な復号を試し、弾かれた時だけ詳しく調べる
            try
            {
                return strictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            { }

            //末尾で多バイト文字が切れているだけならUTF-8として読む。
            //厳密な復号はこれも弾くが、レガシー扱いにすると全文が化ける
            if (IsValidUtf8(bytes, n))
                return Encoding.UTF8.GetString(bytes);

            return DecodeLegacy(bytes);
        }

        /// <summary>行単位で読むためのTextReaderを返す</summary>
        public static TextReader OpenText(string path)
        {
            var text = ReadAllText(path);
            return text == null ? null : new StringReader(text);
        }

        /// <summary>File.ReadAllLinesの置き換え</summary>
        public static string[] ReadAllLines(string path)
        {
            var text = ReadAllText(path);
            if (text == null)
                return null;
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }
    }
}
