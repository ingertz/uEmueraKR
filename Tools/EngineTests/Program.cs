using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

// エンジンのうちUnityに頼らない部分(セーブの読み書き・ハッシュ・乱数・版の比較)を確かめる。
// run.ps1 が Assets/Scripts をRoslynでコンパイルしたDLLを、ここから反射で呼ぶ。
// Unityの描画やテクスチャに触る物はここでは動かせない
//
//   dotnet run -- <コンパイルしたDLL> <Unityの Managed/UnityEngine フォルダ>
class Program
{
    static Assembly asm;
    static int fails = 0;
    static int passes = 0;

    static Type T(string n) => asm.GetType(n, true);

    static void Check(bool ok, string what)
    {
        if (ok) { passes++; return; }
        fails++;
        Console.WriteLine("FAIL " + what);
    }

    static void SetConfig(string prop, object v)
    {
        var p = T("MinorShift.Emuera.Config").GetProperty(prop, BindingFlags.Static | BindingFlags.Public);
        p.GetSetMethod(true).Invoke(null, new[] { v });
    }

    static object Call(object o, string m, params object[] a)
    {
        var t = o as Type ?? o.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | (o is Type ? BindingFlags.Static : BindingFlags.Instance);
        var mi = t.GetMethods(flags).First(x => x.Name == m && x.GetParameters().Length == a.Length
            && x.GetParameters().Select((pp, i) => a[i] == null || pp.ParameterType.IsAssignableFrom(a[i].GetType())).All(b => b));
        return mi.Invoke(o is Type ? null : o, a);
    }

    static int Main(string[] args)
    {
        var unityDir = args[1];
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name + ".dll";
            foreach (var dir in new[] { unityDir, Path.GetDirectoryName(Path.GetFullPath(args[0])) })
            {
                var p = Path.Combine(dir, name);
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            return null;
        };
        asm = Assembly.LoadFrom(args[0]);
        var dir = Path.Combine(Path.GetTempPath(), "uemuera_enginetests");
        Directory.CreateDirectory(dir);

        Run("binary save", () => BinarySave(dir));
        Run("text save", () => TextSave(dir));
        Run("xxhash", XxHash);
        Run("random", NewRandom);
        Run("version compare", VersionCompare);

        Console.WriteLine(fails == 0 ? $"ALL PASSED ({passes} checks)" : $"{fails} FAILED, {passes} passed");
        return fails == 0 ? 0 : 1;
    }

    static void Run(string name, Action test)
    {
        int before = fails;
        try { test(); }
        catch (Exception e) { fails++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e)); }
        Console.WriteLine((fails == before ? "ok   " : "NG   ") + name);
    }

    // バイナリ形式: 圧縮の有無、MAP・XML・DataTableの2つ目の区画、旧形式、テキスト形式の見分け
    static void BinarySave(string dir)
    {
        var writerT = T("MinorShift.Emuera.Sub.EraBinaryDataWriter");
        var readerT = T("MinorShift.Emuera.Sub.EraBinaryDataReader");
        var fileTypeT = T("MinorShift.Emuera.Sub.EraSaveFileType");
        foreach (var zip in new[] { false, true })
        {
            SetConfig("SystemSaveInBinary", true);
            SetConfig("ZipSaveData", zip);
            var path = Path.Combine(dir, zip ? "z.sav" : "n.sav");
            var map = new Dictionary<string, string> { { "k1", "値1" }, { "k2", "" } };
            var doc = new XmlDocument(); doc.LoadXml("<root a=\"1\"><c>한글</c></root>");
            var dt = new DataTable("tbl"); dt.Columns.Add("id", typeof(long)); dt.Columns.Add("name", typeof(string));
            dt.Rows.Add(1L, "あ"); dt.Rows.Add(2L, "b");

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var w = Activator.CreateInstance(writerT, fs);
                Call(w, "WriteHeader");
                Call(w, "WriteFileType", Enum.ToObject(fileTypeT, 1));
                Call(w, "WriteInt64", 12345L);
                Call(w, "WriteInt64", 7L);
                Call(w, "WriteString", "mes");
                Call(w, "WriteWithKey", "GLOBAL", new long[] { 0, 5, 0, 0, 300000, -1 });
                Call(w, "WriteWithKey", "GLOBALS", new string[] { "", "あいう", null });
                Call(w, "WriteEOF");
                Call(w, "WriteWithKey", "M", map);
                Call(w, "WriteWithKey", "X", doc);
                Call(w, "WriteWithKey", "D", dt);
                Call(w, "WriteEOF");
                Call(w, "Close");
            }
            var raw = File.ReadAllBytes(path);
            string z = " zip=" + zip;
            Check(BitConverter.ToUInt64(raw, 0) == (zip ? 0x0A50495A41524589UL : 0x0A1A0A0D41524589UL), "header" + z);
            if (zip)
                Check(raw[16] == 0x1f && raw[17] == 0x8b, "gzip after header" + z);

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var r = Call(readerT, "CreateReader", fs);
                Check(r != null, "CreateReader" + z);
                Check(Call(r, "ReadFileType").ToString() == "Global", "file type" + z);
                Check((long)Call(r, "ReadInt64") == 12345L && (long)Call(r, "ReadInt64") == 7L, "codes" + z);
                Check((string)Call(r, "ReadString") == "mes", "saveMes" + z);
                dynamic kv = Call(r, "ReadVariableCode");
                Check(kv.Key == "GLOBAL" && kv.Value.ToString() == "IntArray", "GLOBAL code" + z);
                var ga = new long[6]; Call(r, "ReadIntArray", ga, true);
                Check(ga.SequenceEqual(new long[] { 0, 5, 0, 0, 300000, -1 }), "GLOBAL values" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Key == "GLOBALS", "GLOBALS code" + z);
                var gs = new string[3]; Call(r, "ReadStrArray", gs, true);
                Check(gs[1] == "あいう", "GLOBALS values" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Value.ToString() == "EOF", "first EOF" + z);
                Check(!(bool)Call(r, "EOF"), "second section exists" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Key == null && kv.Value.ToString() == "Map" && (string)Call(r, "ReadString") == "M", "map code/key" + z);
                var m2 = (Dictionary<string, string>)Call(r, "ReadMap");
                Check(m2.Count == 2 && m2["k1"] == "値1" && m2["k2"] == "", "map values" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Value.ToString() == "Xml" && (string)Call(r, "ReadString") == "X", "xml code/key" + z);
                Check(((XmlDocument)Call(r, "ReadXml")).OuterXml == doc.OuterXml, "xml value" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Value.ToString() == "DT" && (string)Call(r, "ReadString") == "D", "dt code/key" + z);
                var t2 = (DataTable)Call(r, "ReadDataTable");
                Check(t2.TableName == "tbl" && t2.Rows.Count == 2 && (string)t2.Rows[0]["name"] == "あ" && (long)t2.Rows[1]["id"] == 2L, "dt values" + z);
                kv = Call(r, "ReadVariableCode");
                Check(kv.Value.ToString() == "EOF" && (bool)Call(r, "EOF"), "end of file" + z);
                Call(r, "Close");
            }
            Check(CanRewrite(path), "file closed after reading" + z);
        }

        //2つ目の区画が無い旧形式
        SetConfig("ZipSaveData", false);
        var oldPath = Path.Combine(dir, "old.sav");
        using (var fs = new FileStream(oldPath, FileMode.Create, FileAccess.Write))
        {
            var w = Activator.CreateInstance(writerT, fs);
            Call(w, "WriteHeader");
            Call(w, "WriteFileType", Enum.ToObject(fileTypeT, 1));
            Call(w, "WriteInt64", 1L); Call(w, "WriteInt64", 1L); Call(w, "WriteString", "");
            Call(w, "WriteWithKey", "GLOBAL", new long[] { 9 });
            Call(w, "WriteEOF");
            Call(w, "Close");
        }
        using (var fs = new FileStream(oldPath, FileMode.Open, FileAccess.Read))
        {
            var r = Call(readerT, "CreateReader", fs);
            Call(r, "ReadFileType"); Call(r, "ReadInt64"); Call(r, "ReadInt64"); Call(r, "ReadString");
            Call(r, "ReadVariableCode"); Call(r, "ReadIntArray", new long[1], true);
            dynamic kv = Call(r, "ReadVariableCode");
            Check(kv.Value.ToString() == "EOF" && (bool)Call(r, "EOF"), "old format has no second section");
            Call(r, "Close");
        }

        //テキスト形式はCreateReaderがnullを返し、読み直せるようにfsを開いたままにする
        var txt = Path.Combine(dir, "t.sav");
        File.WriteAllText(txt, "12345\r\n7\r\n0123456789abcdef\r\n");
        using (var fs = new FileStream(txt, FileMode.Open, FileAccess.Read))
        {
            Check(Call(readerT, "CreateReader", fs) == null, "text save is not binary");
            Check(fs.CanRead, "stream left open for the text reader");
        }
    }

    // テキスト形式: 書いて読み、読んだ後に同じファイルへ書けること(0.3.1dで壊れていた所)
    static void TextSave(string dir)
    {
        var path = Path.Combine(dir, "text.sav");
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            var w = Activator.CreateInstance(T("MinorShift.Emuera.Sub.EraDataWriter"), fs);
            Call(w, "Write", 4242L);
            Call(w, "Write", "セーブ名");
            Call(w, "Write", new long[] { 1, 0, 3 });
            Call(w, "Write", (object)new string[] { "가", "", "c" });
            Call(w, "EmuStart");
            Call(w, "Close");
        }
        {
            //エンジンと同じく、ファイルは読み手のCloseに任せて自分では閉じない
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var r = Activator.CreateInstance(T("MinorShift.Emuera.Sub.EraDataReader"), fs);
            Check((long)Call(r, "ReadInt64") == 4242L, "text int");
            Check((string)Call(r, "ReadString") == "セーブ名", "text string");
            var la = new long[3]; Call(r, "ReadInt64Array", la);
            Check(la.SequenceEqual(new long[] { 1, 0, 3 }), "text int array");
            var sa = new string[3]; Call(r, "ReadStringArray", (object)sa);
            Check(sa[0] == "가" && sa[2] == "c", "text string array");
            Check((bool)Call(r, "SeekEmuStart"), "text emu start");
            Call(r, "Close");
        }
        Check(CanRewrite(path), "text save closed after reading");

        //PC版のShift-JISで書かれたセーブも読める
        var sjis = Path.Combine(dir, "sjis.sav");
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        File.WriteAllText(sjis, "1\r\n日本語のセーブ\r\n", System.Text.Encoding.GetEncoding(932));
        using (var fs = new FileStream(sjis, FileMode.Open, FileAccess.Read))
        {
            var r = Activator.CreateInstance(T("MinorShift.Emuera.Sub.EraDataReader"), fs);
            Call(r, "ReadInt64");
            Check((string)Call(r, "ReadString") == "日本語のセーブ", "Shift-JIS text save");
            Call(r, "Close");
        }
    }

    static bool CanRewrite(string path)
    {
        try { using (new FileStream(path, FileMode.Create, FileAccess.Write)) { } return true; }
        catch { return false; }
    }

    // HASH_XXH32 / HASH_XXH3。長さ0は公式の値、他は System.IO.Hashing と突き合わせ済みの実装から取った値
    static void XxHash()
    {
        var t = T("MinorShift.Emuera.Sub.XxHash");
        var cases = new (int len, uint h32, ulong h3)[]
        {
            (0, 0x02CC5D05u, 0x2D06800538D394C2ul),
            (1, 0x002E0D32u, 0x4C5CCA45D0F4811Ful),
            (3, 0xACA17380u, 0x15F7093B173D005Cul),
            (4, 0x073FAA82u, 0xDCA012F95811B6B9ul),
            (8, 0xB9893C6Cu, 0xDEC6A9A43575982Eul),
            (9, 0xEF682A43u, 0xCBE393399F17FFBDul),
            (16, 0x3F6C9665u, 0x7E484C18D74895D0ul),
            (17, 0xE048ECDBu, 0x208BDE5EE2BED407ul),
            (128, 0x0BF98264u, 0xF92B70EAA21A6288ul),
            (129, 0xF2D10021u, 0xF8F76713F2BB60FAul),
            (240, 0x6BE3CA8Bu, 0xCCC7375172C41F03ul),
            (241, 0x8C1402A0u, 0x0B3B630948CE4A00ul),
            (1024, 0xEAD2CE3Cu, 0x23BC880EBF0D29C6ul),
            (5000, 0x0310232Du, 0x559FFF92C2B7F8EEul),
        };
        foreach (var c in cases)
        {
            var b = new byte[c.len];
            for (int i = 0; i < c.len; i++) b[i] = (byte)(i * 31 + 7);
            Check((uint)Call(t, "Hash32", b) == c.h32, "XXH32 len " + c.len);
            Check((ulong)Call(t, "Hash3", b) == c.h3, "XXH3 len " + c.len);
        }
    }

    // setting.json の UseNewRandom で使う乱数: 0 以上 max 未満で偏らない
    static void NewRandom()
    {
        var m = T("MinorShift.Emuera.GameData.Variable.VariableEvaluator").GetMethod("nextInt64", BindingFlags.NonPublic | BindingFlags.Static);
        var r = new Random(1);
        Check((long)m.Invoke(null, new object[] { r, 0L }) == 0 && (long)m.Invoke(null, new object[] { r, 1L }) == 0, "max <= 1 gives 0");
        foreach (long max in new long[] { 2, 3, 7, 100, 1L << 40, long.MaxValue })
        {
            bool inRange = true;
            for (int i = 0; i < 5000; i++)
            {
                long v = (long)m.Invoke(null, new object[] { r, max });
                inRange &= v >= 0 && v < max;
            }
            Check(inRange, "in range for max " + max);
        }
        var counts = new int[7];
        for (int i = 0; i < 70000; i++)
            counts[(long)m.Invoke(null, new object[] { r, 7L })]++;
        Check(counts.All(c => c > 9000 && c < 11000), "even for max 7: " + string.Join(",", counts));
    }

    // 업데이트 확인 버튼의 버전 비교
    static void VersionCompare()
    {
        var m = T("OptionWindow").GetMethod("CompareVersion", BindingFlags.NonPublic | BindingFlags.Static);
        Func<string, string, int> cmp = (a, b) => Math.Sign((int)m.Invoke(null, new object[] { a, b }));
        Check(cmp("0.3.1e", "0.3.1d") > 0, "0.3.1e > 0.3.1d");
        Check(cmp("0.3.1", "0.3.1d") < 0, "0.3.1 < 0.3.1d");
        Check(cmp("0.3.2", "0.3.1e") > 0, "0.3.2 > 0.3.1e");
        Check(cmp("0.3.10", "0.3.9") > 0, "0.3.10 > 0.3.9");
        Check(cmp("v0.3.1d", "0.3.1d") == 0, "v prefix ignored");
    }
}
