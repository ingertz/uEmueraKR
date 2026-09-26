using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift._Library;
using System.Text;

public class MainEntry : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 24;
        //画面に変化が無い間は描画を間引く
        RenderThrottle.Create();
        ResolutionHelper.Apply();
    }

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RequestPermissionsAndInit());
#else
        InitApp();
#endif
    }

    void InitApp()
    {
        LoadConfigMaps();
        if(!MultiLanguage.SetLanguage())
        {
            Object.FindObjectOfType<OptionWindow>().ShowLanguageBox();
        }

        // Initialize SoundManager
        var soundManagerGO = new GameObject("SoundManager");
        soundManagerGO.AddComponent<uEmuera.SoundManager>();

#if UNITY_EDITOR
        uEmuera.Logger.info = GenericUtils.Info;
        uEmuera.Logger.warn = GenericUtils.Warn;
        uEmuera.Logger.error = GenericUtils.Error;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    System.Collections.IEnumerator RequestPermissionsAndInit()
    {
        // Android API 레벨 확인
        int apiLevel = GetApiLevel();

        // Android 13+ (API 33): READ_MEDIA_* 권한 요청
        if (apiLevel >= 33)
        {
            string[] mediaPermissions = new string[] {
                "android.permission.READ_MEDIA_IMAGES",
                "android.permission.READ_MEDIA_AUDIO"
            };
            RequestNativePermissions(mediaPermissions);
            yield return new WaitForSeconds(1f);
        }
        // Android 12 이하: 기존 저장소 권한 요청
        else
        {
            string[] storagePermissions = new string[] {
                "android.permission.READ_EXTERNAL_STORAGE",
                "android.permission.WRITE_EXTERNAL_STORAGE"
            };
            RequestNativePermissions(storagePermissions);
            yield return new WaitForSeconds(1f);
        }

        // Android 11+ (API 30): 모든 파일 접근 권한 요청
        if (apiLevel >= 30 && CheckNeedsAllFilesAccess())
        {
            OpenAllFilesAccessSettings();
            yield return new WaitForSeconds(1f);
            while (CheckNeedsAllFilesAccess())
            {
                yield return new WaitForSeconds(1f);
            }
        }

        InitApp();
    }

    int GetApiLevel()
    {
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
        catch (System.Exception)
        {
            return 28;
        }
    }

    void RequestNativePermissions(string[] permissions)
    {
        try
        {
            using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call("requestPermissions", permissions, 0);
            }
        }
        catch (System.Exception) { }
    }

    bool CheckNeedsAllFilesAccess()
    {
        try
        {
            using (var envClass = new AndroidJavaClass("android.os.Environment"))
            {
                return !envClass.CallStatic<bool>("isExternalStorageManager");
            }
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    void OpenAllFilesAccessSettings()
    {
        try
        {
            using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // 이 앱 전용 "모든 파일 접근" 설정 페이지로 바로 이동
                string packageName = activity.Call<string>("getPackageName");
                using (var uri = new AndroidJavaClass("android.net.Uri")
                    .CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null))
                using (var intent = new AndroidJavaObject("android.content.Intent",
                    "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION", uri))
                {
                    activity.Call("startActivity", intent);
                }
            }
        }
        catch (System.Exception)
        {
            // 폴백: 일반 모든 파일 접근 목록 페이지
            try
            {
                using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = new AndroidJavaObject("android.content.Intent",
                    "android.settings.MANAGE_ALL_FILES_ACCESS_PERMISSION"))
                {
                    activity.Call("startActivity", intent);
                }
            }
            catch (System.Exception) { }
        }
    }
#endif

#if UNITY_EDITOR
    public string era_path;
#endif

    void LoadConfigMaps()
    {
        char[] split = new char[] { '\x0d', '\x0a' };
        var shiftjis = Resources.Load<TextAsset>("Text/emuera_config_shiftjis");
        if(shiftjis == null)
            return;
        var utf8 = Resources.Load<TextAsset>("Text/emuera_config_utf8");
        if(utf8 == null)
            return;
        var utf8_cn = Resources.Load<TextAsset>("Text/emuera_config_utf8_zhcn");
        if(utf8_cn == null)
            return;

        //var jis_text = System.Text.Encoding.UTF8.GetString(shiftjis.bytes);
        //var jis_strs = jis_text.Split(split);

        var jis_bytes = shiftjis.bytes;
        var jis_md5_strs = GenericUtils.CalcMd5List(jis_bytes);

        var utf8_strs = utf8.text.Split(split);
        var utf8_str_list = new List<string>();
        foreach (var str in utf8_strs)
        {
            if (string.IsNullOrWhiteSpace(str))
                continue;
            utf8_str_list.Add(str);
        }

        var utf8cn_strs = utf8_cn.text.Split(split);
        var utf8cn_str_list = new List<string>();
        foreach (var str in utf8cn_strs)
        {
            if (string.IsNullOrWhiteSpace(str))
                continue;
            utf8cn_str_list.Add(str);
        }

        if (jis_md5_strs.Count != utf8cn_str_list.Count)
            return;

        Dictionary<string, string> jis_map = new Dictionary<string, string>();
        for(int i = 0; i < jis_md5_strs.Count; ++i)
        {
            jis_map[jis_md5_strs[i]] = utf8_str_list[i];
        }
        Dictionary<string, string> utf8cn_map = new Dictionary<string, string>();
        for(int i = 0; i < utf8cn_str_list.Count; ++i)
        {
            utf8cn_map[utf8cn_str_list[i]] = utf8_str_list[i];
        }
        uEmuera.Utils.SetSHIFTJIS_to_UTF8Dict(jis_map);
        uEmuera.Utils.SetUTF8ZHCN_to_UTF8Dict(utf8cn_map);
    }
}
