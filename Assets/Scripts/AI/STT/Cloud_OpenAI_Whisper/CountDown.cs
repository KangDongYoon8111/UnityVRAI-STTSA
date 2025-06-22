using System.Collections;
using UnityEngine;
using TMPro;

public class CountDown : MonoBehaviour
{
    public GameObject countPanel;
    public TextMeshProUGUI countText;

    public void CountDownStart(float second)
    {
        countPanel.SetActive(true);
        StartCoroutine(Count((int)second));
    }

    IEnumerator Count(int second)
    {
        TextMeshProUGUI countText = countPanel.GetComponentInChildren<TextMeshProUGUI>();

        for (int i = second; i >= 0; i--)
        {
            countText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
    }
}
