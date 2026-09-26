using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FontUtils
{
    static readonly Dictionary<string, string> name_path_map = new Dictionary<string, string>
    {
        {"ＭＳ ゴシック", "MS Gothic"},
        {"MS Gothic", "MS Gothic"},
        {"ＭＳ Ｐゴシック", "MS PGothic"},
        {"MS PGothic", "MS PGothic"},
        {"돋움", "Dotum"},
        {"Dotum", "Dotum"},
        {"돋움체", "Dotumche"},
        {"Dotumche", "Dotumche"},
        {"굴림", "Gulim"},
        {"Gulim", "Gulim"},
        {"굴림체", "GulimChe"},
        {"GulimChe", "GulimChe"},
        {"xonFont", "xonFont"},
        {"xonFontH", "xonFontH"},
        {"ぉんFont", "xonFont"},
        {"ぉんFont半角", "xonFontH"}
    };

    /// <summary>
    /// アプリに埋め込んだフォントが実在するか。GetFontと違い既定フォントへ逃げない。
    /// Resources.Loadを使うのでメインスレッドから呼ぶこと
    /// </summary>
    public static bool HasEmbeddedFont(string name)
    {
        if(string.IsNullOrEmpty(name))
            return false;
        string path;
        if(!name_path_map.TryGetValue(name, out path))
            path = name;
        return Resources.Load<Font>("Fonts/" + path) != null;
    }

    public static void SetDefaultFont(string fontname)
    {
        default_font = GetFont(fontname);
        if(default_font == null)
        {
            default_fontname = "ＭＳ ゴシック";
            default_font = GetFont(default_fontname);
        }
        else
        {
            default_fontname = fontname;
        }
    }

    public static Font GetFont(string name)
    {
        if(string.IsNullOrEmpty(name))
            return default_font;

        if(name == last_name)
            return last_font;
        last_name = name;

        string path = null;
        if (!name_path_map.TryGetValue(name, out path))
            path = name; // Fallback to the requested name so custom fonts can be loaded from Resources

        return LoadFont(path);
    }
    static Font LoadFont(string path)
    {
        if(string.IsNullOrEmpty(path))
            last_font = default_font;
        else if(!font_map.TryGetValue(path, out last_font))
        {
            last_font = Resources.Load<Font>("Fonts/" + path);
            if(last_font == null)
                last_font = default_font;
            font_map[path] = last_font;
        }
        return last_font;
    }

    public static string last_name = null;
    public static Font last_font = null;

    public static string default_fontname { get; private set; }
    public static Font default_font { get; private set; }

    static Dictionary<string, Font> font_map = new Dictionary<string, Font>();
}
