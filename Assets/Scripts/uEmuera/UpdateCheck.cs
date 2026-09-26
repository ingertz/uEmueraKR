using System;
using System.IO;
using System.Net;
using System.Threading;

namespace uEmuera
{
    /// <summary>
    /// EE_UPDATECHECKの本体。ゲームのスレッドから呼ばれ、確認の画面だけメインスレッドで出す。
    ///
    /// 本家と同じく、URLの1行目をバージョン名、2行目をリンクとして読み、
    /// GAMEBASE.CSVの「バージョン名」と違えばリンクを開くかどうか尋ねる。
    /// RESULT 0:最新 1:新版あり(開かない) 2:新版あり(開いた) 3:取得失敗 4:禁止設定 5:ネットワーク無し
    /// </summary>
    public static class UpdateCheck
    {
        public static long Run(string url, string versionName)
        {
            if (MinorShift.Emuera.Config.ForbidUpdateCheck)
                return 4;
            if (!IsNetworkAvailable())
                return 5;
            if (string.IsNullOrEmpty(url))
                return 3;

            string version;
            string link;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;
                using (var response = request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    version = reader.ReadLine();
                    link = reader.ReadLine();
                }
            }
            catch
            {
                return 3;
            }
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(link))
                return 3;
            if (version == versionName)
                return 0;

            bool open = AskOpen(version, link);
            if (!open)
                return 1;
            SpriteManager.RunOnMainThread(() => UnityEngine.Application.OpenURL(link));
            return 2;
        }

        static bool IsNetworkAvailable()
        {
            bool available = true;
            var done = new ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() =>
            {
                try
                {
                    available = UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable;
                }
                finally
                {
                    done.Set();
                }
            });
            done.WaitOne();
            return available;
        }

        /// <summary>
        /// 新しい版があることを知らせ、リンクを開くかどうか尋ねる(本家の既定の答えは「いいえ」)
        /// </summary>
        static bool AskOpen(string version, string link)
        {
            bool answer = false;
            var done = new ManualResetEvent(false);
            SpriteManager.RunOnMainThread(() =>
            {
                var content = EmueraContent.instance;
                if (content == null || content.option_window == null)
                {
                    done.Set();
                    return;
                }
                content.option_window.ShowConfirm(
                    "UPDATECHECK",
                    "新しいバージョン(" + version + ")があります。\n" + link + "\nを開きますか？",
                    () => { answer = true; done.Set(); },
                    () => { answer = false; done.Set(); });
            });
            done.WaitOne();
            return answer;
        }
    }
}
