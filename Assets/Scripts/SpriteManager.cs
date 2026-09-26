using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera.Content;
using uEmuera.Drawing;
using WebP;

internal static class SpriteManager
{
    static float kPastTime = 300.0f;

    internal class SpriteInfo : IDisposable
    {
        internal SpriteInfo(TextureInfo p, Sprite s)
        {
            parent = p;
            sprite = s;
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(sprite);
            sprite = null;
        }
        internal Sprite sprite;
        internal TextureInfo parent;
    }
    internal class TextureInfo : IDisposable
    {
        internal TextureInfo(string b, Texture2D tex)
        {
            imagename = b;
            texture = tex;
            pasttime = Time.unscaledTime + kPastTime;
        }
        internal SpriteInfo GetSprite(ASprite src)
        {
            //アニメスプライトは今のフレームが指す範囲を切り出す。
            //フレームごとに元のGも切り出し位置も変わるので名前だけでは足りない
            var key = src.Name;
            var rect = src.Rectangle;
            var anime = src as SpriteAnime;
            if(anime != null)
            {
                key = src.Name + "#" + anime.CurrentFrameIndex;
                rect = anime.CurrentFrameRectangle;
            }
            SpriteInfo sprite = null;
            if(!sprites.TryGetValue(key, out sprite))
            {
                sprite = new SpriteInfo(this,
                    Sprite.Create(texture,
                        GenericUtils.ToUnityRect(rect, texture.width, texture.height),
                        Vector2.zero)
                    );
                sprites[key] = sprite;
            }
            if(sprite != null)
                refcount += 1;
            return sprite;
        }
        internal void Release()
        {
            refcount -= 1;
            pasttime = Time.unscaledTime + kPastTime;
        }
        public void Dispose()
        {
            var iter = sprites.Values.GetEnumerator();
            while(iter.MoveNext())
            {
                iter.Current.Dispose();
            }
            sprites.Clear();
            sprites = null;

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }
        internal string imagename = null;
        /// <summary>
        /// GCREATEなどERB側が描いたテクスチャ。ファイルが無いので捨てると二度と戻せない
        /// </summary>
        internal bool isDynamic = false;
        internal int refcount = 0;
        internal float pasttime = 0;
        internal float width { get { return texture.width; } }
        internal float height { get { return texture.height; } }
        internal Texture2D texture = null;
        Dictionary<string, SpriteInfo> sprites = new Dictionary<string, SpriteInfo>();
    }
    class CallbackInfo
    {
        public CallbackInfo(ASprite src, object obj, 
                            Action<object, SpriteInfo> callback)
        {
            this.src = src;
            this.obj = obj;
            this.callback = callback;
        }
        public void DoCallback(SpriteInfo info)
        {
            callback(obj, info);
        }
        public ASprite src;
        object obj;
        Action<object, SpriteInfo> callback;
    }

    static int mainThreadId = -1;
    static System.Collections.Concurrent.ConcurrentQueue<Action> mainThreadActions = new System.Collections.Concurrent.ConcurrentQueue<Action>();
    public static void RunOnMainThread(Action action)
    {
        if (mainThreadId != -1 && System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId)
        {
            try { action(); } catch (Exception e) { Debug.LogError(e); }
        }
        else
        {
            mainThreadActions.Enqueue(action);
        }
    }
    /// <summary>
    /// SetPixelで書き換えたテクスチャ。次のフレームの頭でまとめてApplyする
    /// </summary>
    static readonly HashSet<Texture2D> pending_apply_ = new HashSet<Texture2D>();
    public static void RequestApply(Texture2D tex)
    {
        if (tex != null)
            pending_apply_.Add(tex);
    }
    static void FlushPendingApply()
    {
        if (pending_apply_.Count == 0)
            return;
        //表示中の画像が書き換わった
        RenderThrottle.Wake();
        foreach (var tex in pending_apply_)
        {
            if (tex != null)
                tex.Apply(false, false);
        }
        pending_apply_.Clear();
    }

    static IEnumerator UpdateMainThread()
    {
        if (mainThreadId == -1) mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var swTotal = new System.Diagnostics.Stopwatch();
        var swWait = new System.Diagnostics.Stopwatch();
        while(true)
        {
            //アニメスプライトが参照する時刻。ここで進めないと止まったままになる
            MinorShift._Library.WinmmTimer.FrameStart();
            FlushPendingApply();
            bool processedAny = false;
            swTotal.Restart();
            while (mainThreadActions.TryDequeue(out Action action))
            {
                if (!processedAny)
                    RenderThrottle.Wake();//G*の描画などテクスチャを触る処理が来た
                processedAny = true;
                try { action(); } catch (Exception e) { Debug.LogError(e); }
                if (swTotal.ElapsedMilliseconds >= 30) break;
            }
            if (processedAny && swTotal.ElapsedMilliseconds < 30)
            {
                swWait.Restart();
                while (swWait.ElapsedMilliseconds < 15 && swTotal.ElapsedMilliseconds < 30)
                {
                    if (mainThreadActions.TryDequeue(out Action action))
                    {
                        try { action(); } catch (Exception e) { Debug.LogError(e); }
                        swWait.Restart();
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
            }
            yield return null;
        }
    }

    public static void Init()
    {
        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#if UNITY_EDITOR
        kPastTime = 300.0f;
#else
        var memorysize = SystemInfo.systemMemorySize;
        if(memorysize <= 4096)
            kPastTime = 300.0f;
        else if(memorysize <= 8192)
            kPastTime = 600.0f;
        else
            kPastTime = 1200.0f;
#endif
        GenericUtils.StartCoroutine(Update());
        GenericUtils.StartCoroutine(UpdateRenderOP());
        GenericUtils.StartCoroutine(UpdateMainThread());
    }
    public static void GetSprite(ASprite src, 
                                object obj, Action<object, SpriteInfo> callback)
    {
        if(src == null)
        {
            if(callback != null) callback(obj, null);
            return;
        }
        if(src.Bitmap == null)
        {
            if(callback != null) callback(obj, null);
            return;
        }

        var basename = src.Bitmap.name;
        TextureInfo ti = null;
        texture_dict.TryGetValue(basename, out ti);
        if(ti == null)
        {
            var item = new CallbackInfo(src, obj, callback);
            List<CallbackInfo> list = null;
            if(loading_set.TryGetValue(basename, out list))
                list.Add(item);
            else
            {
                list = new List<CallbackInfo> { item };
                loading_set.Add(basename, list);
                GenericUtils.StartCoroutine(Loading(src.Bitmap));
            }
        }
        else
            callback(obj, GetSpriteInfo(ti, src));
    }

    public static TextureInfo GetTextureInfo(string name, string filename)
    {
        TextureInfo ti = null;
        if(texture_dict.TryGetValue(name, out ti))
            return ti;
        if(string.IsNullOrEmpty(filename))
            return null;

        FileInfo fi = new FileInfo(filename);
        if(!fi.Exists)
            return null;

        //読み終わったら必ず閉じる。閉じ忘れるとテクスチャを捨てて読み直す度に
        //ハンドルが積み上がり、Windowsではファイルも掴んだままになる
        byte[] content = new byte[fi.Length];
        using(FileStream fs = fi.OpenRead())
        {
            int read = 0;
            while(read < content.Length)
            {
                int n = fs.Read(content, read, content.Length - read);
                if(n <= 0)
                    break;
                read += n;
            }
        }

        TextureFormat format = TextureFormat.RGBA32;

        var extname = uEmuera.Utils.GetSuffix(filename).ToLower();
        if (extname == "png")
            format = TextureFormat.RGBA32;

        if (extname == "webp")
        {
            var tex = Texture2DExt.CreateTexture2DFromWebP(content, false, false,
                out Error err);
            if (err != Error.Success)
            {
                Debug.LogWarning($"{filename} {err.ToString()}");
                return null;
            }
            ti = new TextureInfo(name, tex);
            texture_dict[name] = ti;
        }
        else
        {
            var tex = DecodeTexture(content, format);
            if (tex != null)
            {
                ti = new TextureInfo(name, tex);
                texture_dict[name] = ti;
            }
        }
        return ti;
    }

    /// <summary>
    /// PNG/JPGはUnityのLoadImageで、BMP/GIFは自前の復号器で読む。
    /// LoadImageはBMP/GIFを読めず、失敗すると画像が出なかった。
    /// 判定はファイル先頭のバイトで行い、拡張子は見ない
    /// </summary>
    static Texture2D DecodeTexture(byte[] content, TextureFormat format)
    {
        if (uEmuera.LegacyImageDecoder.CanDecode(content))
        {
            int w, h;
            byte[] rgba;
            if (!uEmuera.LegacyImageDecoder.TryDecode(content, out w, out h, out rgba))
                return null;
            var btex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            btex.LoadRawTextureData(rgba);
            btex.Apply(false, false);
            return btex;
        }
        var tex = new Texture2D(4, 4, format, false);
        if (tex.LoadImage(content))
            return tex;
        UnityEngine.Object.Destroy(tex);
        return null;
    }

    public static void RegisterDynamicTexture(string name, Texture2D tex)
    {
        if (texture_dict.ContainsKey(name)) return;
        var ti = new TextureInfo(name, tex);
        ti.isDynamic = true;
        texture_dict[name] = ti;
    }

    /// <summary>
    /// 指定名の書き込み用テクスチャを得る。同じ大きさの物が既にあればそれを使い回す。
    /// 既存のTexture2Dは破棄しない(表示中のSpriteが参照している可能性があるため)
    /// </summary>
    public static Texture2D GetOrCreateDynamicTexture(string name, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return null;
        TextureInfo ti = null;
        if (texture_dict.TryGetValue(name, out ti) && ti != null && ti.texture != null &&
            ti.texture.width == width && ti.texture.height == height)
            return ti.texture;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture_dict[name] = new TextureInfo(name, tex) { isDynamic = true };
        return tex;
    }

    public static TextureInfoOtherThread GetTextureInfoOtherThread(
        string name, string path, Action<TextureInfo> callback)
    {
        var ti = new TextureInfoOtherThread
        {
            name = name,
            path = path,
            callback = callback,
            mutex = null,
        };
        texture_other_threads.Add(ti);
        return ti;
    }
    public class TextureInfoOtherThread
    {
        public string name;
        public string path;
        public Action<TextureInfo> callback;
        public System.Threading.Mutex mutex;
    }
    static List<TextureInfoOtherThread> texture_other_threads = new List<TextureInfoOtherThread>();

    public static RenderTextureOtherThread GetRenderTextureOtherThread(int x, int y, Action<RenderTexture> callback)
    {
        var ti = new RenderTextureOtherThread
        {
            x = x,
            y = y,
            callback = callback,
            mutex = null,
        };
        render_texture_other_threads.Add(ti);
        return ti;
    }
    public class RenderTextureOtherThread
    {
        public int x;
        public int y;
        public Action<RenderTexture> callback;
        public System.Threading.Mutex mutex;
    }
    static List<RenderTextureOtherThread> render_texture_other_threads = new List<RenderTextureOtherThread>();

    ///public static RenderTextureDoSomething RenderTexture
    ///

    public class RenderTextureDoSomething
    {
        public enum Code
        {
            kClear,
            kDrawRectangle,
            kFillRectangle,
            kDrawCImg,
            kDrawG,
            kDrawGWithMask,
            kSetColor,
            kGetColor,
        }
        //Todo: 实现对于方法
    }

    static IEnumerator Loading(Bitmap baseimage)
    {
        TextureInfo ti = null;
        FileInfo fi = new FileInfo(baseimage.path);
        if(fi.Exists)
        {
            byte[] content = new byte[fi.Length];
            //yieldを跨ぐのでusingでは囲えない。読み終えたら明示的に閉じる
            FileStream fs = fi.OpenRead();
            var async = fs.BeginRead(content, 0, content.Length, null, null);
            while(!async.IsCompleted)
                yield return null;
            fs.EndRead(async);
            fs.Close();

            TextureFormat format = TextureFormat.RGBA32;

            var extname = uEmuera.Utils.GetSuffix(baseimage.path).ToLower();
            if (extname == "png")
                format = TextureFormat.RGBA32;

            if (extname == "webp")
            {
                var tex = Texture2DExt.CreateTexture2DFromWebP(content, false, false,
                out Error err);
                if (err != Error.Success)
                {
                    Debug.LogWarning($"{baseimage.path} {err.ToString()}");
                }
                else
                {
                    ti = new TextureInfo(baseimage.name, tex);
                    texture_dict[baseimage.name] = ti;

                    baseimage.size.Width = tex.width;
                    baseimage.size.Height = tex.height;
                }
            }
            else
            {
                var tex = DecodeTexture(content, format);
                if (tex != null)
                {
                    ti = new TextureInfo(baseimage.name, tex);
                    texture_dict[baseimage.name] = ti;

                    baseimage.size.Width = tex.width;
                    baseimage.size.Height = tex.height;
                }
            }
        }
        List<CallbackInfo> list = null;
        if(loading_set.TryGetValue(baseimage.name, out list))
        {
            var count = list.Count;
            CallbackInfo item = null;
            for(int i=0; i<count; ++i)
            {
                item = list[i];
                item.DoCallback(GetSpriteInfo(ti, item.src));
            }
            list.Clear();
            loading_set.Remove(baseimage.name);
        }
    }
    static SpriteInfo GetSpriteInfo(TextureInfo textinfo, ASprite src)
    {
        if (textinfo == null) return null;
        return textinfo.GetSprite(src);
    }
    internal static void GivebackSpriteInfo(SpriteInfo info)
    {
        if(info == null)
            return;
        info.parent.Release();
    }
    static IEnumerator Update()
    {
        while(true)
        {
            do
            {
                yield return new WaitForSeconds(1.0f);
            } while(texture_dict.Count == 0);

            var now = Time.unscaledTime;
            TextureInfo tinfo = null;
            TextureInfo ti = null;
            var iter = texture_dict.Values.GetEnumerator();
            while(iter.MoveNext())
            {
                ti = iter.Current;
                //ERB側が描いたテクスチャは読み直せないので捨てない。
                //捨てるとGDISPOSEもしていない画像が突然消え、以後空のまま戻らない
                if(ti.isDynamic)
                    continue;
                if(ti.refcount == 0 && now > ti.pasttime)
                {
                    tinfo = ti;
                    break;
                }
            }
            if(tinfo != null)
            {
                tinfo.Dispose();
                texture_dict.Remove(tinfo.imagename);
                tinfo = null;

                // GC.Collect(); - Removed synchronous GC freeze during gameplay
            }
        }
    }
    static IEnumerator UpdateRenderOP()
    {
        while(true)
        {
            do
            {
                yield return null;
            } while(texture_other_threads.Count == 0
                && render_texture_other_threads.Count == 0);

            TextureInfo ti = null;
            if(texture_other_threads.Count > 0)
            {
                TextureInfoOtherThread tiot = null;
                var tiotiter = texture_other_threads.GetEnumerator();
                while(tiotiter.MoveNext())
                {
                    tiot = tiotiter.Current;
                    tiot.mutex = new System.Threading.Mutex(true);
                    //tiot.mutex.WaitOne();
                    ti = GetTextureInfo(tiot.name, tiot.path);
                    tiot.callback(ti);
                    tiot.mutex.ReleaseMutex();
                }
                texture_other_threads.Clear();
            }
            if(render_texture_other_threads.Count > 0)
            {
                RenderTextureOtherThread rtot = null;
                var rtotiter = render_texture_other_threads.GetEnumerator();
                while(rtotiter.MoveNext())
                {
                    rtot = rtotiter.Current;
                    rtot.mutex = new System.Threading.Mutex(true);
                    //tiot.mutex.WaitOne();
                    var rt = new RenderTexture(rtot.x, rtot.y, 24, RenderTextureFormat.ARGB32);
                    rtot.callback(rt);
                    rtot.mutex.ReleaseMutex();
                }
                render_texture_other_threads.Clear();
            }
        }
    }
    internal static void ForceClear()
    {
        var iter = texture_dict.Values.GetEnumerator();
        while(iter.MoveNext())
        {
            iter.Current.Dispose();
        }
        texture_dict.Clear();
        GC.Collect();
    }
    /// <summary>
    /// 整形結果の作り方を変えたら上げる。上げないと、前の版が残した
    /// 壊れた内容を読み続けてしまう。CSVの更新時刻だけでは気付けない
    /// </summary>
    const string kResourceCSVCacheVersion = "_v2";

    internal static void SetResourceCSVLine(string filename, string[] lines)
    {
        var key = filename + kResourceCSVCacheVersion;
        var cache = string.Join("\n", lines);
        UnityEngine.PlayerPrefs.SetInt(key + "_fixed", 1);
        UnityEngine.PlayerPrefs.SetString(key + "_time",
                        File.GetLastWriteTime(filename).ToString());
        UnityEngine.PlayerPrefs.SetString(key, cache);
    }
    internal static string[] GetResourceCSVLines(string filename)
    {
        var key = filename + kResourceCSVCacheVersion;
        if(PlayerPrefs.GetInt(key + "_fixed", 0) == 0)
            return null;
        var oldwritetime = PlayerPrefs.GetString(key + "_time", null);
        if(string.IsNullOrEmpty(oldwritetime))
            return null;
        var writetime = File.GetLastWriteTime(filename).ToString();
        if(oldwritetime != writetime)
            return null;
        var cache = UnityEngine.PlayerPrefs.GetString(key, null);
        if(string.IsNullOrEmpty(cache))
            return null;
        return cache.Split('\n');
    }
    internal static void ClearResourceCSVLines(string filename)
    {
        var key = filename + kResourceCSVCacheVersion;
        UnityEngine.PlayerPrefs.SetInt(key + "_fixed", 0);
        UnityEngine.PlayerPrefs.SetString(key + "_time", null);
        UnityEngine.PlayerPrefs.SetString(key, null);
    }
    static Dictionary<string, List<CallbackInfo>> loading_set =
        new Dictionary<string, List<CallbackInfo>>();
    static Dictionary<string, TextureInfo> texture_dict =
        new Dictionary<string, TextureInfo>();
}
