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
                          inputReq.InputType == GameProc.InputType.StrValue);
            }
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
