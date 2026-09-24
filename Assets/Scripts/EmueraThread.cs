using System;
using System.Threading;
using UnityEngine;

public class EmueraThread
{
    public static EmueraThread instance { get { return instance_; } }
    static EmueraThread instance_ = new EmueraThread();

    EmueraThread()
    { }

    public void Start(bool debug, bool use_coroutine)
    {
        debugmode = debug;
        running = true;
        if(use_coroutine)
        {
            coroutine = GenericUtils.StartCoroutine(WorkCo());
            return;
        }
        ThreadPool.QueueUserWorkItem(new WaitCallback(p =>
        {
            Work();
        }));
    }

    public void End()
    {
        if(coroutine != null)
        {
            GenericUtils.StopCoroutine(coroutine);
            coroutine = null;
        }
        running = false;
        wake_.Set();
    }

    /// <summary>
    /// 入力待ちのワーカーを起こす。
    ///
    /// 以前は1ミリ秒ごとに目を覚まして入力の有無を見ていた。
    /// 文章を読んでいる間もずっと秒千回起き続けるので、
    /// CPUが深く眠れず端末が温まり続けていた。
    /// 入力が来た時に起こせば、待っている間は本当に眠っていられる
    /// </summary>
    readonly System.Threading.ManualResetEventSlim wake_ =
        new System.Threading.ManualResetEventSlim(false, 0);

    /// <summary>
    /// 時計が動いている時だけこまめに起きる。
    /// 時計が無ければ長く眠る。入力は合図で即座に起こされるので待たせない
    /// </summary>
    static int WaitMilliseconds()
    {
        return uEmuera.Forms.Timer.HasEnabled() ? 32 : 500;
    }

    public bool Running()
    {
        var console = MinorShift.Emuera.GlobalStatic.Console;
        if(console != null && console.IsInProcess)
            return true;
        return false;
    }

    public void Input(string c, bool from_button, bool skip = false)
    {
        var console = MinorShift.Emuera.GlobalStatic.Console;
        if(console == null)
            return;
        if(!from_button && console.IsWaitingInputSomething)
            return;
        input = c;
        skipflag = skip;
        //ボタン由来の入力はマクロ解析を通さず、ONEINPUT系でも切り詰めない。
        //本家もマウス操作ではPressEnterKeyへtrueを渡している
        from_button_ = from_button;
        wake_.Set();
    }
    bool from_button_ = false;
    /// <summary>
    /// INPUTMOUSEKEY待ちへクリックを渡す。
    /// コンソールの操作はワーカー側で行う必要があるので、ここでは予約だけする
    /// </summary>
    public void InputMouse(int x, int y, int button)
    {
        var console = MinorShift.Emuera.GlobalStatic.Console;
        if(console == null || !console.IsWaitingPrimitive)
            return;
        mouse_x_ = x;
        mouse_y_ = y;
        mouse_button_ = button;
        mouse_pending_ = true;
        from_button_ = false;
        input = "";//ワーカーを起こす
        wake_.Set();
    }
    volatile bool mouse_pending_ = false;
    int mouse_x_ = 0;
    int mouse_y_ = 0;
    int mouse_button_ = 0;

    /// <summary>予約されたマウス入力があれば処理する。ワーカー側から呼ぶ</summary>
    bool ConsumeMouseInput(MinorShift.Emuera.GameView.EmueraConsole console)
    {
        if(!mouse_pending_)
            return false;
        mouse_pending_ = false;
        console.MouseDownFromUnity(mouse_x_, mouse_y_, mouse_button_);
        return true;
    }
    public bool IsSkipFlag { get { return skipflag; } }

    void Work()
    {
        //初始化
        MinorShift.Emuera.Program.debugMode = debugmode;
        MinorShift.Emuera.Program.Main(new string[0] { });

        uEmuera.Utils.ResourceClear();
        GC.Collect();

        input = null;
        var console = MinorShift.Emuera.GlobalStatic.Console;
        var random = new System.Random();
        while(running)
        {
            skipflag = false;

            while(input == null)
            {
                //合図を消してから見直す。この順でないと、
                //消す直前に来た合図を取りこぼす
                wake_.Reset();
                if(input != null)
                    break;
                wake_.Wait(WaitMilliseconds());
                if(!running)
                    return;
                uEmuera.Forms.Timer.Update();
            }

            if(ConsumeMouseInput(console))
            {
                //INPUTMOUSEKEY待ちはPressEnterKeyでは解けない
            }
            else if(console.IsWaitingInput)
            {
                if(console.IsWaitingEnterKey)
                    input = "";
                console.PressEnterKey(skipflag, input, from_button_);
            }
            Thread.Sleep(10);
            input = null;
        }
    }

    System.Collections.IEnumerator WorkCo()
    {
        //初始化
        MinorShift.Emuera.Program.debugMode = debugmode;
        MinorShift.Emuera.Program.Main(new string[0] { });

        uEmuera.Utils.ResourceClear();
        GC.Collect();
        yield return null;

        input = null;
        var console = MinorShift.Emuera.GlobalStatic.Console;
        while(running)
        {
            skipflag = false;

            while(input == null)
            {
                yield return null;
                if(!running)
                    yield break;
                uEmuera.Forms.Timer.Update();
            }

            if(ConsumeMouseInput(console))
            {
                //INPUTMOUSEKEY待ちはPressEnterKeyでは解けない
            }
            else if(console.IsWaitingInput)
            {
                if(console.IsWaitingEnterKey)
                    input = "";
                console.PressEnterKey(skipflag, input, from_button_);
            }
            yield return new WaitForSeconds(0.01f);
            input = null;
        }
    }

    UnityEngine.Coroutine coroutine = null;
    bool debugmode;
    volatile bool running;
    volatile string input;
    bool skipflag;
}
