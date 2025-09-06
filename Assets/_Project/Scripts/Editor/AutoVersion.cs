using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;

public class AutoVersion : IPreprocessBuildWithReport
{
    // false = APENAS manualmente pelo menu; true = incrementa a cada Build
    private static readonly bool AUTO_BUMP_ON_BUILD = false; // troque para true se quiser auto-bump

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (AUTO_BUMP_ON_BUILD)
            BumpPatch(); // padrao = PATCH
    }

    // PATCH (atalho principal)
    [MenuItem("Tools/Version/Bump patch (x.y.z -> x.y.z+1) %#&v")]
    public static void BumpPatch()
    {
        string v = PlayerSettings.bundleVersion;
        var p = v.Split('.');
        int major = (p.Length > 0 && int.TryParse(p[0], out var M)) ? M : 0;
        int minor = (p.Length > 1 && int.TryParse(p[1], out var m)) ? m : 0;
        int patch = (p.Length > 2 && int.TryParse(p[2], out var x)) ? x : 0;

        patch += 1;
        PlayerSettings.bundleVersion = $"{major}.{minor}.{patch}";
        PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode + 1, 1);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AutoVersion] bundleVersion = {PlayerSettings.bundleVersion} | android:versionCode = {PlayerSettings.Android.bundleVersionCode}");
    }

    // MINOR (opcional, fica disponível no menu)
    [MenuItem("Tools/Version/Bump minor (x.y -> x.(y+1)) %#&m")]
    public static void BumpMinor()
    {
        string v = PlayerSettings.bundleVersion;
        var parts = v.Split('.');
        int major = parts.Length > 0 && int.TryParse(parts[0], out var M) ? M : 0;
        int minor = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;

        minor += 1;
        string newV = (parts.Length >= 3) ? $"{major}.{minor}.0" : $"{major}.{minor}";
        PlayerSettings.bundleVersion = newV;
        PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode + 1, 1);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AutoVersion] bundleVersion = {newV} | android:versionCode = {PlayerSettings.Android.bundleVersionCode}");
    }
}
