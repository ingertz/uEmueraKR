using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinorShift._Library;

public class FirstWindow : MonoBehaviour
{
    public static void Show()
    {
        var obj = Resources.Load<GameObject>("Prefab/FirstWindow");
        obj = GameObject.Instantiate(obj);
        obj.name = "FirstWindow";
    }
    static System.Collections.IEnumerator Run(string workspace, string era)
    {
        var async = Resources.UnloadUnusedAssets();
        while(!async.isDone)
            yield return null;

        var ow = EmueraContent.instance.option_window;
        ow.gameObject.SetActive(true);
        ow.ShowGameButton(true);
        ow.ShowInProgress(true);
        yield return null;

        System.GC.Collect();
        SpriteManager.Init();

        Sys.SetWorkFolder(workspace);
        Sys.SetSourceFolder(era);
        //ゲームフォルダのfont/を先に見るため、フォルダ確定直後に走査しておく。
        //文字幅の計測はメインスレッドでしか出来ないのでここで済ませる
        uEmuera.FontProvider.Scan();
        uEmuera.Utils.ResourcePrepare();

        async = Resources.UnloadUnusedAssets();
        while(!async.isDone)
            yield return null;

        EmueraContent.instance.SetNoReady();
        var emuera = GameObject.FindObjectOfType<EmueraMain>();
        emuera.Run();
    }

    void Start()
    {
        if(!string.IsNullOrEmpty(MultiLanguage.FirstWindowTitlebar))
            titlebar.text = MultiLanguage.FirstWindowTitlebar;  

        scroll_rect_ = GenericUtils.FindChildByName<ScrollRect>(gameObject, "ScrollRect");
        item_ = GenericUtils.FindChildByName(gameObject, "Item", true);
        setting_ = GenericUtils.FindChildByName(gameObject, "optionbtn", true);
        GenericUtils.SetListenerOnClick(setting_, OnOptionClick);

        GenericUtils.FindChildByName<Text>(gameObject, "version")
            .text = Application.version + " ";

        GetList(Application.persistentDataPath);
        setting_.SetActive(true);

#if UNITY_EDITOR
        var main_entry = GameObject.FindObjectOfType<MainEntry>();
        if(!string.IsNullOrEmpty(main_entry.era_path))
            GetList(main_entry.era_path);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        // 폴더 브라우저에서 직접 선택한 전체 경로가 있으면 우선 사용
        string fullPath = PlayerPrefs.GetString("custom_era_path_full", "");
        if (!string.IsNullOrEmpty(fullPath))
        {
            GetList(fullPath);
        }
        else
        {
            // PlayerPrefs에서 사용자 지정 폴더명을 읽어옴 (기본값: emuera)
            string eraFolder = PlayerPrefs.GetString("custom_era_path", "emuera");
            try
            {
                using (var envClass = new AndroidJavaClass("android.os.Environment"))
                using (var externalStorageDir = envClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
                {
                    string extPath = externalStorageDir.Call<string>("getAbsolutePath");
                    GetList(extPath + "/" + eraFolder);
                }
            }
            catch (System.Exception)
            {
                GetList("/storage/emulated/0/" + eraFolder);
            }
            
            GetList("/storage/sdcard0/" + eraFolder);
            GetList("/storage/sdcard1/" + eraFolder);
        }
#endif
#if UNITY_STANDALONE && !UNITY_EDITOR
        GetList(Path.GetFullPath(Application.dataPath + "/.."));
#endif
    }

    void OnOptionClick()
    {
        var ow = EmueraContent.instance.option_window;
        ow.ShowMenu();
    }

    void AddItem(string folder, string workspace)
    {
        var rrt = item_.transform as UnityEngine.RectTransform;
        var obj = GameObject.Instantiate(item_);
        var text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "name");
        text.text = folder;
        text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "path");
        text.text = workspace + "/" + folder;

        GenericUtils.SetListenerOnClick(obj, () =>
        {
            scroll_rect_ = null;
            item_ = null;
            GameObject.Destroy(gameObject);
            //Start Game
            GenericUtils.StartCoroutine(Run(workspace, folder));
        });

        var rt = obj.transform as UnityEngine.RectTransform;
        var content = scroll_rect_.content;
        rt.SetParent(content);
        rt.localScale = Vector3.one;
        rt.anchorMax = rrt.anchorMax;
        rt.anchorMin = rrt.anchorMin;
        rt.offsetMax = rrt.offsetMax;
        rt.offsetMin = rrt.offsetMin;
        rt.sizeDelta = rrt.sizeDelta;
        rt.localPosition = new Vector2(0, -rt.sizeDelta.y * itemcount_);
        itemcount_ += 1;

        var ih = rt.sizeDelta.y * itemcount_;
        if(ih > content.sizeDelta.y)
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, ih);
        }
        obj.SetActive(true);
    }

    /// <summary>
    /// 本体が起動するのに要るのはerb/。
    /// フォルダ名の大小は配布物によってまちまち(erb/ERB/Erb)なので実体に合わせる
    /// </summary>
    static bool HasErb(string path)
    {
        return Directory.Exists(uEmuera.Utils.ResolvePath(path + "/erb"));
    }

    /// <summary>
    /// era一式が入っているフォルダを返す。見つからなければnull。
    ///
    /// eraメガテンP版のように、csv/erb/resourcesがDATA/の中へ纏めて
    /// 入っている配布物がある。その場合はDATA/の側を渡さないと起動しない
    /// </summary>
    static string FindGameDir(string path)
    {
        if(HasErb(path))
            return path;
        string[] subs;
        try
        {
            subs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }
        catch(IOException)
        {
            return null;
        }
        catch(System.UnauthorizedAccessException)
        {
            return null;
        }
        for(int i = 0; i < subs.Length; ++i)
        {
            var sub = uEmuera.Utils.NormalizePath(subs[i]);
            if(HasErb(sub))
                return sub;
        }
        return null;
    }

    void GetList(string workspace)
    {
        workspace = uEmuera.Utils.NormalizePath(workspace).TrimEnd('/');
        if(!Directory.Exists(workspace))
            return;
        try
        {
            // 먼저 workspace 폴더 자체가 게임 폴더인지 확인
            //ここでFindGameDirを使って一階層潜ってはいけない。
            //ゲームを並べているだけのフォルダを渡された時に、
            //最初の一本を「潜って見つけた」と解釈して打ち切ってしまう
            if(HasErb(workspace))
            {
                string parentDir = Path.GetDirectoryName(workspace);
                string folderName = Path.GetFileName(workspace);
                if(string.IsNullOrEmpty(parentDir)) parentDir = workspace;
                parentDir = uEmuera.Utils.NormalizePath(parentDir).TrimEnd('/');
                AddItem(folderName, parentDir);
                return; // 이미 게임 폴더를 찾았으면 하위 폴더 탐색 생략
            }

            var paths = Directory.GetDirectories(workspace, "*", SearchOption.TopDirectoryOnly);
            foreach(var p in paths)
            {
                var path = uEmuera.Utils.NormalizePath(p);
                //DATA/の中へ入っている物も拾う。表示名は「era○○/DATA」になる
                var dir = FindGameDir(path);
                if(dir != null)
                    AddItem(dir.Substring(workspace.Length + 1), workspace);
            }
        }
        catch(DirectoryNotFoundException)
        { }
    }

    public Text titlebar = null;
    ScrollRect scroll_rect_ = null;
    GameObject item_ = null;
    GameObject setting_ = null;
    int itemcount_ = 0;
}
