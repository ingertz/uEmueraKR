using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ゲームを実際に動かして、画面の文字をファイルへ書き出す。
/// Unityを手で開かずに「読み込めるか」「どこで止まるか」を確かめる。
///   Unity -batchmode -projectPath . -executeMethod PlayTest.Run
///         -ptGame ゲームフォルダ -ptOut 出力フォルダ [-ptInputs 入力ファイル] [-ptTimeout 秒] [-ptLive]
///
/// 入力ファイルは1行が1回の入力で、ゲームが入力待ちになる度に1行ずつ送る。
/// 空行はEnter(WAITを進める)。「!log 名前」はその時点の画面を 名前.log に書き出す。
/// 「!mem 名前」はTexture2Dの数と大きさをログへ書き、「!wait 秒」は次の入力までその秒数待つ。
/// 入力を使い切るか、エラーで止まるか、時間切れになったら final.log と result.txt を書いて終了する。
/// -ptLive を付けると入力を使い切っても終わらず、入力ファイルへ行が足されるのを待つ(「!quit」で終了)。
/// 入力待ちになる度に screen.log(今の画面)と step.txt(送った入力の数)を書くので、対話的に進められる。
/// ゲームはセーブやログを自分のフォルダへ書くので、写しを渡すこと
/// </summary>
[InitializeOnLoad]
public static class PlayTest
{
    const string kActive = "PlayTest.active";
    const string kGame = "PlayTest.game";
    const string kOut = "PlayTest.out";
    const string kInputs = "PlayTest.inputs";
    const string kTimeout = "PlayTest.timeout";
    const string kLive = "PlayTest.live";

    static PlayTest()
    {
        //プレイモードへ入る時にスクリプトが読み直されるので、その度に付け直す
        if(SessionState.GetBool(kActive, false))
            EditorApplication.update += Tick;
    }

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        var game = Arg(args, "-ptGame");
        var outdir = Arg(args, "-ptOut");
        if(string.IsNullOrEmpty(game) || string.IsNullOrEmpty(outdir) || !Directory.Exists(game))
        {
            Debug.LogError("PlayTest: -ptGame (existing folder) and -ptOut are required");
            EditorApplication.Exit(2);
            return;
        }
        Directory.CreateDirectory(outdir);
        SessionState.SetString(kGame, Path.GetFullPath(game).TrimEnd('\\', '/'));
        SessionState.SetString(kOut, Path.GetFullPath(outdir));
        SessionState.SetString(kInputs, Arg(args, "-ptInputs") ?? "");
        int timeout;
        if(!int.TryParse(Arg(args, "-ptTimeout"), out timeout) || timeout <= 0)
            timeout = 600;
        SessionState.SetInt(kTimeout, timeout);
        SessionState.SetBool(kLive, Array.IndexOf(args, "-ptLive") >= 0);
        SessionState.SetBool(kActive, true);

        EditorSceneManager.OpenScene("Assets/Main.unity");
        EditorApplication.EnterPlaymode();
    }

    static string Arg(string[] args, string name)
    {
        for(int i = 0; i + 1 < args.Length; ++i)
        {
            if(args[i] == name)
                return args[i + 1];
        }
        return null;
    }

    enum Phase { Boot, Loading, Playing, Done }
    static Phase phase_ = Phase.Boot;
    static double started_ = -1;
    static double idle_since_ = -1;
    static double sent_at_ = -1;
    static int frames_ = 0;
    static List<string> inputs_ = new List<string>();
    static int next_ = 0;
    static string last_state_ = "";

    static void Tick()
    {
        if(!EditorApplication.isPlaying || phase_ == Phase.Done)
            return;
        try
        {
            Step();
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            Finish(3, "exception: " + e.Message);
        }
    }

    static void Step()
    {
        double now = EditorApplication.timeSinceStartup;
        if(started_ < 0)
        {
            started_ = now;
            ReadInputs();
        }
        if(now - started_ > SessionState.GetInt(kTimeout, 600))
        {
            Dump("final");
            Finish(4, "timeout in " + phase_ + ", console " + last_state_);
            return;
        }

        if(phase_ == Phase.Boot)
        {
            //最初の画面(ゲーム一覧)が出来るまで待ってから、一覧を閉じて指定のゲームを始める
            ++frames_;
            var first = UnityEngine.Object.FindObjectOfType<FirstWindow>();
            if(first == null || EmueraContent.instance == null || frames_ < 30)
                return;
            UnityEngine.Object.Destroy(first.gameObject);
            var game = SessionState.GetString(kGame, "");
            var run = typeof(FirstWindow).GetMethod("Run", BindingFlags.Static | BindingFlags.NonPublic);
            var co = (IEnumerator)run.Invoke(null, new object[] { Path.GetDirectoryName(game), Path.GetFileName(game) });
            GenericUtils.StartCoroutine(co);
            Debug.Log("PlayTest: starting " + game);
            phase_ = Phase.Loading;
            return;
        }

        var console = Console();
        if(console == null)
            return;
        var state = ConsoleStateName(console);
        if(state != last_state_)
        {
            Debug.Log("PlayTest: console " + last_state_ + " -> " + state);
            last_state_ = state;
        }
        bool stopped = state == "Error" || state == "Quit";
        bool idle = stopped || state == "WaitInput";
        if(!idle || (sent_at_ >= 0 && now - sent_at_ < 0.5))
        {
            idle_since_ = -1;
            return;
        }
        if(idle_since_ < 0)
        {
            idle_since_ = now;
            return;
        }
        //ワーカースレッドが次の処理へ移る途中かもしれないので、少し続いてから入力待ちとみなす
        if(now - idle_since_ < 0.5)
            return;

        if(phase_ == Phase.Loading)
        {
            //読み込み後のメモリ。端末では大きいゲームで落ちる事があるので量っておく
            Debug.Log(string.Format("PlayTest: memory managed={0}MB monoUsed={1}MB monoHeap={2}MB private={3}MB load={4:F0}s",
                GC.GetTotalMemory(true) >> 20,
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() >> 20,
                UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() >> 20,
                System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 >> 20,
                now - started_));
            Dump("loaded");
            phase_ = Phase.Playing;
        }
        bool live = SessionState.GetBool(kLive, false);
        if(live)
        {
            //同じ入力待ちの間は1度だけ書く
            if(reported_ != next_)
            {
                reported_ = next_;
                Dump("screen");
                File.WriteAllText(Path.Combine(SessionState.GetString(kOut, "."), "step.txt"), next_ + " " + state);
            }
            if(!stopped)
                ReadInputs();
        }
        if(stopped)
        {
            Dump("final");
            Finish(state == "Error" ? 1 : 0, "console " + state);
            return;
        }
        if(now < wait_until_)
            return;
        while(next_ < inputs_.Count && (inputs_[next_].StartsWith("!log ") ||
            inputs_[next_].StartsWith("!mem ") || inputs_[next_].StartsWith("!wait ")))
        {
            var cmd = inputs_[next_++];
            var arg = cmd.Substring(cmd.IndexOf(' ') + 1).Trim();
            if(cmd.StartsWith("!log "))
                Dump(arg);
            else if(cmd.StartsWith("!mem "))
                LogTextures(arg);
            else
            {
                //画面側の後始末(使い終わったテクスチャの解放など)を待つ
                wait_until_ = now + double.Parse(arg);
                return;
            }
        }
        if(next_ >= inputs_.Count)
        {
            if(live)
                return;
            Dump("final");
            Finish(0, "inputs done");
            return;
        }
        var input = inputs_[next_++];
        if(input == "!quit")
        {
            Dump("final");
            Finish(0, "quit");
            return;
        }
        Debug.Log("PlayTest: input [" + input + "]");
        //空行はEnter。それ以外はボタンを押した扱いにして、INPUT待ちでも受け付けさせる
        EmueraThread.instance.Input(input, input.Length > 0);
        sent_at_ = now;
        idle_since_ = -1;
    }

    static int reported_ = -1;
    static double wait_until_ = -1;

    /// <summary>今あるTexture2Dの数と大きさを書く。画像を使うゲームのメモリの増え方を見る</summary>
    static void LogTextures(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<Texture2D>();
        long bytes = 0;
        foreach(var t in all)
            bytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
        Debug.Log(string.Format("PlayTest: textures {0} count={1} size={2}MB", name, all.Length, bytes >> 20));
    }

    /// <summary>入力ファイルを読み直す。-ptLiveでは実行中に行が足される</summary>
    static void ReadInputs()
    {
        var path = SessionState.GetString(kInputs, "");
        if(string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        try
        {
            string text;
            using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var reader = new StreamReader(fs, Encoding.UTF8))
                text = reader.ReadToEnd();
            //書きかけの最後の行は、改行が付くまで読まない
            int end = text.LastIndexOf((char)10);
            if(end < 0)
                return;
            var lines = text.Substring(0, end).Replace(((char)13).ToString(), "").Split((char)10);
            for(int i = inputs_.Count; i < lines.Length; ++i)
                inputs_.Add(lines[i]);
        }
        catch(IOException)
        {
        }
    }

    static object Console()
    {
        var t = typeof(EmueraThread).Assembly.GetType("MinorShift.Emuera.GlobalStatic");
        return t.GetField("Console", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
    }

    static string ConsoleStateName(object console)
    {
        var f = console.GetType().GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
        return f != null ? f.GetValue(console).ToString() : "?";
    }

    /// <summary>今の画面(履歴ごと)を 名前.log へ書く</summary>
    static void Dump(string name)
    {
        var sb = new StringBuilder();
        var console = Console();
        if(console != null)
        {
            var m = console.GetType().GetMethod("GetDisplayStrings", BindingFlags.Instance | BindingFlags.Public);
            m.Invoke(console, new object[] { sb });
        }
        var path = Path.Combine(SessionState.GetString(kOut, "."), name + ".log");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Debug.Log("PlayTest: wrote " + path + " (" + sb.Length + " chars)");
    }

    static void Finish(int code, string reason)
    {
        phase_ = Phase.Done;
        SessionState.SetBool(kActive, false);
        Debug.Log("PlayTest: finished " + code + " " + reason);
        try
        {
            File.WriteAllText(Path.Combine(SessionState.GetString(kOut, "."), "result.txt"),
                code + " " + reason + "\n", new UTF8Encoding(false));
        }
        catch(Exception) { }
        EditorApplication.Exit(code);
    }
}
