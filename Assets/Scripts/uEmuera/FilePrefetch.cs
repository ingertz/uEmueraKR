using System;
using System.Collections.Generic;
using System.Threading;

namespace uEmuera
{
    /// <summary>
    /// 読み込みと解析を重ねる。
    ///
    /// ERBが1万ファイルあるゲームでは、読み込みだけで13秒かかっていた。
    /// 1ファイル1ms強で、その殆どは端末の記憶域の待ち時間。計算ではないので、
    /// 解析している間に次を読んでおけばそのまま消える。
    ///
    /// 解析の順序には触らない。中身を先に用意しておくだけ
    /// </summary>
    public static class FilePrefetch
    {
        /// <summary>
        /// 同時に抱える最大数。全部読むと数百MBになるので歯止めが要る
        /// </summary>
        const int kWindow = 128;

        static readonly object lock_ = new object();
        static Dictionary<string, string> ready_;
        static HashSet<string> managed_;
        static List<string> order_;
        static int next_;
        static int inflight_;
        static int workers_;

        public static void Begin(List<string> paths, int workers)
        {
            End();
            if(paths == null || paths.Count == 0)
                return;
            if(workers < 1)
                workers = 1;
            lock(lock_)
            {
                order_ = new List<string>(paths);
                managed_ = new HashSet<string>(paths);
                ready_ = new Dictionary<string, string>();
                next_ = 0;
                inflight_ = 0;
                workers_ = workers;
            }
            for(int i = 0; i < workers; ++i)
            {
                var t = new Thread(Work);
                t.IsBackground = true;
                t.Start();
            }
        }

        /// <summary>
        /// 用意してある中身を取り出す。まだなら出来るまで待つ。
        /// 管理していないパスなら偽を返して、呼び元にそのまま読ませる
        /// </summary>
        public static bool TryTake(string path, out string text)
        {
            text = null;
            lock(lock_)
            {
                if(managed_ == null || !managed_.Contains(path))
                    return false;
                while(ready_ != null && !ready_.ContainsKey(path))
                {
                    //読み手が全て終わっているのに無いなら、もう来ない
                    if(workers_ <= 0)
                        return false;
                    Monitor.Wait(lock_);
                }
                if(ready_ == null)
                    return false;
                text = ready_[path];
                ready_.Remove(path);
                managed_.Remove(path);
                Monitor.PulseAll(lock_);
                return text != null;
            }
        }

        public static void End()
        {
            lock(lock_)
            {
                order_ = null;
                ready_ = null;
                managed_ = null;
                Monitor.PulseAll(lock_);
            }
        }

        static void Work()
        {
            try
            {
                while(true)
                {
                    string path;
                    lock(lock_)
                    {
                        while(order_ != null && next_ < order_.Count &&
                            ready_.Count + inflight_ >= kWindow)
                            Monitor.Wait(lock_);
                        if(order_ == null || next_ >= order_.Count)
                            return;
                        path = order_[next_++];
                        inflight_ += 1;
                    }

                    string text = null;
                    try { text = TextFileReader.ReadAllText(path); }
                    catch(Exception) { }

                    lock(lock_)
                    {
                        inflight_ -= 1;
                        if(ready_ == null)
                            return;
                        //読めなかった事も残す。待ち手を止めないため
                        ready_[path] = text;
                        Monitor.PulseAll(lock_);
                    }
                }
            }
            finally
            {
                lock(lock_)
                {
                    workers_ -= 1;
                    Monitor.PulseAll(lock_);
                }
            }
        }
    }
}
