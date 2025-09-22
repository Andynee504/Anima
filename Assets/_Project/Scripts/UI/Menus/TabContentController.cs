using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabContentController : MonoBehaviour
{
    [System.Serializable]
    public struct Tab
    {
        public Toggle toggle;       // Toggle da aba (Audio/Video/Acessibilidade)
        public GameObject content;  // Root do conteúdo correspondente
    }

    [SerializeField] List<Tab> tabs = new List<Tab>();
    [SerializeField] bool enforceOneOn = true; // garante 1 ativa sempre

    void Awake()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i;
            if (tabs[idx].toggle != null)
                tabs[idx].toggle.onValueChanged.AddListener(on => { if (on) Show(idx); });
        }
        // estado inicial
        int firstOn = GetFirstOn();
        Show(firstOn >= 0 ? firstOn : 0);
    }

    int GetFirstOn()
    {
        for (int i = 0; i < tabs.Count; i++)
            if (tabs[i].toggle && tabs[i].toggle.isOn) return i;
        return -1;
    }

    public void Show(int i)
    {
        for (int t = 0; t < tabs.Count; t++)
        {
            bool on = (t == i);
            if (tabs[t].content) tabs[t].content.SetActive(on);
            if (enforceOneOn && tabs[t].toggle && tabs[t].toggle.isOn != on)
                tabs[t].toggle.SetIsOnWithoutNotify(on); // não dispara evento de novo
        }
    }
}
