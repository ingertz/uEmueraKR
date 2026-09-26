using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace uEmuera
{
    /// <summary>
    /// フォントの実体を解決し、実測の文字幅を提供する。
    ///
    /// 探索順は ゲームフォルダのfont/ → Unityに埋め込んだResources/Fonts の順。
    /// ゲーム固有のフォント(ぉんFont, game-icons など)を同梱せずに使えるようにするため、
    /// ゲームフォルダを優先する。
    ///
    /// 文字幅の計測はUnityのメインスレッドでしか行えないが、
    /// レイアウトはエミュレータ側のワーカースレッドで走る。
    /// そのため起動時に一度だけ表を作り、以降は表を読むだけにする。
    /// </summary>
    public static class FontProvider
    {
        /// <summary>計測に使う基準サイズ。実使用サイズへは比例で換算する</summary>
        const int kReferenceSize = 64;

        /// <summary>
        /// configやERBが指す名前と、実ファイル名(拡張子なし)の対応。
        /// ここに無い名前はそのままファイル名として探す
        /// </summary>
        static readonly Dictionary<string, string> alias = new Dictionary<string, string>
        {
            {"ＭＳ ゴシック", "MS Gothic"},
            {"ＭＳ Ｐゴシック", "MS PGothic"},
            {"돋움", "Dotum"},
            {"돋움체", "Dotumche"},
            {"굴림", "Gulim"},
            {"굴림체", "GulimChe"},
            {"xonFont", "ぉんFont"},
            {"xonFontH", "ぉんFont半角"},
            {"ぉんFont", "ぉんFont"},
            {"ぉんFont半角", "ぉんFont半角"},
        };

        static readonly string[] kExtensions = { ".ttf", ".otf", ".ttc", ".TTF", ".OTF", ".TTC" };

        /// <summary>ゲームフォルダのfont/にあるファイル。キーは拡張子なしの大文字</summary>
        static Dictionary<string, string> game_files_ = new Dictionary<string, string>();
        /// <summary>フォント名(大文字) -> 文字コード -> 基準サイズでの送り幅</summary>
        static Dictionary<string, Dictionary<int, float>> metrics_ =
            new Dictionary<string, Dictionary<int, float>>();

        static bool engine_ready_ = false;
        static string scanned_dir_ = null;

        /// <summary>
        /// ゲームフォルダが確定した後、メインスレッドから呼ぶ。
        /// 同じフォルダに対して二度目以降は何もしない
        /// </summary>
        public static void Scan()
        {
            var dir = MinorShift._Library.Sys.ExeDir;
            if (string.IsNullOrEmpty(dir))
                return;
            var fontdir = Utils.ResolvePath(dir + "font/");
            if (scanned_dir_ == fontdir)
                return;
            scanned_dir_ = fontdir;

            var files = new Dictionary<string, string>();
            //eraメガテンP版のように、本体一式がData/の中に在って
            //font/だけ一つ上に置かれている配布物がある。
            //近い方を優先したいので、親を先に読んで同名は上書きさせる。
            //
            //親を見るのは潜って見つけた配布物の時だけ。
            //普通の配布物で親を見ると、ゲームを並べたフォルダに誰かが置いた
            //無関係なフォントを全ゲームが拾ってしまい、字幅が変わって
            //表示が崩れる
            if (MinorShift._Library.Sys.SourceIsNested)
            {
                var parent = ParentDir(dir);
                if (parent != null)
                    CollectFonts(Utils.ResolvePath(parent + "font/"), files);
            }
            CollectFonts(fontdir, files);

            game_files_ = files;
            metrics_ = new Dictionary<string, Dictionary<int, float>>();

            Debug.Log("FontProvider: " + fontdir + " 에서 폰트 " + files.Count + "개 발견");
        }

        /// <summary>末尾に/が付いたパスの、一つ上のフォルダ。無ければnull</summary>
        static string ParentDir(string dir)
        {
            var d = dir.TrimEnd('/');
            var i = d.LastIndexOf('/');
            return i <= 0 ? null : d.Substring(0, i + 1);
        }

        static void CollectFonts(string fontdir, Dictionary<string, string> files)
        {
            if (string.IsNullOrEmpty(fontdir))
                return;
            try
            {
                if (!Directory.Exists(fontdir))
                    return;
                foreach (var path in Directory.GetFiles(fontdir))
                {
                    var ext = Path.GetExtension(path);
                    bool ok = false;
                    for (int i = 0; i < kExtensions.Length; ++i)
                        if (string.Equals(ext, kExtensions[i], System.StringComparison.OrdinalIgnoreCase))
                        { ok = true; break; }
                    if (!ok)
                        continue;
                    files[Path.GetFileNameWithoutExtension(path).ToUpper()] = path;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("FontProvider: font 폴더 조회 실패 " + e.Message);
            }
        }

        /// <summary>configやERBの名前を、実ファイル名(拡張子なし)へ直す</summary>
        public static string MapToFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            string mapped;
            return alias.TryGetValue(name, out mapped) ? mapped : name;
        }

        /// <summary>ゲームフォルダで見つかったフォント名(拡張子なし)</summary>
        public static List<string> GameFontNames()
        {
            var ret = new List<string>();
            foreach (var kv in game_files_)
                ret.Add(Path.GetFileNameWithoutExtension(kv.Value));
            return ret;
        }

        /// <summary>
        /// 名前に対応するゲームフォルダ内のフォントファイル。無ければnull
        /// </summary>
        public static string GetGameFontPath(string name)
        {
            if (string.IsNullOrEmpty(name) || game_files_.Count == 0)
                return null;
            string path = null;
            string mapped;
            if (alias.TryGetValue(name, out mapped) && game_files_.TryGetValue(mapped.ToUpper(), out path))
                return path;
            if (game_files_.TryGetValue(name.ToUpper(), out path))
                return path;
            return null;
        }

        /// <summary>
        /// CHKFONT用。ゲームフォルダのfont/か、アプリに埋め込んだフォントに実在するか。
        /// FontUtils.GetFontは見つからないと既定フォントを返すので、存在確認には使えない。
        /// 埋め込みの確認(Resources.Load)はメインスレッドで行う
        /// </summary>
        public static bool HasFont(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (GetGameFontPath(name) != null)
                return true;
            bool found = false;
            var mapped = MapToFileName(name);
            using (var done = new System.Threading.ManualResetEvent(false))
            {
                SpriteManager.RunOnMainThread(() =>
                {
                    try
                    {
                        found = FontUtils.HasEmbeddedFont(name)
                            || (mapped != name && FontUtils.HasEmbeddedFont(mapped));
                    }
                    finally { done.Set(); }
                });
                done.WaitOne();
            }
            return found;
        }

        /// <summary>
        /// 実測の送り幅。まだ計測していない文字や、フォントが見つからない場合はfalse。
        /// 呼び出し元は従来の半角/全角判定へ退避すること
        /// </summary>
        public static bool TryGetAdvance(string fontName, int unicode, float fontSize, out float advance)
        {
            advance = 0f;
            if (string.IsNullOrEmpty(fontName))
                return false;
            Dictionary<int, float> table;
            if (!metrics_.TryGetValue(fontName.ToUpper(), out table))
                return false;
            float raw;
            if (!table.TryGetValue(unicode, out raw))
                return false;
            advance = raw * fontSize / kReferenceSize;
            return true;
        }

        /// <summary>計測済みかどうか</summary>
        public static bool IsMeasured(string fontName)
        {
            return !string.IsNullOrEmpty(fontName) && metrics_.ContainsKey(fontName.ToUpper());
        }

        /// <summary>
        /// 計測がまだなら行う。ワーカースレッドから呼ばれた場合は
        /// メインスレッドへ投げて終わるまで待つ(フォント毎に一度きり)
        /// </summary>
        public static bool EnsureMeasured(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return false;
            if (metrics_.ContainsKey(fontName.ToUpper()))
                return true;

            bool result = false;
            var waitHandle = new System.Threading.ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() =>
            {
                try { result = Measure(fontName); }
                catch (System.Exception e) { Debug.LogWarning("FontProvider: " + e.Message); }
                finally { waitHandle.Set(); }
            });
            waitHandle.WaitOne();
            return result;
        }

        /// <summary>
        /// フォントの文字幅を計測して表に入れる。必ずメインスレッドから呼ぶこと。
        /// ゲームフォルダにあればそれを、無ければ埋め込みフォントを使う
        /// </summary>
        public static bool Measure(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return false;
            var key = fontName.ToUpper();
            if (metrics_.ContainsKey(key))
                return true;

            if (!engine_ready_)
            {
                if (FontEngine.InitializeFontEngine() != FontEngineError.Success)
                {
                    Debug.LogWarning("FontProvider: FontEngine 초기화 실패");
                    return false;
                }
                engine_ready_ = true;
            }

            var loaded = false;
            var path = GetGameFontPath(fontName);
            if (!string.IsNullOrEmpty(path))
                loaded = FontEngine.LoadFontFace(path, kReferenceSize, 0) == FontEngineError.Success;

            if (!loaded)
            {
                var embedded = FontUtils.GetFont(fontName);
                if (embedded != null)
                    loaded = FontEngine.LoadFontFace(embedded, kReferenceSize) == FontEngineError.Success;
            }
            if (!loaded)
                return false;

            var table = new Dictionary<int, float>();
            for (int r = 0; r < kMeasureRanges.Length; r += 2)
            {
                for (int u = kMeasureRanges[r]; u <= kMeasureRanges[r + 1]; ++u)
                {
                    Glyph glyph;
                    if (FontEngine.TryGetGlyphWithUnicodeValue((uint)u, GlyphLoadFlags.LOAD_NO_BITMAP, out glyph))
                        table[u] = glyph.metrics.horizontalAdvance;
                }
            }
            metrics_[key] = table;
            Debug.Log("FontProvider: [" + fontName + "] 계측 완료 " + table.Count + "자 ("
                + (string.IsNullOrEmpty(path) ? "임베드" : path) + ")");
            return true;
        }

        /// <summary>
        /// 計測する文字の範囲。開始・終了の組。
        /// 表に無い文字(漢字・ハングルなど)は必ず全角なので従来判定で足りる
        /// </summary>
        static readonly int[] kMeasureRanges =
        {
            0x0020, 0x00FF,   // ASCII + Latin-1補助。ぉんFontのアイコンはここに入る
            0x0100, 0x017F,   // ラテン拡張A
            0x0370, 0x03FF,   // ギリシア。α β γ は全角の書体が多く、判定では決められない
            0x0400, 0x04FF,   // キリル
            0x2000, 0x206F,   // 一般句読点
            0x2070, 0x209F,   // 上付き・下付き
            0x20A0, 0x20BF,   // 通貨記号
            0x2100, 0x214F,   // 文字様記号。№ ™ ℃
            0x2150, 0x218F,   // 数字の形。Ⅰ Ⅱ Ⅲ
            0x2190, 0x21FF,   // 矢印
            0x2200, 0x22FF,   // 数学記号。≦ ≠ ∀
            0x2300, 0x23FF,   // その他の技術用記号
            0x2460, 0x24FF,   // 囲み英数字。① ② ⑩ はera系で多用される
            0x2500, 0x257F,   // 罫線素片。枠線UIが依存する
            0x2580, 0x25FF,   // ブロック・幾何学模様
            0x2600, 0x26FF,   // その他の記号
            0x2700, 0x27BF,   // 装飾記号。✓ ✕ ★
            0x3000, 0x303F,   // CJKの記号と句読点
            0x3040, 0x30FF,   // かな
            0x3190, 0x319F,   // 漢文用記号
            0x3200, 0x33FF,   // CJK互換。㈱ ℡ ㎝
            0xFE30, 0xFE4F,   // CJK互換形。縦書き用の括弧
            //半角形を落としていたため、ｱｲｳや｢｣が全角として並べられていた。
            //ここは全角形(FF00-FF60)と地続きなので、まとめて計測する
            0xFF00, 0xFFEE,
        };
    }
}
