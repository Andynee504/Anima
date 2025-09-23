using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

public static class PianoKeyboardImporter
{
    [MenuItem("Tools/Piano/Preencher Teclas do JSON (auto-link)")]
    public static void FillFromJson()
    {
        string jsonPath = "Assets/_Project/Audio/Piano/mapping.json";
        if (string.IsNullOrEmpty(jsonPath))
        {
            Debug.LogError("mapping.json não encontrado no projeto.");
            return;
        }

        var jsonText = File.ReadAllText(jsonPath);
        var entries = JsonUtility.FromJson<Wrapper>(FixJson(jsonText)).items;
        if (entries == null || entries.Length == 0)
        {
            Debug.LogError("mapping.json lido, mas sem itens.");
            return;
        }

        var piano = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<PianoKeyboard>() : null;
        if (!piano) piano = Object.FindObjectOfType<PianoKeyboard>();
        if (!piano)
        {
            Debug.LogError("Nenhum PianoKeyboard encontrado na cena.");
            return;
        }

        var oldKeys = piano.keys ?? new PianoKeyboard.KeyBinding[0];
        var oldByName = new Dictionary<string, PianoKeyboard.KeyBinding>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var k in oldKeys)
        {
            if (!string.IsNullOrEmpty(k.keyName) && !oldByName.ContainsKey(k.keyName))
                oldByName[k.keyName] = k;
        }

        var newList = new List<PianoKeyboard.KeyBinding>(entries.Length);

        for (int idx = 0; idx < entries.Length; idx++)
        {
            var e = entries[idx];

            // tenta preservar pelos nomes; se nao existir, tenta pelo índice
            PianoKeyboard.KeyBinding old =
                (oldByName.TryGetValue(e.note, out var byName)) ? byName :
                (idx < oldKeys.Length ? oldKeys[idx] : new PianoKeyboard.KeyBinding());

            var kb = new PianoKeyboard.KeyBinding
            {
                // mantem o nome existente, senao usa o do JSON
                keyName = string.IsNullOrEmpty(old.keyName) ? e.note : old.keyName,

                // PRESERVA o XRBaseInteractable ja setado:
                key = old.key,

                // popula os 3 clips a partir do JSON
                attackClip = FindClipByFileName(e.files.attack),
                sustainLoopClip = FindClipByFileName(e.files.loop),
                releaseClip = FindClipByFileName(e.files.release),

                // nao usar o 'clip' legado quando ALR existem
                clip = null,

                // preserva ajustes do usuario quando existentes
                pitch = (old.pitch > 0f ? old.pitch : 1f),
                volumeOverride = old.volumeOverride
            };

            newList.Add(kb);
        }

        Undo.RecordObject(piano, "Preencher PianoKeyboard do JSON");
        piano.keys = newList.ToArray();
        EditorUtility.SetDirty(piano);

        int okKey = piano.keys.Count(k => k.key);
        int okAtk = piano.keys.Count(k => k.attackClip);
        int okLp = piano.keys.Count(k => k.sustainLoopClip);
        int okRel = piano.keys.Count(k => k.releaseClip);
        Debug.Log($"PianoKeyboard preenchido. Teclas preservadas: {okKey}/{piano.keys.Length}. Clips: attack {okAtk}, loop {okLp}, release {okRel}.");
    }

    // ---- utilitários ----
    static AudioClip FindClipByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string nameNoExt = Path.GetFileNameWithoutExtension(fileName);

        var guids = AssetDatabase.FindAssets($"t:AudioClip {nameNoExt}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(nameNoExt, System.StringComparison.OrdinalIgnoreCase))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip) return clip;
            }
        }
        return null;
    }

    static string FindAssetPathByName(string fileName, string typeName)
    {
        string nameNoExt = Path.GetFileName(fileName);
        string[] guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(nameNoExt)} t:{typeName}");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path).Equals(nameNoExt, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }
        return null;
    }

    // JsonUtility helpers
    [System.Serializable] class Wrapper { public Entry[] items; }
    [System.Serializable] class Entry { public int index; public string note; public int midi; public float frequency_hz; public Files files; }
    [System.Serializable] class Files { public string attack; public string loop; public string release; }
    static string FixJson(string json) => json.TrimStart().StartsWith("[") ? "{\"items\":" + json + "}" : json;
}
