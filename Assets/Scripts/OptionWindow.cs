using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionWindow : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        GenericUtils.SetListenerOnClick(quick_button.gameObject, OnQuickButtonClick);
        GenericUtils.SetListenerOnClick(input_button.gameObject, OnInputPadButtonClick);
        GenericUtils.SetListenerOnClick(magnifier_button.gameObject, OnScalePadButtonClick);
        GenericUtils.SetListenerOnClick(option_button.gameObject, OnShowMenu2);

        orientation_lock_image = orientation_lock_button.GetComponent<Image>();
        GenericUtils.SetListenerOnClick(orientation_lock_button.gameObject, OnLockOrientationClick);

        GenericUtils.SetListenerOnClick(msg_confirm, OnMsgConfirm);
        GenericUtils.SetListenerOnClick(msg_cancel, OnMsgCancel);

        GenericUtils.SetListenerOnClick(menu_pad, OnMenuPad);
        GenericUtils.SetListenerOnClick(menu_1_resolution, OnMenuResolution);
#if !UNITY_STANDALONE
        if (menu_1_resolution != null)
            menu_1_resolution.SetActive(false);
#endif
        GenericUtils.SetListenerOnClick(menu_1_language, ShowLanguageBox);
        if(menu_1_path != null)
            GenericUtils.SetListenerOnClick(menu_1_path, OnMenuPath);
        GenericUtils.SetListenerOnClick(menu_1_github, OnGithub);
        GenericUtils.SetListenerOnClick(menu_1_exit, OnMenuExit);

        GenericUtils.SetListenerOnClick(menu_2_back, OnMenu2Back);
        GenericUtils.SetListenerOnClick(menu_2_restart, OnMenu2Restart);
        GenericUtils.SetListenerOnClick(menu_2_gototitle, OnMenuGotoTitle);
        GenericUtils.SetListenerOnClick(menu_2_savelog, OnMenuSaveLog);
        GenericUtils.SetListenerOnClick(menu_2_intent, OnIntentBoxShow);
        GenericUtils.SetListenerOnClick(menu_2_exit, OnMenuExit);

        GenericUtils.SetListenerOnClick(resolution_pad, OnResolutionOut);
        GenericUtils.SetListenerOnClick(resolution_1080p, OnResolution1080p);
        GenericUtils.SetListenerOnClick(resolution_900p, OnResolution900p);
        GenericUtils.SetListenerOnClick(resolution_720p, OnResolution720p);
        GenericUtils.SetListenerOnClick(resolution_540p, OnResolution540p);

        GenericUtils.SetListenerOnClick(language_zhcn, OnSelectLanguage);
        GenericUtils.SetListenerOnClick(language_jp, OnSelectLanguage);
        GenericUtils.SetListenerOnClick(language_enus, OnSelectLanguage);
        GenericUtils.SetListenerOnClick(language_ko, OnSelectLanguage);

        BuildIntentBox();
        GenericUtils.SetListenerOnClick(intentbox_close, OnIntentClose);
        GenericUtils.SetListenerOnClick(intentbox_reset, OnIntentReset);

        HideResolutionIcon();
        switch(ResolutionHelper.resolution_index)
        {
        case 2:
            resolution_900p_icon.SetActive(true);
            break;
        case 3:
            resolution_720p_icon.SetActive(true);
            break;
        case 4:
            resolution_540p_icon.SetActive(true);
            break;
        case 1:
        default:
            resolution_1080p_icon.SetActive(true);
            break;
        }
    }

    void OnQuickButtonClick()
    {
        if(quick_buttons.IsShow)
        {
            quick_buttons.Hide();
            SwitchButton(-1);
        }
        else
        {
            input_pad.Hide();
            scale_pad.Hide();
            quick_buttons.Show();
            EmueraContent.instance.SetLastButtonGeneration(
                EmueraContent.instance.button_generation);

            SwitchButton(0);
        }
    }
    void OnInputPadButtonClick()
    {
        if(input_pad.IsShow)
        {
            input_pad.Hide();
            SwitchButton(-1);
        }
        else
        {
            quick_buttons.Hide();
            scale_pad.Hide();
            input_pad.Show();
            SwitchButton(1);
        }
    }
    void OnScalePadButtonClick()
    {
        if(scale_pad.IsShow)
        {
            scale_pad.Hide();
            SwitchButton(-1);
        }
        else
        {
            quick_buttons.Hide();
            input_pad.Hide();
            scale_pad.Show();
            SwitchButton(2);
        }
    }
    void OnLockOrientationClick()
    {
        if(auto_rotation)
        {
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            orientation_lock_image.sprite = lock_sprite;
        }
        else
        {
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            orientation_lock_image.sprite = unlock_sprite;
        }
    }

    public void ShowMenu()
    {
        menu_pad.SetActive(true);
        menu_1.SetActive(true);
    }

    void OnShowMenu2()
    {
        menu_pad.SetActive(true);
        menu_2.SetActive(true);
    }
    void OnMenu2Back()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"), 
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[BackMenu]"),
                MultiLanguage.GetText("[BackMenuContent]"),
                () =>
                {
                    var emuera = GameObject.FindObjectOfType<EmueraMain>();
                    emuera.Clear();
                }, () => { });
        }
        HideMenu();
    }
    void OnMenu2Restart()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"),
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[ReloadGame]"),
                MultiLanguage.GetText("[ReloadGameContent]"),
            () =>
            {
                var emuera = GameObject.FindObjectOfType<EmueraMain>();
                emuera.Restart();
            }, () => { });
        }
        HideMenu();
    }
    void OnMenuGotoTitle()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"),
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[BackTitle]"),
                MultiLanguage.GetText("[BackTitleContent]"),
            () =>
            {
                MinorShift.Emuera.GlobalStatic.Console.GotoTitle();
            }, () => { });
        }
        HideMenu();
    }
    void OnMenuSaveLog()
    {
        var path = MinorShift.Emuera.Program.ExeDir;
        System.DateTime time = System.DateTime.Now;
        string fname = time.ToString("yyyyMMdd-HHmmss");
        path = path + fname + ".log";
        bool result = MinorShift.Emuera.GlobalStatic.Console.OutputLog(path);

        ShowMessageBox(MultiLanguage.GetText("[SaveLog]"), 
            result ? string.Format("{1}：\n{0}", path, MultiLanguage.GetText("[SavePath]")) : MultiLanguage.GetText("[Failure]"));
        HideMenu();
    }
    void OnMenuResolution()
    {
        resolution_pad.SetActive(true);
        HideMenu();
    }
    void OnMenuExit()
    {
        ShowMessageBox(
            MultiLanguage.GetText("[Exit]"),
            MultiLanguage.GetText("[ExitContent]"), 
            ()=> {
                Application.Quit();
            }, ()=> { });
        HideMenu();
    }

    void OnMenuPad()
    {
        HideMenu();
    }

    void OnResolutionOut()
    {
        resolution_pad.SetActive(false);
    }

    void OnResolution1080p()
    {
        ResolutionHelper.resolution_index = 1;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_1080p_icon.SetActive(true);
    }

    void OnResolution900p()
    {
        ResolutionHelper.resolution_index = 2;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_900p_icon.SetActive(true);
    }

    void OnResolution720p()
    {
        ResolutionHelper.resolution_index = 3;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_720p_icon.SetActive(true);
    }

    void OnResolution540p()
    {
        ResolutionHelper.resolution_index = 4;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_540p_icon.SetActive(true);
    }

    void HideResolutionIcon()
    {
        resolution_1080p_icon.SetActive(false);
        resolution_900p_icon.SetActive(false);
        resolution_720p_icon.SetActive(false);
        resolution_540p_icon.SetActive(false);
    }

    public void Ready()
    {
        var texts = inprogress.GetComponentsInChildren<Text>();
        var length = texts.Length;
        for(int i=0; i<length; ++i)
        {
            var text = texts[i];
            text.color = EmueraBehaviour.FontColor;
        }

        var buttoncolor = EmueraBehaviour.FontColor;
        buttoncolor.a = 0.6f;
        quick_button.GetComponent<Image>().color = buttoncolor;
        input_button.GetComponent<Image>().color = buttoncolor;
        magnifier_button.GetComponent<Image>().color = buttoncolor;
        option_button.GetComponent<Image>().color = buttoncolor;
        orientation_lock_image.color = buttoncolor;
        scale_pad.SetColor(buttoncolor);
        input_pad.SetColor(buttoncolor, 
            GenericUtils.ToUnityColor(MinorShift.Emuera.Config.BackColor));

        buttoncolor.a = 1.0f;
        button_shadows = new List<Shadow>();
        var shadow = quick_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = input_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = magnifier_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = option_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
    }

    public void ShowGameButton(bool value)
    {
        game_button.SetActive(value);
        if(auto_rotation)
            orientation_lock_image.sprite = unlock_sprite;
        else
            orientation_lock_image.sprite = lock_sprite;
    }

    public void ShowInProgress(bool value)
    {
        inprogress.SetActive(value);
    }

    void SwitchButton(int index)
    {
        for(int i=0; i < button_shadows.Count; ++i)
        {
            var shadow = button_shadows[i];
            shadow.enabled = (i == index);
        }
    }

    void HideMenu()
    {
        menu_1.SetActive(false);
        menu_2.SetActive(false);
        menu_pad.SetActive(false);
    }

    /// <summary>
    /// ゲーム側(UPDATECHECKなど)から確認を求める。メインスレッドで呼ぶこと
    /// </summary>
    public void ShowConfirm(string title, string content,
        System.Action confirm_callback, System.Action cancel_callback)
    {
        ShowMessageBox(title, content, confirm_callback, cancel_callback);
    }

    void ShowMessageBox(string title, string content,
        System.Action confirm_callback = null,
        System.Action cancel_callback = null)
    {
        msg_confirm_callback = confirm_callback;
        msg_cancel_callback = cancel_callback;
        msg_cancel.SetActive(msg_cancel_callback != null);
        msg_title.text = title;
        msg_content.text = content;
        msg_box.SetActive(true);
        RenderThrottle.Wake();
    }
    void HideMessageBox()
    {
        msg_title.text = "";
        msg_content.text = "";
        msg_confirm_callback = null;
        msg_cancel_callback = null;
        msg_box.SetActive(false);
    }
    void OnMsgConfirm()
    {
        if(msg_confirm_callback != null)
            msg_confirm_callback();
        HideMessageBox();
    }
    void OnMsgCancel()
    {
        if(msg_cancel_callback != null)
            msg_cancel_callback();
        HideMessageBox();
    }

    public void ShowLanguageBox()
    {
        HideMenu();
        language_box.SetActive(true);
    }
    void OnSelectLanguage(UnityEngine.EventSystems.PointerEventData e)
    {
        MultiLanguage.SetLanguage(e.pointerPress.name);
        language_box.SetActive(false);
    }

    void OnMenuPath()
    {
        HideMenu();
        ShowPathInputDialog();
    }

    void ShowPathInputDialog()
    {
        if (pathDialogObj_ != null) return;

        // 시작 경로 결정
#if UNITY_ANDROID && !UNITY_EDITOR
        string startPath = "/storage/emulated/0";
        try
        {
            using (var envClass = new AndroidJavaClass("android.os.Environment"))
            using (var dir = envClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
            {
                startPath = dir.Call<string>("getAbsolutePath");
            }
        }
        catch (System.Exception) { }
#else
        string startPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
#endif

        Font uiFont = null;
        if (menu_1_path != null)
        {
            var textComp = menu_1_path.GetComponentInChildren<Text>(true);
            if (textComp != null) uiFont = textComp.font;
        }
        if (uiFont == null && msg_title != null) uiFont = msg_title.font;
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 배경 오버레이
        pathDialogObj_ = new GameObject("PathDialog");
        pathDialogObj_.transform.SetParent(transform, false);
        var bgRt = pathDialogObj_.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImage = pathDialogObj_.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.85f);

        // 상단 현재 경로 표시
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(pathDialogObj_.transform, false);
        var headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.02f, 0.88f);
        headerRt.anchorMax = new Vector2(0.98f, 0.98f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        var headerImg = headerObj.AddComponent<Image>();
        headerImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        var pathTextObj = new GameObject("PathText");
        pathTextObj.transform.SetParent(headerObj.transform, false);
        var pathTextRt = pathTextObj.AddComponent<RectTransform>();
        pathTextRt.anchorMin = Vector2.zero;
        pathTextRt.anchorMax = Vector2.one;
        pathTextRt.offsetMin = Vector2.zero;
        pathTextRt.offsetMax = Vector2.zero;
        
        pathLabel_ = pathTextObj.AddComponent<Text>();
        pathLabel_.font = uiFont;
        pathLabel_.fontSize = 20;
        pathLabel_.color = Color.white;
        pathLabel_.alignment = TextAnchor.MiddleLeft;
        pathLabel_.resizeTextForBestFit = true;
        pathLabel_.resizeTextMinSize = 12;
        pathLabel_.resizeTextMaxSize = 20;
        pathLabel_.text = "  " + startPath;

        // 스크롤 영역
        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(pathDialogObj_.transform, false);
        var scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.15f);
        scrollRt.anchorMax = new Vector2(0.98f, 0.87f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        scrollObj.AddComponent<Mask>().showMaskGraphic = true;
        folderScrollRect_ = scrollObj.AddComponent<ScrollRect>();
        folderScrollRect_.horizontal = false;
        folderScrollRect_.movementType = ScrollRect.MovementType.Clamped;

        // Content 오브젝트
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        var contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        folderContent_ = contentRt;
        folderScrollRect_.content = contentRt;

        // 하단 버튼들
        // 선택 버튼
        var selectObj = new GameObject("SelectButton");
        selectObj.transform.SetParent(pathDialogObj_.transform, false);
        var selectRt = selectObj.AddComponent<RectTransform>();
        selectRt.anchorMin = new Vector2(0.55f, 0.03f);
        selectRt.anchorMax = new Vector2(0.98f, 0.12f);
        selectRt.offsetMin = Vector2.zero;
        selectRt.offsetMax = Vector2.zero;
        var selectImg = selectObj.AddComponent<Image>();
        selectImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);
        var selectBtn = selectObj.AddComponent<Button>();
        var selectTextObj = new GameObject("Text");
        selectTextObj.transform.SetParent(selectObj.transform, false);
        var selectTextRt = selectTextObj.AddComponent<RectTransform>();
        selectTextRt.anchorMin = Vector2.zero;
        selectTextRt.anchorMax = Vector2.one;
        selectTextRt.offsetMin = Vector2.zero;
        selectTextRt.offsetMax = Vector2.zero;
        var selectText = selectTextObj.AddComponent<Text>();
        selectText.text = MultiLanguage.GetText("[PathSelect]");
        selectText.font = uiFont;
        selectText.fontSize = 22;
        selectText.color = Color.white;
        selectText.alignment = TextAnchor.MiddleCenter;
        selectText.resizeTextForBestFit = true;
        selectText.resizeTextMinSize = 14;
        selectText.resizeTextMaxSize = 22;

        selectBtn.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("custom_era_path_full", currentBrowsePath_);
            PlayerPrefs.Save();
            ShowMessageBox(MultiLanguage.GetText("[BackMenu]"), MultiLanguage.GetText("[PathSaved]"));
            GameObject.Destroy(pathDialogObj_);
            pathDialogObj_ = null;
            pathLabel_ = null;
            folderContent_ = null;
            folderScrollRect_ = null;
        });

        // 초기화 버튼
        var resetObj = new GameObject("ResetButton");
        resetObj.transform.SetParent(pathDialogObj_.transform, false);
        var resetRt = resetObj.AddComponent<RectTransform>();
        resetRt.anchorMin = new Vector2(0.28f, 0.03f);
        resetRt.anchorMax = new Vector2(0.52f, 0.12f);
        resetRt.offsetMin = Vector2.zero;
        resetRt.offsetMax = Vector2.zero;
        var resetImg = resetObj.AddComponent<Image>();
        resetImg.color = new Color(0.6f, 0.4f, 0.2f, 1f);
        var resetBtn = resetObj.AddComponent<Button>();
        var resetTextObj = new GameObject("Text");
        resetTextObj.transform.SetParent(resetObj.transform, false);
        var resetTextRt = resetTextObj.AddComponent<RectTransform>();
        resetTextRt.anchorMin = Vector2.zero;
        resetTextRt.anchorMax = Vector2.one;
        resetTextRt.offsetMin = Vector2.zero;
        resetTextRt.offsetMax = Vector2.zero;
        var resetText = resetTextObj.AddComponent<Text>();
        resetText.text = MultiLanguage.GetText("[PathReset]");
        resetText.font = uiFont;
        resetText.fontSize = 18;
        resetText.color = Color.white;
        resetText.alignment = TextAnchor.MiddleCenter;
        resetText.resizeTextForBestFit = true;
        resetText.resizeTextMinSize = 12;
        resetText.resizeTextMaxSize = 18;

        resetBtn.onClick.AddListener(() =>
        {
            PlayerPrefs.DeleteKey("custom_era_path_full");
            PlayerPrefs.SetString("custom_era_path", "emuera");
            PlayerPrefs.Save();
            ShowMessageBox(MultiLanguage.GetText("[BackMenu]"), MultiLanguage.GetText("[PathResetMsg]"));
            GameObject.Destroy(pathDialogObj_);
            pathDialogObj_ = null;
            pathLabel_ = null;
            folderContent_ = null;
            folderScrollRect_ = null;
        });

        // 취소 버튼
        var cancelObj = new GameObject("CancelButton");
        cancelObj.transform.SetParent(pathDialogObj_.transform, false);
        var cancelRt = cancelObj.AddComponent<RectTransform>();
        cancelRt.anchorMin = new Vector2(0.02f, 0.03f);
        cancelRt.anchorMax = new Vector2(0.25f, 0.12f);
        cancelRt.offsetMin = Vector2.zero;
        cancelRt.offsetMax = Vector2.zero;
        var cancelImg = cancelObj.AddComponent<Image>();
        cancelImg.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        var cancelBtn = cancelObj.AddComponent<Button>();
        var cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelObj.transform, false);
        var cancelTextRt = cancelTextObj.AddComponent<RectTransform>();
        cancelTextRt.anchorMin = Vector2.zero;
        cancelTextRt.anchorMax = Vector2.one;
        cancelTextRt.offsetMin = Vector2.zero;
        cancelTextRt.offsetMax = Vector2.zero;
        var cancelText = cancelTextObj.AddComponent<Text>();
        cancelText.text = MultiLanguage.GetText("[PathCancel]");
        cancelText.font = uiFont;
        cancelText.fontSize = 18;
        cancelText.color = Color.white;
        cancelText.alignment = TextAnchor.MiddleCenter;
        cancelText.resizeTextForBestFit = true;
        cancelText.resizeTextMinSize = 12;
        cancelText.resizeTextMaxSize = 18;

        cancelBtn.onClick.AddListener(() =>
        {
            GameObject.Destroy(pathDialogObj_);
            pathDialogObj_ = null;
            pathLabel_ = null;
            folderContent_ = null;
            folderScrollRect_ = null;
        });

        // 폴더 목록 표시
        currentBrowsePath_ = startPath;
        RefreshFolderList();
    }

    void RefreshFolderList()
    {
        if (folderContent_ == null) return;

        // 기존 항목들 제거
        for (int i = folderContent_.childCount - 1; i >= 0; i--)
            GameObject.Destroy(folderContent_.GetChild(i).gameObject);

        pathLabel_.text = "  " + currentBrowsePath_;

        float itemHeight = 70f;
        int index = 0;

        // 상위 폴더로 이동 버튼
        var parentDir = System.IO.Directory.GetParent(currentBrowsePath_);
        if (parentDir != null)
        {
            CreateFolderItem("📁 ..", itemHeight, index, () =>
            {
                currentBrowsePath_ = parentDir.FullName;
                RefreshFolderList();
            });
            index++;
        }

        // 하위 폴더 나열
        try
        {
            var dirs = System.IO.Directory.GetDirectories(currentBrowsePath_);
            System.Array.Sort(dirs);
            foreach (var dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);
                string fullPath = dir;
                CreateFolderItem("📂 " + dirName, itemHeight, index, () =>
                {
                    currentBrowsePath_ = fullPath;
                    RefreshFolderList();
                });
                index++;
            }
        }
        catch (System.Exception) { }

        // Content 높이 업데이트
        float totalHeight = itemHeight * index;
        folderContent_.sizeDelta = new Vector2(folderContent_.sizeDelta.x, totalHeight);
        folderScrollRect_.normalizedPosition = new Vector2(0, 1); // 맨 위로 스크롤
    }

    void CreateFolderItem(string label, float itemHeight, int index, System.Action onClick)
    {
        var itemObj = new GameObject("FolderItem_" + index);
        itemObj.transform.SetParent(folderContent_, false);
        var itemRt = itemObj.AddComponent<RectTransform>();
        itemRt.anchorMin = new Vector2(0, 1);
        itemRt.anchorMax = new Vector2(1, 1);
        itemRt.pivot = new Vector2(0.5f, 1);
        itemRt.sizeDelta = new Vector2(0, itemHeight);
        itemRt.anchoredPosition = new Vector2(0, -itemHeight * index);

        var itemImg = itemObj.AddComponent<Image>();
        itemImg.color = (index % 2 == 0) ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.16f, 1f);

        var btn = itemObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.4f, 0.6f, 1f);
        colors.pressedColor = new Color(0.2f, 0.3f, 0.5f, 1f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(itemObj.transform, false);
        var textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.03f, 0f);
        textRt.anchorMax = new Vector2(0.97f, 1f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = msg_title != null ? msg_title.font : null;
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = 26;

        btn.onClick.AddListener(() => onClick());
    }

    void OnGithub()
    {
        Application.OpenURL("https://github.com/xerysherry/uEmuera/releases");
    }

    void OnIntentBoxShow()
    {
        intentbox.SetActive(true);
        HideMenu();
    }
    /// <summary>
    /// 余白の調節を4方向へ広げる。
    ///
    /// プリファブには左右の2組しか無いので、上下の組はその複製で作る。
    /// 手で6個の参照を繋ぎ直すより取り違えが起きにくい。
    ///
    /// 並びは左上=左, 右上=右, 左下=上, 右下=下。
    /// 矢印は「境界が動く向き」に合わせる。左と上は矢印の向き＝余白が増える向きだが、
    /// 右と下は逆になる
    /// </summary>
    void BuildIntentBox()
    {
        if(intent_texts_ != null)
            return;
        intent_texts_ = new Text[4];
        if(intentbox_L_left == null || intentbox_L_right == null || intentbox_L_text == null)
            return;

        intent_texts_[0] = intentbox_L_text;
        intent_texts_[1] = intentbox_R_text;

        //枠を広げ、見出しの分の間も取って配置し直す
        var border = intentbox_L_left.transform.parent as RectTransform;
        if(border != null)
            border.sizeDelta = new Vector2(border.sizeDelta.x, 340f);
        const float kLabel1 = 95f;
        const float kRow1 = 55f;
        const float kLabel2 = 10f;
        const float kRow2 = -30f;
        const float kButtonY = -100f;
        const float kLeftX = -95f;
        const float kRightX = 95f;

        MoveIntentRow(intentbox_L_left, intentbox_L_text, intentbox_L_right, kLeftX, kRow1);
        MoveIntentRow(intentbox_R_left, intentbox_R_text, intentbox_R_right, kRightX, kRow1);
        SetLocalY(intentbox_reset, kButtonY);
        SetLocalY(intentbox_close, kButtonY);

        //上下の組を作る
        intent_texts_[2] = CloneIntentRow(2, "T", kLeftX, kRow2);
        intent_texts_[3] = CloneIntentRow(3, "B", kRightX, kRow2);

        //どのつまみがどこを動かすのか、見出しが無いと分からない
        MakeIntentLabel("LabelL", Localized("[IntentL]", "Left"), kLeftX, kLabel1);
        MakeIntentLabel("LabelR", Localized("[IntentR]", "Right"), kRightX, kLabel1);
        MakeIntentLabel("LabelT", Localized("[IntentT]", "Top"), kLeftX, kLabel2);
        MakeIntentLabel("LabelB", Localized("[IntentB]", "Bottom"), kRightX, kLabel2);

        //左と上は矢印の向きへ余白が増え、右と下は逆
        GenericUtils.SetListenerOnClick(intentbox_L_left, () => ChangeIntent(0, -1));
        GenericUtils.SetListenerOnClick(intentbox_L_right, () => ChangeIntent(0, +1));
        GenericUtils.SetListenerOnClick(intentbox_R_left, () => ChangeIntent(1, +1));
        GenericUtils.SetListenerOnClick(intentbox_R_right, () => ChangeIntent(1, -1));

        for(int i = 0; i < 4; ++i)
        {
            if(intent_texts_[i] != null)
                intent_texts_[i].text = PlayerPrefs.GetInt(kIntentKeys[i], 0).ToString();
        }
    }

    /// <summary>
    /// 言語ファイルに項目が無ければ既定の綴りを使う。
    /// GetTextは見つからないと鍵をそのまま返すので、そのままでは
    /// 画面に「[IntentL]」と出てしまう
    /// </summary>
    static string Localized(string key, string fallback)
    {
        try
        {
            var v = MultiLanguage.GetText(key);
            if(!string.IsNullOrEmpty(v) && v != key)
                return v;
        }
        catch(System.Exception)
        { }
        return fallback;
    }

    void MakeIntentLabel(string name, string caption, float x, float y)
    {
        if(intentbox_L_text == null)
            return;
        var go = GameObject.Instantiate(intentbox_L_text.gameObject,
                                        intentbox_L_text.transform.parent);
        go.name = name;
        go.transform.localScale = Vector3.one;
        var rt = go.transform as RectTransform;
        //数字は引き伸ばしアンカーで親の大きさに依存している。
        //見出しは中央固定にして、枠の高さを変えても位置が動かないようにする
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 32f);
        rt.anchoredPosition = new Vector2(x, y);
        var t = go.GetComponent<Text>();
        if(t != null)
        {
            t.text = caption;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        go.SetActive(true);
    }

    static void ClearClickListeners(GameObject go)
    {
        if(go == null)
            return;
        var l = go.GetComponent<GenericUtils.PointerClickListener>();
        if(l != null)
        {
            l.callbacks1.Clear();
            l.callbacks2.Clear();
        }
    }

    static void SetLocalY(GameObject go, float y)
    {
        if(go == null)
            return;
        var rt = go.transform as RectTransform;
        if(rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    static void MoveIntentRow(GameObject minus, Text label, GameObject plus, float cx, float y)
    {
        if(minus != null)
            (minus.transform as RectTransform).anchoredPosition = new Vector2(cx - 45f, y);
        if(plus != null)
            (plus.transform as RectTransform).anchoredPosition = new Vector2(cx + 45f, y);
        if(label != null)
            (label.transform as RectTransform).anchoredPosition = new Vector2(cx, y);
    }

    Text CloneIntentRow(int index, string tag, float cx, float y)
    {
        var minus = GameObject.Instantiate(intentbox_L_left, intentbox_L_left.transform.parent);
        var plus = GameObject.Instantiate(intentbox_L_right, intentbox_L_right.transform.parent);
        var label = GameObject.Instantiate(intentbox_L_text.gameObject,
                                            intentbox_L_text.transform.parent);
        minus.name = tag + "_left";
        plus.name = tag + "_right";
        label.name = tag;
        minus.transform.localScale = Vector3.one;
        plus.transform.localScale = Vector3.one;
        label.transform.localScale = Vector3.one;
        //複製元の反応が付いて回らないように念のため消しておく
        ClearClickListeners(minus);
        ClearClickListeners(plus);

        var text = label.GetComponent<Text>();
        MoveIntentRow(minus, text, plus, cx, y);

        //上は矢印の向きへ余白が増え、下は逆
        int inc = (index == 2) ? +1 : -1;
        GenericUtils.SetListenerOnClick(minus, () => ChangeIntent(index, inc));
        GenericUtils.SetListenerOnClick(plus, () => ChangeIntent(index, -inc));
        minus.SetActive(true);
        plus.SetActive(true);
        label.SetActive(true);
        return text;
    }
    Text[] intent_texts_ = null;

    //上下左右の4方向を同じ手順で扱う。
    //方向ごとに関数を書くと、符号の付け方を間違えても気付きにくい
    static readonly string[] kIntentKeys = { "IntentBox_L", "IntentBox_R", "IntentBox_T", "IntentBox_B" };

    void ApplyIntentBox()
    {
        EmueraContent.instance.SetIntentBox(
            PlayerPrefs.GetInt(kIntentKeys[0], 0),
            PlayerPrefs.GetInt(kIntentKeys[1], 0),
            PlayerPrefs.GetInt(kIntentKeys[2], 0),
            PlayerPrefs.GetInt(kIntentKeys[3], 0));
    }

    /// <summary>
    /// 余白を増減する。deltaは余白そのものの増減で、矢印の向きとは別。
    /// 矢印は「境界が動く向き」に合わせてあるので、
    /// 右・下側は矢印と余白の増減が逆になる
    /// </summary>
    void ChangeIntent(int index, int delta)
    {
        var key = kIntentKeys[index];
        int value = PlayerPrefs.GetInt(key, 0) + delta;
        if(value < 0)
            value = 0;
        else if(value > 99)
            value = 99;
        PlayerPrefs.SetInt(key, value);
        if(intent_texts_[index] != null)
            intent_texts_[index].text = value.ToString();
        ApplyIntentBox();
    }

    void OnIntentClose()
    {
        intentbox.SetActive(false);
    }
    void OnIntentReset()
    {
        for(int i = 0; i < kIntentKeys.Length; ++i)
        {
            PlayerPrefs.SetInt(kIntentKeys[i], 0);
            if(intent_texts_ != null && intent_texts_[i] != null)
                intent_texts_[i].text = "0";
        }
        ApplyIntentBox();
    }

    public GameObject game_button;
    public Button quick_button;
    public Button input_button;
    public Button magnifier_button;
    public Button option_button;

    public Button orientation_lock_button;
    Image orientation_lock_image;
    public Sprite lock_sprite;
    public Sprite unlock_sprite;

    public GameObject inprogress;
    List<Shadow> button_shadows;

    public QuickButtons quick_buttons;
    public Inputpad input_pad;
    public Scalepad scale_pad;

    public GameObject msg_box;
    public Text msg_title;
    public Text msg_content;
    public GameObject msg_confirm;
    public GameObject msg_cancel;
    System.Action msg_confirm_callback;
    System.Action msg_cancel_callback;

    public GameObject menu_pad;
    public GameObject menu_1;
    public GameObject menu_1_resolution;
    public GameObject menu_1_language;
    public GameObject menu_1_path;
    public GameObject menu_1_github;
    public GameObject menu_1_exit;
    GameObject pathDialogObj_ = null;

    public GameObject menu_2;
    public GameObject menu_2_back;
    public GameObject menu_2_restart;
    public GameObject menu_2_gototitle;
    public GameObject menu_2_savelog;
    public GameObject menu_2_intent;
    public GameObject menu_2_exit;

    public GameObject resolution_pad;
    public GameObject resolution_1080p;
    public GameObject resolution_1080p_icon;
    public GameObject resolution_900p;
    public GameObject resolution_900p_icon;
    public GameObject resolution_720p;
    public GameObject resolution_720p_icon;
    public GameObject resolution_540p;
    public GameObject resolution_540p_icon;

    public GameObject language_box;
    public GameObject language_zhcn;
    public GameObject language_jp;
    public GameObject language_enus;
    public GameObject language_ko;

    public GameObject intentbox;
    public GameObject intentbox_L_left;
    public GameObject intentbox_L_right;
    public GameObject intentbox_R_left;
    public GameObject intentbox_R_right;
    public GameObject intentbox_close;
    public GameObject intentbox_reset;
    public Text intentbox_L_text;
    public Text intentbox_R_text;

    Text pathLabel_ = null;
    RectTransform folderContent_ = null;
    ScrollRect folderScrollRect_ = null;
    string currentBrowsePath_ = "";

    bool auto_rotation
    {
        get
        {
            return Screen.autorotateToLandscapeLeft &&
                    Screen.autorotateToLandscapeRight &&
                    Screen.autorotateToPortrait &&
                    Screen.autorotateToPortraitUpsideDown;
        }
    }
}
