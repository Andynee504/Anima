using UnityEngine;

public class AppMenuActions : MonoBehaviour
{
    [SerializeField] UIScreenRouter router;

    public void StartGame() { router.Resume(); }          // fecha menus e volta ao jogo
    public void OpenSettings() { router.GoSettings(); }
    public void OpenMain() { router.GoMain(); }
    public void OpenPause() { router.GoPause(); }

    public void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // no Quest, volta ao Home
#endif
    }
}
