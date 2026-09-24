using System;
using System.IO;
using System.Collections.Generic;

namespace uEmuera
{
    public static class Logger
    {
        public static void Info(object content)
        {
            if(info == null)
                return;
            info(content);
        }
        public static void Warn(object content)
        {
            if(warn == null)
                return;
            warn(content);
        }
        public static void Error(object content)
        {
            if(error == null)
                return;
            error(content);
        }
        public static System.Action<object> info;
        public static System.Action<object> warn;
        public static System.Action<object> error;
    }

    public static class Utils
    {
        public static void SetSHIFTJIS_to_UTF8Dict(Dictionary<string, string> dict)
        {
            shiftjis_to_utf8 = dict;
        }
        public static void SetUTF8ZHCN_to_UTF8Dict(Dictionary<string, string> dict)
        {
            utf8zhcn_to_utf8 = dict;
        }
        public static string SHIFTJIS_to_UTF8(string text, string md5)
        {
            if(shiftjis_to_utf8 == null)
                return null;
            string result = null;
            //md5が取れない行もある。Dictionaryはnullキーで例外になるので避ける
            if(!string.IsNullOrEmpty(md5))
                shiftjis_to_utf8.TryGetValue(md5, out result);
            if(string.IsNullOrEmpty(result))
                utf8zhcn_to_utf8.TryGetValue(text, out result);
            return result;
        }
        static Dictionary<string, string> shiftjis_to_utf8;
        static Dictionary<string, string> utf8zhcn_to_utf8;

        /// <summary>
        /// 标准化目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizePath(string path)
        {
            var ps = path.Split('/', '\\');
            var n = "";
            for(int i = 0; i < ps.Length - 1; ++i)
            {
                var p = ps[i];
                if(string.IsNullOrEmpty(p))
                    continue;
                n = string.Concat(n, p, '/');
            }
            if(ps.Length == 1)
                return ps[0];
            else if(ps.Length > 0)
                return n + ps[ps.Length - 1];
            return "";
        }

        /// <summary>
        /// ERBが書いたパスの区切りを揃える。
        /// 「タイトル画像\タイトル000.webp」のように円記号で書かれている事が多く、
        /// Windowsでは通るがAndroidでは区切りとみなされずファイルが見つからない
        /// </summary>
        public static string NormalizeGamePath(string path)
        {
            if(string.IsNullOrEmpty(path))
                return path;
            return path.IndexOf('\\') >= 0 ? path.Replace('\\', '/') : path;
        }

        /// <summary>
        /// ERBやCSVに書かれた綴りを、実際に在るファイル・フォルダの綴りへ合わせる。
        ///
        /// era本体はWindows前提なので、大小が実体と食い違っていても動いてしまう。
        /// 「resources/PARTS」を「parts/」と書く、拡張子だけ小文字、といった物は珍しくない。
        /// 大小を区別するAndroidではそのまま開けず、画像だけが出ないという形で表れる。
        ///
        /// 実在すればそのまま返すので、Windowsでは一切余計な事をしない
        /// </summary>
        public static string ResolvePath(string path)
        {
            if(string.IsNullOrEmpty(path))
                return path;
            //実在するなら何もしない。この判定は毎回やる。
            //ここを覚えてしまうと、遊んでいる最中に作られたファイルを見落とす
            if(File.Exists(path) || Directory.Exists(path))
                return path;

            //綴りを辿る所だけ覚える。同じ画像を何度も引きに来るので、
            //その度に一段ずつ辿り直すと積み重なって効いてくる
            if(resolved_ == null)
                resolved_ = new Dictionary<string, string>();
            string done;
            if(resolved_.TryGetValue(path, out done))
                return done;

            var norm = path.IndexOf('\\') >= 0 ? path.Replace('\\', '/') : path;
            bool isdir = norm[norm.Length - 1] == '/';
            var parts = norm.Split('/');
            if(parts.Length < 2)
                return resolved_[path] = path;

            //先頭要素はルート。絶対パスなら空文字、Windowsなら"C:"
            var cur = parts[0].Length == 0 ? "/" : parts[0];
            for(int i = 1; i < parts.Length; ++i)
            {
                var name = parts[i];
                if(name.Length == 0)
                    continue;
                var next = cur[cur.Length - 1] == '/' ? cur + name : cur + "/" + name;
                if(Directory.Exists(next) || File.Exists(next))
                {
                    cur = next;
                    continue;
                }
                var real = FindEntryIgnoreCase(cur, name);
                if(real == null)
                    //見つからないなら元のまま返して呼び元に判断させる
                    return resolved_[path] = path;
                cur = cur[cur.Length - 1] == '/' ? cur + real : cur + "/" + real;
            }
            return resolved_[path] = (isdir ? cur + "/" : cur);
        }
        static Dictionary<string, string> resolved_ = null;

        static string FindEntryIgnoreCase(string dir, string name)
        {
            var e = GetDirEntries(dir, false);
            if(e == null)
                return null;
            string real;
            if(e.map.TryGetValue(name, out real))
                return real;
            //実行中に作られたファイルかもしれない。更新されていれば読み直す
            if(Directory.GetLastWriteTime(dir) == e.stamp)
                return null;
            e = GetDirEntries(dir, true);
            if(e == null)
                return null;
            return e.map.TryGetValue(name, out real) ? real : null;
        }

        class DirEntries
        {
            public DateTime stamp;
            public Dictionary<string, string> map;
        }
        static DirEntries GetDirEntries(string dir, bool force)
        {
            if(dir_entries_ == null)
                dir_entries_ = new Dictionary<string, DirEntries>();
            DirEntries e;
            if(!force && dir_entries_.TryGetValue(dir, out e))
                return e;
            if(!Directory.Exists(dir))
                return null;
            e = new DirEntries
            {
                stamp = Directory.GetLastWriteTime(dir),
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            };
            try
            {
                var entries = Directory.GetFileSystemEntries(dir);
                for(int i = 0; i < entries.Length; ++i)
                {
                    var n = Path.GetFileName(entries[i]);
                    if(!e.map.ContainsKey(n))
                        e.map.Add(n, n);
                }
            }
            catch(Exception)
            { }
            dir_entries_[dir] = e;
            return e;
        }
        static Dictionary<string, DirEntries> dir_entries_ = null;

        public static void ResolvePathClear()
        {
            if(dir_entries_ != null)
            {
                dir_entries_.Clear();
                dir_entries_ = null;
            }
            if(resolved_ != null)
            {
                resolved_.Clear();
                resolved_ = null;
            }
        }

        /// <summary>
        /// "*.ERB"や"1000_*"のような書式との照合。
        /// 本体はWindowsで動く物なので、大文字小文字は区別しない
        /// </summary>
        public static bool MatchWildcard(string s, string pattern)
        {
            return MatchWildcard(s, 0, pattern, 0);
        }
        static bool MatchWildcard(string s, int si, string p, int pi)
        {
            while(pi < p.Length)
            {
                char pc = p[pi];
                if(pc == '*')
                {
                    if(pi + 1 == p.Length)
                        return true;
                    for(int k = si; k <= s.Length; ++k)
                        if(MatchWildcard(s, k, p, pi + 1))
                            return true;
                    return false;
                }
                if(si >= s.Length)
                    return false;
                if(pc != '?' && char.ToUpperInvariant(pc) != char.ToUpperInvariant(s[si]))
                    return false;
                si++;
                pi++;
            }
            return si == s.Length;
        }

        /// <summary>
        /// ENUMFILESが見るフォルダの中身。
        ///
        /// ERB側はこれを回しながら何百回も呼ぶ事がある。
        /// eraOCG2はキャラ1人につきCHARAフォルダ(1080ファイル)を2回数え直しており、
        /// それだけで開始時に数分かかっていた。一度数えたら覚えておく。
        ///
        /// 中身が変わったかはフォルダの更新時刻で見る。書き込む命令からは
        /// EnumCacheClearを呼んで捨てさせる
        /// </summary>
        public static string[] GetDirFiles(string dir, bool recursive)
        {
            var key = (recursive ? "R:" : "T:") + dir;
            if(enum_cache_ == null)
                enum_cache_ = new Dictionary<string, EnumEntry>();

            DateTime stamp;
            try { stamp = Directory.GetLastWriteTime(dir); }
            catch(Exception) { return null; }

            EnumEntry e;
            if(enum_cache_.TryGetValue(key, out e) && e.stamp == stamp)
                return e.files;

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*",
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch(Exception)
            { return null; }

            enum_cache_[key] = new EnumEntry { stamp = stamp, files = files };
            return files;
        }
        class EnumEntry
        {
            public DateTime stamp;
            public string[] files;
        }
        static Dictionary<string, EnumEntry> enum_cache_ = null;

        public static void EnumCacheClear()
        {
            if(enum_cache_ != null)
            {
                enum_cache_.Clear();
                enum_cache_ = null;
            }
        }

        public static string GetSuffix(string filename)
        {
            int last_slash = filename.LastIndexOf('.');
            if(last_slash != -1)
                return filename.Substring(last_slash + 1);
            return filename;
        }
        /// <summary>
        /// 获取文本长
        /// </summary>
        /// <param name="s"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public static int GetDisplayLength(string s, uEmuera.Drawing.Font font)
        {
            if(font == null)
                return GetDisplayLength(s, 16f);
            var name = font.FontFamily != null ? font.FontFamily.Name : null;
            return GetDisplayLength(s, name, font.Size);
        }

        /// <summary>
        /// フォントの実測字幅で計測する。
        /// ぉんFontのように、通常は半角の文字コードへ全角のアイコンを割り当てた
        /// フォントがあるため、文字コードだけでは幅を決められない。
        /// 計測表に無い文字(漢字・ハングルなど)は従来の半角/全角判定へ退避する
        /// </summary>
        public static int GetDisplayLength(string s, string fontname, float fontsize)
        {
            if(string.IsNullOrEmpty(s))
                return 0;
            if(!FontProvider.IsMeasured(fontname))
                return GetDisplayLength(s, fontsize);

            float xsize = 0;
            for(int i = 0; i < s.Length; ++i)
            {
                var c = s[i];
                if(c == '\t')
                    continue;   //タブは幅を持たない。IsTabZeroWidthの説明を参照
                float adv;
                if(FontProvider.TryGetAdvance(fontname, c, fontsize, out adv))
                    xsize += adv;
                else if(CheckHalfSize(c))
                    xsize += fontsize / 2;
                else
                    xsize += fontsize;
            }
            return (int)xsize;
        }

        /// <summary>
        /// 1文字分の送り幅。GetDisplayLengthと同じ判定を1文字で行う。
        /// 描画側の格子もこれで組むことで、エミュレータの座標計算と食い違わなくなる
        /// </summary>
        /// <summary>
        /// タブの表示幅は0。
        ///
        /// 本家はGDIで描いており、タブ位置を設定していないのでタブは場所を取らない。
        /// ERBには「PRINTPLAIN ┃&lt;TAB&gt;」のように引数末尾へタブが紛れている物があり、
        /// 半角1文字分として数えると枠が1桁ずれて右側のUIが次の行へ落ちる。
        /// なお文字数(STRLENS)ではShift-JISと同じく1バイトのままにする
        /// </summary>
        public static float GetCharWidth(string fontname, char c, float fontsize)
        {
            if(c == '\t')
                return 0f;
            float adv;
            if(FontProvider.TryGetAdvance(fontname, c, fontsize, out adv))
                return adv;
            return CheckHalfSize(c) ? fontsize / 2 : fontsize;
        }

        public static readonly HashSet<char> fullsize = new HashSet<char>
        {
            '´',
        };
        public static bool CheckFullSize(char c)
        {
            return fullsize.Contains(c);
        }
        public static readonly HashSet<char> halfsize = new HashSet<char>
        {
            '▀','▁','▂','▃','▄','▅',
            '▆','▇','█','▉','▊','▋',
            '▌','▍','▎','▏','▐','░',
            '▒','▓','▔','▕', '▮',
            '┮', '╮', '◮', '♮', '❮',
            '⟮', '⠮','⡮','⢮', '⣮', '║',
            '▤','▥','▦', '▧', '▨', '▩',
            '▪', '▫','~', '´', 'ﾄ', '｡', '･', '—',
        };
        public static bool CheckHalfSize(char c)
        {
            //半角形。ｱｲｳ ｢｣ ｰ ﾞ などは1文字幅。
            //計測表に無いフォントで全角として並べると、表が横へ伸びて桁が崩れる
            if(c >= 0xFF61 && c <= 0xFFDC)
                return true;
            if(c >= 0xFFE8 && c <= 0xFFEE)
                return true;
            return c < 0x127 || halfsize.Contains(c);
        }
        /// <summary>
        /// 获取文本长
        /// </summary>
        /// <param name="s"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public static int GetDisplayLength(string s, float fontsize)
        {
            float xsize = 0;
            char c = '\x0';
            for(int i = 0; i < s.Length; ++i)
            {
                c = s[i];
                if(c == '\t')
                    continue;   //タブは幅を持たない
                if(CheckHalfSize(c))
                    xsize += fontsize / 2;
                else
                    xsize += fontsize;
            }

            return (int)xsize;
        }

        public static string GetStBar(char c, uEmuera.Drawing.Font font)
        {
            return GetStBar(c, font.Size);
        }

        public static string GetStBar(char c, float fontsize)
        {
            float s = fontsize;
            if(CheckHalfSize(c))
                s /= 2;
            var w = MinorShift.Emuera.Config.DrawableWidth;
            var count = (int)System.Math.Floor(w / s);
            var build = new System.Text.StringBuilder(count);
            for(int i = 0; i < count; ++i)
                build.Append(c);
            return build.ToString();
        }

        public static int GetByteCount(string str)
        {
            if(string.IsNullOrEmpty(str))
                return 0;
            var count = 0;
            var length = str.Length;
            for(int i = 0; i < length; ++i)
            {
                if(CheckHalfSize(str[i]))
                    count += 1;
                else
                    count += 2;
            }
            return count;
        }
        /// <summary>
        /// resourcesフォルダの中身。起動中に何度も要るので一度だけ列挙する
        /// </summary>
        public static string[] GetContentEntries()
        {
            if(content_entries_ != null)
                return content_entries_;
            var dir = GetContentDir();
            if(string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return content_entries_ = new string[0];
            content_entries_ = ListFilesParallel(dir, 4);
            return content_entries_;
        }
        static string[] content_entries_ = null;

        /// <summary>
        /// フォルダの中身を手分けして数える。
        /// 一本で辿ると画像6000枚で3.5秒かかるが、中身は記憶域の待ち時間なので重なる。
        /// 見つけたフォルダを積んで各自が取っていくので、枝の大きさが偏っていても均される
        /// </summary>
        public static string[] ListFilesParallel(string root, int workers)
        {
            if(string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return new string[0];
            if(workers < 1)
                workers = 1;

            var found = new List<string>();
            var pending = new Stack<string>();
            pending.Push(root);
            var gate = new object();
            int active = 0;

            System.Threading.ThreadStart work = () =>
            {
                while(true)
                {
                    string dir;
                    lock(gate)
                    {
                        while(pending.Count == 0 && active > 0)
                            System.Threading.Monitor.Wait(gate);
                        if(pending.Count == 0)
                        {
                            System.Threading.Monitor.PulseAll(gate);
                            return;
                        }
                        dir = pending.Pop();
                        active += 1;
                    }

                    string[] files = null;
                    string[] subs = null;
                    try
                    {
                        files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        subs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
                    }
                    catch(Exception)
                    { }

                    lock(gate)
                    {
                        if(files != null)
                            found.AddRange(files);
                        if(subs != null)
                            for(int i = 0; i < subs.Length; ++i)
                                pending.Push(subs[i]);
                        active -= 1;
                        System.Threading.Monitor.PulseAll(gate);
                    }
                }
            };

            var helpers = new System.Threading.Thread[workers - 1];
            for(int i = 0; i < helpers.Length; ++i)
            {
                helpers[i] = new System.Threading.Thread(work);
                helpers[i].IsBackground = true;
                helpers[i].Start();
            }
            work();
            for(int i = 0; i < helpers.Length; ++i)
                helpers[i].Join();

            //集まる順が毎回変わるので並べ直す。csvの読み込み順が揺れないように
            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found.ToArray();
        }

        /// <summary>
        /// 拡張子で絞る。列挙済みの一覧を渡せばそこから選ぶ
        /// </summary>
        public static List<string> PickByExtension(string[] files, string extension)
        {
            var result = new List<string>();
            if(files == null)
                return result;
            for(int i = 0; i < files.Length; ++i)
            {
                if(string.Compare(Path.GetExtension(files[i]), extension, true) == 0)
                    result.Add(files[i]);
            }
            return result;
        }

        public static List<string> GetFiles(string search, string extension, SearchOption option)
        {
            //"*.???"では拡張子が4文字の.webpや.jpegが漏れる。判定は下の比較に任せる
            var files = Directory.GetFiles(search, "*", option);
            var filecount = files.Length;
            var result = new List<string>();
            for(int i=0; i<filecount; ++i)
            {
                var file = files[i];
                string ext = Path.GetExtension(file);
                if(string.Compare(ext, extension, true) == 0)
                    result.Add(file);
            }
            return result;
        }
        public static List<string> GetFiles(string search, string[] extensions, SearchOption option)
        {
            var extension_checker = new HashSet<string>();
            for(int i = 0; i < extensions.Length; ++i)
                extension_checker.Add(extensions[i].ToUpper());

            var files = Directory.GetFiles(search, "*", option);
            var filecount = files.Length;
            var result = new List<string>();
            for(int i = 0; i < filecount; ++i)
            {
                var file = files[i];
                string ext = Path.GetExtension(file).ToUpper();
                if(extension_checker.Contains(ext))
                    result.Add(file);
            }
            return result;
        }
        /// <summary>
        /// resourcesフォルダ。名前の大小は配布物次第なので実体に合わせる。
        /// ResourcePrepareはProgram.Mainより先に走るので、Program側が
        /// まだ決めていない場合は自前で求める
        /// </summary>
        static string GetContentDir()
        {
            var dir = MinorShift.Emuera.Program.ContentDir;
            if(!string.IsNullOrEmpty(dir))
                return dir;
            var exedir = MinorShift._Library.Sys.ExeDir;
            if(string.IsNullOrEmpty(exedir))
                return null;
            return ResolvePath(exedir + "resources/");
        }

        public static Dictionary<string, string> GetContentFiles()
        {
            if(content_files != null)
                return content_files;
            content_files = new Dictionary<string, string>();

            //フォルダ名の大小は配布物次第。Program側で確定した物を使う
            var contentdir = GetContentDir();
            if(string.IsNullOrEmpty(contentdir) || !Directory.Exists(contentdir))
                return content_files;

            //拡張子の大小は総当たりせず一度の走査で判定する。
            //".Png"のような混在も拾えるし、走査も一回で済む
            var extensions = new HashSet<string>(
                new string[] { ".PNG", ".BMP", ".JPG", ".JPEG", ".GIF", ".WEBP" });
            var entries = GetContentEntries();
            var bmpfilelist = new List<string>();
            for(int i = 0; i < entries.Length; ++i)
            {
                if(extensions.Contains(Path.GetExtension(entries[i]).ToUpper()))
                    bmpfilelist.Add(entries[i]);
            }

            var filecount = bmpfilelist.Count;
            for(int i=0; i<filecount; ++i)
            {
                var filename = bmpfilelist[i];
                //resourcesからの相対パスと、ファイル名単独の両方で引けるようにする。
                //CSVは「CHARA/001.png」とも「001.png」とも書かれるため
                var relative = (filename.Length > contentdir.Length
                        ? filename.Substring(contentdir.Length)
                        : Path.GetFileName(filename))
                    .Replace('\\', '/').TrimStart('/').ToUpper();
                content_files[relative] = filename;
                string name = Path.GetFileName(filename).ToUpper();
                if(!content_files.ContainsKey(name))
                    content_files[name] = filename;
            }
            return content_files;
        }
        public static string[] GetResourceCSVLines(
            string csvpath, System.Text.Encoding encoding)
        {
            string[] lines = null;
            if(resource_csv_lines_ != null &&
                resource_csv_lines_.TryGetValue(csvpath, out lines))
                return lines;
            //文字コードは中身から判別する。encodingは互換のため残す
            lines = TextFileReader.ReadAllLines(csvpath);
            return lines;
        }
        public static void ResourcePrepare()
        {
            var content_files = GetContentFiles();
            if(content_files.Count == 0)
                return;

            var contentdir = GetContentDir();
            if(string.IsNullOrEmpty(contentdir) || !Directory.Exists(contentdir))
                return;
            var csvFiles = PickByExtension(GetContentEntries(), ".CSV");
            resource_csv_lines_ = new Dictionary<string, string[]>();

            var filecount = csvFiles.Count;
            for(int index=0; index < filecount; ++index)
            {
                var filename = csvFiles[index];
                //SpriteManager.ClearResourceCSVLines(filename);
                string[] lines = SpriteManager.GetResourceCSVLines(filename);
                if(lines != null)
                {
                    resource_csv_lines_.Add(filename, lines);
                    continue;
                }

                List<string> newlines = new List<string>();
                lines = TextFileReader.ReadAllLines(filename);
                int fixcount = 0;
                for(int i = 0; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    if(line.Length == 0)
                        continue;
                    string str = line.Trim();
                    if(str.Length == 0 || str.StartsWith(";"))
                        continue;

                    string[] tokens = str.Split(',');
                    if(tokens.Length >= 6)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(tokens[2]) &&
                                !string.IsNullOrEmpty(tokens[3]) &&
                                !string.IsNullOrEmpty(tokens[4]) &&
                                !string.IsNullOrEmpty(tokens[5]))
                            {
                                var w = int.Parse(tokens[4]);
                                var h = int.Parse(tokens[5]);
                                if (w != 0 && h != 0)
                                {
                                    newlines.Add(line);
                                    continue;
                                }
                            }
                        }
                        catch (Exception e)
                        {}
                    }
                    if (tokens.Length <= 1)
                        continue;
                    //ここで補えるのは大きさだけ。補えなくても行は残す。
                    //捨ててしまうとAppContents側がスプライトの宣言自体を見失う
                    string name = tokens[1].Replace('\\', '/').ToUpper();
                    string imagepath = null;
                    if(name != "ANIME")
                        content_files.TryGetValue(name, out imagepath);
                    if(imagepath == null)
                    {
                        newlines.Add(line);
                        continue;
                    }

                    var ti = SpriteManager.GetTextureInfo(name, imagepath);
                    if(ti == null)
                    {
                        newlines.Add(line);
                        continue;
                    }
                    line = string.Format("{0},{1},0,0,{2},{3}",
                        tokens[0], tokens[1], ti.width, ti.height);
                    newlines.Add(line);
                    fixcount += 1;
                }
                lines = newlines.ToArray();
                resource_csv_lines_.Add(filename, lines);
                if(fixcount > 0)
                    SpriteManager.SetResourceCSVLine(filename, lines);
            }
        }
        public static void ResourcePrepareSimple()
        {
            var content_files = GetContentFiles();
            if(content_files.Count == 0)
                return;

            var contentdir = GetContentDir();
            if(string.IsNullOrEmpty(contentdir) || !Directory.Exists(contentdir))
                return;
            var csvFiles = PickByExtension(GetContentEntries(), ".CSV");
            resource_csv_lines_ = new Dictionary<string, string[]>();

            var filecount = csvFiles.Count;
            for(int index = 0; index < filecount; ++index)
            {
                var filename = csvFiles[index];
                //SpriteManager.ClearResourceCSVLines(filename);
                string[] lines = SpriteManager.GetResourceCSVLines(filename);
                if(lines != null)
                    resource_csv_lines_.Add(filename, lines);
            }
        }
        public static void ResourceClear()
        {
            ResolvePathClear();
            content_entries_ = null;
            if(content_files != null)
            {
                content_files.Clear();
                content_files = null;
            }
            if(resource_csv_lines_ != null)
            {
                resource_csv_lines_.Clear();
                resource_csv_lines_ = null;
            }
        }
        static Dictionary<string, string> content_files = null;
        static Dictionary<string, string[]> resource_csv_lines_ = null;
    }
}