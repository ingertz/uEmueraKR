using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace uEmuera
{
    /// <summary>
    /// 警告とエラーをゲームフォルダの uEmuera.log に書き出す。
    ///
    /// Androidの標準のログ(logcat)は利用者が取り出しにくく、不具合の報告を受けても
    /// 原因が分からなかった。ゲームを開くまでの分は覚えておき、ゲームフォルダが
    /// 決まった時点でまとめて書く。ファイルはゲームを開く度に作り直し、大きさに上限を設ける
    /// </summary>
    public static class FileLog
    {
        const long kMaxBytes = 1024 * 1024;
        const int kMaxPending = 200;

        static readonly object lock_ = new object();
        static readonly List<string> pending_ = new List<string>();
        static string path_ = null;
        static long written_ = 0;
        static bool started_ = false;
        //UnityのAPIはメインスレッドでしか呼べない物があるので、起動時に作っておく
        static string device_ = "";

        /// <summary>アプリ起動時に一度呼ぶ</summary>
        public static void Start()
        {
            if (started_)
                return;
            started_ = true;
            try
            {
                device_ = "uEmuera " + Application.version + " / " + SystemInfo.operatingSystem
                    + " / " + SystemInfo.deviceModel;
            }
            catch { }
            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>ゲームフォルダが決まった時に呼ぶ(どのスレッドからでもよい)。以前のログは消して書き直す</summary>
        public static void SetGameDir(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return;
            lock (lock_)
            {
                try
                {
                    path_ = Path.Combine(dir, "uEmuera.log");
                    var header = device_ + " / " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        + Environment.NewLine;
                    File.WriteAllText(path_, header, Encoding.UTF8);
                    written_ = header.Length;
                    foreach (var line in pending_)
                        Append(line);
                    pending_.Clear();
                }
                catch
                {
                    path_ = null;
                }
            }
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                return;
            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ")
              .Append(type).Append(": ").Append(condition).Append(Environment.NewLine);
            if (type != LogType.Warning && !string.IsNullOrEmpty(stackTrace))
                sb.Append(stackTrace).Append(Environment.NewLine);
            var text = sb.ToString();
            lock (lock_)
            {
                if (path_ == null)
                {
                    if (pending_.Count < kMaxPending)
                        pending_.Add(text);
                    return;
                }
                Append(text);
            }
        }

        static void Append(string text)
        {
            if (path_ == null || written_ >= kMaxBytes)
                return;
            try
            {
                File.AppendAllText(path_, text, Encoding.UTF8);
                written_ += text.Length;
            }
            catch
            {
                path_ = null;
            }
        }
    }
}
