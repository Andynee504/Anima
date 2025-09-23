/*
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PianoKeyboardImporter : EditorWindow
{
    [MenuItem("Tools/Piano/Preencher Teclas do JSON")]
    public static void FillFromJson()
    {
        string path = "Assets/_Project/Audio/Piano/mapping.json";
        if (!File.Exists(path))
        {
            Debug.LogError("mapping.json não encontrado em " + path);
            return;
        }

        var json = File.ReadAllText(path);
        var entries = JsonUtility.FromJson<Wrapper>(FixJson(json)).items;

        var piano = FindObjectOfType<PianoKeyboard>();
        if (!piano)
        {
            Debug.LogError("Nenhum PianoKeyboard na cena.");
            return;
        }

        var keys = new List<PianoKeyboard.KeyBinding>();
        foreach (var e in entries)
        {
            var kb = new PianoKeyboard.KeyBinding
            {
                keyName = e.note,
                key = null,
                attackClip = LoadClip(e.files.attack),
                sustainLoopClip = LoadClip(e.files.loop),
                releaseClip = LoadClip(e.files.release),
                clip = null,
                pitch = 1f,
                volumeOverride = 0f
            };
            keys.Add(kb);
        }

        Undo.RecordObject(piano, "Auto-preencher PianoKeyboard");
        piano.keys = keys.ToArray();
        EditorUtility.SetDirty(piano);
        Debug.Log("Teclas do teclado preenchidas a partir do JSON.");
    }

    static AudioClip LoadClip(string filename)
    {
        string assetPath = "Assets/_Project/Audio/Piano/" + filename;
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    // Helpers para ler array com JsonUtility
    [System.Serializable]
    class Wrapper { public Entry[] items; }
    [System.Serializable]
    class Entry { public string note; public FileSet files; }
    [System.Serializable]
    class FileSet { public string attack, loop, release; }

    static string FixJson(string json) =>
        "{\"items\":" + json + "}";
}
*/