using UnityEngine;
using TMPro;

public class UIDebugController : MonoBehaviour
{
    public static UIDebugController instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = FindAnyObjectByType<UIDebugController>();
            }
            return m_instance;
        }
    }

    private static UIDebugController m_instance;

    public TextMeshProUGUI debugText;

    public void DebugLog(string msg)
    {
        debugText.text = msg;
    }
}
