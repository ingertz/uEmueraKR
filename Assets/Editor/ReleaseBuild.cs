using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 配布用のAPKをコマンドラインから作る。
///   Unity -batchmode -projectPath . -buildTarget Android -executeMethod ReleaseBuild.Android
/// Build/Release/ に uEmuera.arm64-v8a.apk / uEmuera.armeabi-v7a.apk と
/// uEmuera.apk(全CPU)を出す。Build/Release/ は毎回作り直す。
///
/// 署名鍵のパスワードはプロジェクトに保存されないので環境変数から読む
///   UEMUERA_KEYSTORE_PASS  user.keystoreのパスワード
///   UEMUERA_KEYALIAS_PASS  鍵(uemuera)のパスワード。無ければ上と同じ物を使う
/// </summary>
public static class ReleaseBuild
{
    const string kOutDir = "Build/Release";
    const string kPerCpuDir = kOutDir + "/per_cpu";
    const string kApkName = "uEmuera.apk";

    public static void Android()
    {
        int code = 1;
        try
        {
            if(Run())
                code = 0;
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
        EditorApplication.Exit(code);
    }

    static bool Run()
    {
        var pass = GetEnv("UEMUERA_KEYSTORE_PASS");
        if(string.IsNullOrEmpty(pass))
        {
            Debug.LogError("ReleaseBuild: UEMUERA_KEYSTORE_PASS is not set");
            return false;
        }
        var alias_pass = GetEnv("UEMUERA_KEYALIAS_PASS");
        if(string.IsNullOrEmpty(alias_pass))
            alias_pass = pass;
        PlayerSettings.Android.keystorePass = pass;
        PlayerSettings.Android.keyaliasPass = alias_pass;
        EditorUserBuildSettings.buildAppBundle = false;

        if(Directory.Exists(kOutDir))
            Directory.Delete(kOutDir, true);
        Directory.CreateDirectory(kOutDir);
        var per_cpu = PlayerSettings.Android.buildApkPerCpuArchitecture;
        try
        {
            if(!Build(true) || !Build(false))
                return false;
            //CPU別のビルドは指定した名前のフォルダを作って、その中に
            //uEmuera.arm64-v8a.apk などを置く。全CPU版の隣へ出しておく
            foreach(var apk in Directory.GetFiles(kPerCpuDir, "*.apk", SearchOption.AllDirectories))
                File.Move(apk, kOutDir + "/" + Path.GetFileName(apk));
            Directory.Delete(kPerCpuDir, true);
            return true;
        }
        finally
        {
            PlayerSettings.Android.buildApkPerCpuArchitecture = per_cpu;
        }
    }

    static bool Build(bool per_cpu)
    {
        PlayerSettings.Android.buildApkPerCpuArchitecture = per_cpu;
        var options = new BuildPlayerOptions
        {
            //Build Settingsのシーン一覧は空(開いているシーンを使う設定)なので明示する
            scenes = new[] { "Assets/Main.unity" },
            locationPathName = (per_cpu ? kPerCpuDir : kOutDir) + "/" + kApkName,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            //エディタの Build Profiles (Android) と同じLZ4
            options = BuildOptions.CompressWithLz4,
        };
        var summary = BuildPipeline.BuildPlayer(options).summary;
        Debug.Log(string.Format("ReleaseBuild: {0} {1} per_cpu={2} result={3} errors={4}",
            PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode,
            per_cpu, summary.result, summary.totalErrors));
        return summary.result == BuildResult.Succeeded;
    }

    /// <summary>
    /// 起動済みのアプリから呼ばれると、後から設定したユーザー環境変数は
    /// プロセスに入っていないので、そちらも見る
    /// </summary>
    static string GetEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if(string.IsNullOrEmpty(value))
        {
            try { value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User); }
            catch(Exception) { value = null; }
        }
        return value;
    }
}
