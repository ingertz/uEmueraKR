using System;

namespace MinorShift.Emuera.GameView
{
    internal sealed partial class EmueraConsole : IDisposable
    {
        internal ConsoleDisplayLine GetDisplayLinesForuEmuera(int index)
        {
            if(index < 0 || index >= displayLineList.Count)
                return null;
            return displayLineList[index];
        }
        internal int GetDisplayLinesCount()
        {
            return displayLineList.Count;
        }
        internal bool IsInitializing
        {
            get { return state == ConsoleState.Initializing; }
        }
        internal int LastButtonGeneration
        {
            get { return lastButtonGeneration; }
        }
        internal bool IsWaitingInput
        {
            get { return state == ConsoleState.WaitInput; }
        }
        /// <summary>
        /// INPUTMOUSEKEYの待ちを解除する。WinForms版のMouseDown相当。
        /// PressEnterKeyでは PrimitiveMouseKey の待ちを解けないため、
        /// Unity側のクリックはこちらへ流す必要がある
        /// </summary>
        /// <param name="x">クライアント左上基準のX</param>
        /// <param name="y">クライアント左上基準のY</param>
        /// <param name="button">WinFormsのMouseButtons値(左=0x100000)</param>
        internal void MouseDownFromUnity(int x, int y, int button, int cbgButton = -1)
        {
            if(!IsWaitingPrimitive)
                return;
            //ERB側は左下基準の座標を受け取る。RESULT:4はCBGのボタン番号(無ければ-1)
            InputMouseKey(1, button, x, y - ClientHeight, cbgButton);
        }
        #region HTML_PRINT_ISLAND
        //本家(Emuera.NET)の_htmlElementList。ログに残らず、画面の上端から1行ずつ重ねて描く
        readonly System.Collections.Generic.List<ConsoleDisplayLine> islandLines =
            new System.Collections.Generic.List<ConsoleDisplayLine>();
        volatile int islandVersion = 0;
        internal int IslandVersion { get { return islandVersion; } }

        public void PrintHTMLIsland(string html)
        {
            var lines = HtmlManager.Html2DisplayLine(html, StrMeasure, this);
            lock (islandLines)
            {
                if (lines != null)
                    islandLines.AddRange(lines);
                ++islandVersion;
            }
        }
        public void ClearHTMLIsland()
        {
            lock (islandLines)
            {
                if (islandLines.Count == 0)
                    return;
                islandLines.Clear();
                ++islandVersion;
            }
        }
        /// <summary>Unity側へ渡す写し。ワーカースレッドで書き換わるのでロックして取る</summary>
        internal ConsoleDisplayLine[] GetIslandSnapshot()
        {
            lock (islandLines)
                return islandLines.ToArray();
        }
        #endregion

        #region CBG(クライアント背景)をUnity側へ渡す
        /// <summary>
        /// CBGの内容が変わるたびに増える。Unity側はこれを見て作り直す
        /// </summary>
        volatile int cbgVersion = 0;
        internal int CBGVersion { get { return cbgVersion; } }

        /// <summary>Unity側へ渡すCBG1枚分の写し</summary>
        internal struct CBGItem
        {
            public Content.ASprite Img;
            public int X;
            public int Y;
            public int ZDepth;
            public bool IsButton;
            public int ButtonValue;
        }

        /// <summary>
        /// CBGの一覧を奥から手前の順で返す。
        /// 一覧はワーカースレッドで書き換わるので、ロックして写しを作る
        /// </summary>
        internal System.Collections.Generic.List<CBGItem> GetCBGSnapshot(out Content.GraphicsImage buttonMap)
        {
            var ret = new System.Collections.Generic.List<CBGItem>();
            lock (cbgList)
            {
                buttonMap = cbgButtonMap;
                for (int i = 0; i < cbgList.Count; ++i)
                {
                    var c = cbgList[i];
                    if (c.zdepth == 0 || c.Img == null)
                        continue;//0は文字列の位置を示すダミー
                    ret.Add(new CBGItem
                    {
                        Img = c.Img,
                        X = c.x,
                        Y = c.y,
                        ZDepth = c.zdepth,
                        IsButton = c.isButton,
                        ButtonValue = c.buttonValue,
                    });
                }
            }
            return ret;
        }
        #endregion

        internal bool IsWaitingInputSomething
        {
            get {
                return state == ConsoleState.WaitInput &&
                          (inputReq.InputType == GameProc.InputType.IntValue || 
                          inputReq.InputType == GameProc.InputType.StrValue ||
                          inputReq.InputType == GameProc.InputType.IntButton ||
                          inputReq.InputType == GameProc.InputType.StrButton);
            }
        }

        /// <summary>
        /// EM/EE: マウス入力付きINPUTへのクリック結果を書き込む(本家MainWindowのMouseUp相当)。
        /// RESULT:1=マウスボタン(1左 2右 3中) RESULT:2=修飾キー RESULTS:1=ボタン文字列 RESULT:3=マスク色。
        /// ボタン以外を押した時は制限時間を止め、既定値で入力を終えられるようにする
        /// </summary>
        internal void ApplyMouseInputResult(string buttonStr, int mouseButton, long mappedColor)
        {
            if (!IsWaitingInputWithMouse)
                return;
            var result = GlobalStatic.VEvaluator.RESULT_ARRAY;
            var results = GlobalStatic.VEvaluator.RESULTS_ARRAY;
            if (buttonStr != null)
            {
                result[3] = mappedColor;
                results[1] = buttonStr;
            }
            result[1] = mouseButton;
            result[2] = 0;   //タッチには修飾キーが無い
            if (buttonStr == null)
                inputReq.Timelimit = 0;
        }

        /// <summary>EM/EE: マウス入力付きのINPUT待ちか(INPUT系の第2引数)</summary>
        internal bool IsWaitingInputWithMouse
        {
            get { return state == ConsoleState.WaitInput && inputReq != null && inputReq.MouseInput; }
        }

        /// <summary>
        /// 現在の世代のボタン(と、その行に含まれるdivの中のボタン)に条件を満たす物があるか。
        /// 本家BINPUT系と同じく、後ろの行から見て古い世代のボタンに着いたら打ち切る
        /// </summary>
        internal bool FindCurrentButton(System.Predicate<ConsoleButtonString> pred)
        {
            for (int i = displayLineList.Count - 1; i >= 0; --i)
            {
                var buttons = displayLineList[i].Buttons;
                for (int b = 0; b < buttons.Length; ++b)
                {
                    var button = buttons[b];
                    if (button.Generation != 0 && button.Generation != lastButtonGeneration)
                        return false;
                    if (button.IsButton && pred(button))
                        return true;
                    if (FindInDivs(button, pred))
                        return true;
                }
            }
            return false;
        }
        static bool FindInDivs(ConsoleButtonString button, System.Predicate<ConsoleButtonString> pred)
        {
            var parts = button.StrArray;
            for (int p = 0; p < parts.Length; ++p)
            {
                var div = parts[p] as ConsoleDivPart;
                if (div == null || div.Children == null)
                    continue;
                foreach (var line in div.Children)
                {
                    if (line == null)
                        continue;
                    foreach (var child in line.Buttons)
                    {
                        if (child.IsButton && pred(child))
                            return true;
                        if (FindInDivs(child, pred))
                            return true;
                    }
                }
            }
            return false;
        }
        internal GameProc.InputType InputType
        {
            get
            {
                if(inputReq == null)
                    return GameProc.InputType.Void;
                return inputReq.InputType;
            }
        }
    }
}
