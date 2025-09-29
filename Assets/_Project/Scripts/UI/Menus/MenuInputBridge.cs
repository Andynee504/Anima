using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInputBridge : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference menuAction;// arraste a Action "Menu"

    [Header("UI Routing")]
    public UIScreenRouter router;// arraste o Router da cena
    public string screenWhenOpened = "Pause";// qual tela abrir

    [Header("Detectar se já há menu aberto")]
    public GameObject[] anyMenuRoots;// ex.: Main, Pause, Settings

    void OnEnable()
    {
        if (menuAction)
        {
            menuAction.action.performed += OnMenu;
            menuAction.action.Enable();
        }
    }
    void OnDisable()
    {
        if (menuAction)
        {
            menuAction.action.performed -= OnMenu;
            menuAction.action.Disable();
        }
    }

    void OnMenu(InputAction.CallbackContext _)
    {
        if (IsAnyMenuOpen())
            router.Resume();// fecha tudo e volta ao jogo
        else
            router.Show(screenWhenOpened);// abre a tela definida (Pause)
    }

    bool IsAnyMenuOpen()
    {
        if (anyMenuRoots == null) return false;
        foreach (var go in anyMenuRoots) if (go && go.activeInHierarchy) return true;
        return false;
    }
}
