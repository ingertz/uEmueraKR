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
        internal void MouseDownFromUnity(int x, int y, int button)
        {
            if(!IsWaitingPrimitive)
                return;
            //ERB側は左下基準の座標を受け取る
            InputMouseKey(1, button, x, y - ClientHeight, -1);
        }
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
