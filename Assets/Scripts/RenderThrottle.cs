using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 画面に変化が無い間は描画の回数を落として、発熱と電池の消費を抑える。
///
/// テキストゲームは大半の時間を入力待ちで過ごし、その間の画面は1枚の絵のまま変わらない。
/// それでも毎秒24回描き直していたので、変化が無くなって少し経ったら
/// OnDemandRenderingで描画だけを間引く(Updateや入力の受け付けは毎フレーム続く)。
///
/// 画面を変える処理はWake()を呼ぶ。触っている間、キーボードを出している間、
/// スクリプトの実行中、アニメするスプライトの表示中も起きたままにする。
/// 呼び忘れがあっても、間引いた後の描画(毎秒2回)で遅れて反映される
/// </summary>
public class RenderThrottle : MonoBehaviour
{
    /// <summary>最後の変化からこの秒数が過ぎたら間引く</summary>
    const float kIdleSeconds = 1.0f;
    /// <summary>間引いた時の描画間隔(フレーム数)。24fpsなら毎秒2回</summary>
    const int kIdleInterval = 12;

    static volatile bool wake_requested_ = true;
    static RenderThrottle instance_ = null;

    float last_active_ = 0;
    int screen_w_ = 0;
    int screen_h_ = 0;

    /// <summary>画面を変えた時に呼ぶ。どのスレッドから呼んでもよい</summary>
    public static void Wake()
    {
        wake_requested_ = true;
    }

    public static void Create()
    {
        if (instance_ != null)
            return;
        var obj = new GameObject("RenderThrottle");
        DontDestroyOnLoad(obj);
        instance_ = obj.AddComponent<RenderThrottle>();
    }

    void Update()
    {
        bool active = wake_requested_;
        wake_requested_ = false;

        if (Input.touchCount > 0 || Input.anyKey || Input.mouseScrollDelta != Vector2.zero)
            active = true;
        if (TouchScreenKeyboard.visible)
            active = true;
        if (Screen.width != screen_w_ || Screen.height != screen_h_)
        {
            screen_w_ = Screen.width;
            screen_h_ = Screen.height;
            active = true;
        }
        //スクリプトの実行中(処理中の表示が出ている間)は出力が続く
        var console = MinorShift.Emuera.GlobalStatic.Console;
        if (console != null && console.IsInProcess)
            active = true;
        var content = EmueraContent.instance;
        if (content != null && content.option_window != null &&
            content.option_window.inprogress != null && content.option_window.inprogress.activeSelf)
            active = true;

        if (active)
            last_active_ = Time.unscaledTime;
        int interval = Time.unscaledTime - last_active_ > kIdleSeconds ? kIdleInterval : 1;
        if (OnDemandRendering.renderFrameInterval != interval)
            OnDemandRendering.renderFrameInterval = interval;
    }

    void OnApplicationFocus(bool focus)
    {
        Wake();
    }

    void OnApplicationPause(bool pause)
    {
        Wake();
    }
}
