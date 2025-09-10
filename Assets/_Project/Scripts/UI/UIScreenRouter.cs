using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIScreenRouter : MonoBehaviour
{
    [System.Serializable]
    public class Screen
    {
        public string key; // "Main", "Pause", "Settings"
        public GameObject root; // painel raiz
        public bool pauseGame; // pausa Time.timeScale quando ativa?
    }

    [SerializeField] List<Screen> screens = new();
    [SerializeField] string defaultKey = "Main";  // abre no início
    [SerializeField] List<Behaviour> disableWhilePaused = new(); // opcional: locomotion/turn/teleport

    string currentKey = null;

    void Awake() => Show(defaultKey);

    public void Show(string key)
    {
        currentKey = key;
        foreach (var s in screens) if (s.root) s.root.SetActive(s.key == key);
        ApplyPauseFlag(key);
    }

    public void ShowNone()
    {
        // fecha todas as telas (volta pro jogo)
        currentKey = null;
        foreach (var s in screens) if (s.root) s.root.SetActive(false);
        ApplyPauseFlag(null);
    }

    void ApplyPauseFlag(string key)
    {
        var s = screens.FirstOrDefault(x => x.key == key);
        bool paused = (s != null && s.pauseGame);
        Time.timeScale = paused ? 0f : 1f;
        foreach (var b in disableWhilePaused) if (b) b.enabled = !paused; // ex.: Move/Turn/Teleport providers
    }

    // Helpers para botão
    public void GoMain() => Show("Main");
    public void GoPause() => Show("Pause");
    public void GoSettings() => Show("Settings");
    public void Resume() => ShowNone();
}
