using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace uEmuera
{
    /// <summary>
    /// TMP_FontAssetを実行時に作る。
    /// ゲームフォルダのfont/にあるファイルはそのまま読み込めるので、
    /// ゲーム固有のフォントをアプリへ同梱する必要がない。
    ///
    /// 必ずメインスレッドから呼ぶこと。
    /// </summary>
    public static class TMPFonts
    {
        /// <summary>
        /// アトラスの基準サイズ。SDFなので拡大縮小に耐える。
        /// 表示が甘く見えるようならSMOOTH_HINTEDへ変える余地がある
        /// </summary>
        const int kSamplingPointSize = 90;
        const int kAtlasPadding = 9;
        const int kAtlasSize = 1024;
        const GlyphRenderMode kRenderMode = GlyphRenderMode.SDFAA;

        static readonly Dictionary<string, TMP_FontAsset> assets_ =
            new Dictionary<string, TMP_FontAsset>();

        /// <summary>
        /// 名前からTMP_FontAssetを得る。
        /// ゲームフォルダのfont/を優先し、無ければ埋め込みフォントを使う。
        /// どちらも駄目ならnull
        /// </summary>
        public static TMP_FontAsset Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            var key = name.ToUpper();
            TMP_FontAsset asset;
            if (assets_.TryGetValue(key, out asset))
                return asset;

            asset = Build(name);
            //失敗もキャッシュする。毎回作り直そうとして重くなるのを避ける。
            //先に入れておく事で、下の連鎖付けから再び呼ばれても止まる
            assets_[key] = asset;
            if (asset != null)
            {
                //<font="名前">タグはMaterialReferenceManagerから名前で引かれる。
                //実行時に作ったフォントは自分で登録しないと解決できない
                try { MaterialReferenceManager.AddFontAsset(asset); }
                catch (System.Exception e) { Debug.LogWarning("TMPFonts: 등록 실패 " + e.Message); }
                AttachDefaultFallback(asset);
                //<font=～>で差し込まれるフォントも本文と同じ行送りにする。
                //連鎖(fallback)にだけ揃えていたので、タグで呼ばれるぉんFontのような
                //フォントは行の高さを押し上げ、上下の行に食い込んでいた
                MatchLineMetrics(asset, reference_);
            }
            return asset;
        }

        /// <summary>
        /// 行の高さの基準にする本文フォント。本文が決まった時に一度だけ渡す
        /// </summary>
        public static void SetReference(TMP_FontAsset asset)
        {
            if (asset == null)
                return;
            reference_ = asset;
            //既に作ってあった物も揃え直す
            var iter = assets_.GetEnumerator();
            var list = new List<TMP_FontAsset>();
            while (iter.MoveNext())
            {
                if (iter.Current.Value != null)
                    list.Add(iter.Current.Value);
            }
            for (int i = 0; i < list.Count; ++i)
                MatchLineMetrics(list[i], asset);
        }
        static TMP_FontAsset reference_ = null;

        /// <summary>
        /// 行の高さを決める指標を、基準フォントの物へ揃える。
        ///
        /// TMPは1行の高さを「その行に出てくるフォントの中で最大の値」で決める。
        /// ハングルがNoto Sans CJK KRで描かれると、そのフォントの行送りが
        /// ＭＳ ゴシックより遥かに大きいため、行の高さが16→23.17へ跳ね上がり、
        /// 16px間隔で並べているコンソールでは上下の行に食い込む。
        ///
        /// どのフォントも同じ基準サイズで作っているので、値をそのまま写せばよい
        /// </summary>
        static void MatchLineMetrics(TMP_FontAsset asset, TMP_FontAsset reference)
        {
            if (asset == null || reference == null || asset == reference)
                return;
            var refFi = reference.faceInfo;
            var fi = asset.faceInfo;
            if (fi.pointSize != refFi.pointSize || fi.scale != refFi.scale)
                return;   //基準が違うと単純な写しでは合わない
            if (fi.ascentLine == refFi.ascentLine && fi.descentLine == refFi.descentLine
                && fi.lineHeight == refFi.lineHeight)
                return;

            fi.ascentLine = refFi.ascentLine;
            fi.descentLine = refFi.descentLine;
            fi.lineHeight = refFi.lineHeight;
            fi.baseline = refFi.baseline;
            asset.faceInfo = fi;
        }

        /// <summary>
        /// 作った全てのフォントへ既定の連鎖を付ける。
        ///
        /// SETFONTや&lt;font&gt;タグで切り替わったフォントにも字形の穴はある。
        /// 例えばＭＳ Ｐゴシックにハングルは無く、連鎖が無いとその区間だけ豆腐になる。
        /// 本文フォントにしか連鎖を張っていなかったのが取りこぼしの原因だった
        /// </summary>
        static void AttachDefaultFallback(TMP_FontAsset asset)
        {
            if (asset == null)
                return;
            var list = new List<TMP_FontAsset>();
            for (int i = 0; i < kDefaultNames.Length; ++i)
            {
                var fb = Get(kDefaultNames[i]);
                if (fb != null && fb != asset && !list.Contains(fb))
                    list.Add(fb);
            }
            var sys = GetSystemCJK();
            if (sys != null && sys != asset && !list.Contains(sys))
                list.Add(sys);
            //行の高さが跳ねないよう、連鎖に入るフォントの指標を本体へ合わせる
            for (int i = 0; i < list.Count; ++i)
                MatchLineMetrics(list[i], asset);
            if (list.Count > 0)
                asset.fallbackFontAssetTable = list;
        }

        /// <summary>
        /// 名前で引けなかった時に諦めず既定へ落とす。
        /// 以前のFontUtils.GetFontは失敗すると既定フォントを返していて、
        /// それが安全網になっていた。TMPでも同じ保証が要る。
        /// フォントが無いままだと字形の供給源が無く、全て豆腐になる
        /// </summary>
        public static TMP_FontAsset GetOrDefault(string name)
        {
            var asset = Get(name);
            if (asset != null)
                return asset;

            //ここは行を描く度に通る。代替の結果を覚えておかないと、
            //探索と警告を毎行くり返して重くなる
            var key = name == null ? "" : name.ToUpper();
            TMP_FontAsset cached;
            if (substitutes_.TryGetValue(key, out cached))
                return cached;

            for (int i = 0; i < kDefaultNames.Length; ++i)
            {
                asset = Get(kDefaultNames[i]);
                if (asset != null)
                {
                    Debug.LogWarning("TMPFonts: [" + name + "]를 찾지 못해 ["
                        + kDefaultNames[i] + "]로 대체");
                    substitutes_[key] = asset;
                    return asset;
                }
            }
            asset = GetSystemCJK();
            if (asset == null)
                Debug.LogError("TMPFonts: 사용할 수 있는 폰트가 하나도 없습니다 (요청=" + name + ")");
            substitutes_[key] = asset;
            return asset;
        }

        /// <summary>名前で引けなかったフォントの代替。GetOrDefaultの結果を覚える</summary>
        static readonly Dictionary<string, TMP_FontAsset> substitutes_ =
            new Dictionary<string, TMP_FontAsset>();

        static readonly string[] kDefaultNames =
        {
            "ＭＳ ゴシック", "ＭＳ Ｐゴシック",
        };

        static TMP_FontAsset Build(string name)
        {
            var path = FontProvider.GetGameFontPath(name);
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var fromFile = TMP_FontAsset.CreateFontAsset(
                        path, 0, kSamplingPointSize, kAtlasPadding,
                        kRenderMode, kAtlasSize, kAtlasSize);
                    if (fromFile != null)
                    {
                        //hashCodeはnameから作られ、<font="名前">タグの解決に使われる。
                        //名前を変えたら作り直させないと引けない
                        fromFile.name = name;
                        fromFile.ReadFontAssetDefinition();
                        Debug.Log("TMPFonts: [" + name + "] 게임 폴더에서 생성 " + path);
                        return fromFile;
                    }
                    Debug.LogWarning("TMPFonts: [" + name + "] 게임 폴더 파일 로드 실패 " + path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("TMPFonts: [" + name + "] " + e.Message);
                }
            }

            //FontUtils.GetFontは見つからない時に既定フォントを返してしまい、
            //別のフォントを掴んだまま成功扱いになる。ここでは直接読んで失敗を検出する
            var mapped = FontProvider.MapToFileName(name);
            var embedded = Resources.Load<Font>("Fonts/" + mapped);
            if (embedded != null)
            {
                try
                {
                    var fromFont = TMP_FontAsset.CreateFontAsset(
                        embedded, kSamplingPointSize, kAtlasPadding,
                        kRenderMode, kAtlasSize, kAtlasSize);
                    if (fromFont != null)
                    {
                        fromFont.name = name;
                        fromFont.ReadFontAssetDefinition();
                        Debug.Log("TMPFonts: [" + name + "] 임베드 폰트에서 생성");
                        return fromFont;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("TMPFonts: [" + name + "] " + e.Message);
                }
            }

            //最後の頼みはOSに入っているフォント。
            //同梱を減らしてもハングルなどの字形を確保できる
            try
            {
                var fromOS = TMP_FontAsset.CreateFontAsset(name, "Regular", kSamplingPointSize);
                if (fromOS == null && mapped != name)
                    fromOS = TMP_FontAsset.CreateFontAsset(mapped, "Regular", kSamplingPointSize);
                if (fromOS != null)
                {
                    fromOS.name = name;
                    fromOS.ReadFontAssetDefinition();
                    Debug.Log("TMPFonts: [" + name + "] OS 폰트에서 생성");
                }
                return fromOS;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TMPFonts: [" + name + "] OS 폰트 " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 字形が無い文字を他のフォントで補うための連鎖を張る。
        /// ＭＳ ゴシックにハングルが無いなど、1つのフォントでは足りない事が多い
        /// </summary>
        public static void SetFallback(TMP_FontAsset primary, params string[] fallbackNames)
        {
            if (primary == null || fallbackNames == null)
                return;
            var list = new List<TMP_FontAsset>();
            for (int i = 0; i < fallbackNames.Length; ++i)
            {
                var fb = Get(fallbackNames[i]);
                if (fb != null && fb != primary && !list.Contains(fb))
                    list.Add(fb);
            }
            //同梱フォントで足りない字形は端末のフォントで補う
            var sys = GetSystemCJK();
            if (sys != null && sys != primary && !list.Contains(sys))
                list.Add(sys);
            primary.fallbackFontAssetTable = list;
        }

        /// <summary>
        /// 端末に元から入っているCJKフォント。同梱フォントを減らした分をここで補う。
        /// Windowsは名前で引けるが、Androidは名前が引けない事があるので
        /// /system/fonts の実ファイルも直接見る
        /// </summary>
        static readonly string[] kSystemCjkNames =
        {
            "Malgun Gothic", "Gulim", "GulimChe", "Dotum",   //Windows
            "Noto Sans CJK KR", "Noto Sans KR", "Noto Sans CJK", "Droid Sans Fallback",
        };
        static readonly string[] kSystemCjkPaths =
        {
            "/system/fonts/NotoSansCJK-Regular.ttc",
            "/system/fonts/NotoSansKR-Regular.otf",
            "/system/fonts/NotoSansCJKkr-Regular.otf",
            "/system/fonts/DroidSansFallback.ttf",
            "/system/fonts/DroidSansFallbackFull.ttf",
        };

        static TMP_FontAsset system_cjk_;
        static bool system_cjk_tried_;

        /// <summary>この字が出せるか。動的フォントなので追加を試させる</summary>
        static bool CanRender(TMP_FontAsset asset, char c)
        {
            if (asset == null)
                return false;
            try { return asset.HasCharacter(c, false, true); }
            catch { return false; }
        }

        /// <summary>
        /// 端末のCJKフォント。同梱を減らした分の字形をここで補う。
        /// ハングルと漢字の両方が出せる物を選ぶ。
        /// /system/fonts のファイル名は端末ごとに違うので、名前一覧では足りず
        /// フォルダを直接漁って字が出せるかで判定する
        /// </summary>
        public static TMP_FontAsset GetSystemCJK()
        {
            if (system_cjk_tried_)
                return system_cjk_;
            system_cjk_tried_ = true;

            //1. 名前で引ける環境(主にWindows)
            for (int i = 0; i < kSystemCjkNames.Length; ++i)
            {
                var byName = Get(kSystemCjkNames[i]);
                if (CanRender(byName, '한'))
                {
                    system_cjk_ = byName;
                    Debug.Log("TMPFonts: 시스템 CJK [" + kSystemCjkNames[i] + "]");
                    return system_cjk_;
                }
            }

            //2. 端末のフォントフォルダを漁る。ハングルが出せる物を優先
            TMP_FontAsset cjkOnly = null;
            foreach (var dir in kSystemFontDirs)
            {
                string[] files;
                try
                {
                    if (!System.IO.Directory.Exists(dir))
                        continue;
                    files = System.IO.Directory.GetFiles(dir);
                }
                catch { continue; }

                System.Array.Sort(files, (a, b) => Score(b).CompareTo(Score(a)));
                for (int i = 0; i < files.Length; ++i)
                {
                    var path = files[i];
                    var ext = System.IO.Path.GetExtension(path).ToLower();
                    if (ext != ".ttf" && ext != ".otf" && ext != ".ttc" && ext != ".otc")
                        continue;
                    if (Score(path) <= 0)
                        continue;

                    TMP_FontAsset asset = null;
                    try
                    {
                        asset = TMP_FontAsset.CreateFontAsset(
                            path, 0, kSamplingPointSize, kAtlasPadding,
                            kRenderMode, kAtlasSize, kAtlasSize);
                    }
                    catch { continue; }
                    if (asset == null)
                        continue;

                    if (CanRender(asset, '한'))
                    {
                        asset.name = "SystemCJK";
                        asset.ReadFontAssetDefinition();
                        try { MaterialReferenceManager.AddFontAsset(asset); } catch { }
                        system_cjk_ = asset;
                        Debug.Log("TMPFonts: 시스템 CJK(한글) " + path);
                        return system_cjk_;
                    }
                    //ハングルは無いが漢字はある物を控えにしておく
                    if (cjkOnly == null && CanRender(asset, '漢'))
                        cjkOnly = asset;
                }
            }

            if (cjkOnly != null)
            {
                cjkOnly.name = "SystemCJK";
                cjkOnly.ReadFontAssetDefinition();
                try { MaterialReferenceManager.AddFontAsset(cjkOnly); } catch { }
                system_cjk_ = cjkOnly;
                Debug.LogWarning("TMPFonts: 시스템 폰트에 한글이 없어 한자만 있는 폰트로 대체");
                return system_cjk_;
            }

            Debug.LogWarning("TMPFonts: 시스템 CJK 폰트를 찾지 못했습니다");
            return null;
        }

        static readonly string[] kSystemFontDirs =
        {
            "/system/fonts", "/system/font", "/data/fonts",
            "/system/fonts/Noto", "/product/fonts",
        };

        /// <summary>ファイル名からCJKらしさを点数化する。高いものから試す</summary>
        static int Score(string path)
        {
            var n = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            if (n.Contains("kr") || n.Contains("korea") || n.Contains("gothic")
                || n.Contains("gulim") || n.Contains("dotum") || n.Contains("batang"))
                return 3;
            if (n.Contains("cjk") || n.Contains("fallback"))
                return 2;
            if (n.Contains("noto") || n.Contains("droid"))
                return 1;
            return 0;
        }

        /// <summary>作成済みのフォントを捨てる。ゲームを切り替える時に呼ぶ</summary>
        public static void Clear()
        {
            foreach (var kv in assets_)
            {
                if (kv.Value != null)
                    Object.Destroy(kv.Value);
            }
            assets_.Clear();
            substitutes_.Clear();
        }
    }
}
