using System.Collections;
using UnityEngine;
using TMPro;

public class RepeatingSTT : MonoBehaviour
{
    public float intervalSeconds = 30f;
    public TextMeshProUGUI debugText;

    private Coroutine repeatingCoroutine;
    private WhisperSTTClient stt;
    private CountDown countDown;

    private void Start()
    {
        stt = GetComponent<WhisperSTTClient>();
        countDown = GetComponent<CountDown>();
    }

    public void StartRepeatingSTT()
    {
        if(repeatingCoroutine == null)
        {
            repeatingCoroutine = StartCoroutine(RepeatSTT());
        }
    }

    public void StopRepeatingSTT()
    {
        if(repeatingCoroutine != null)
        {
            StopCoroutine(repeatingCoroutine);
            repeatingCoroutine = null;
            countDown.GetComponentInParent<GameObject>().SetActive(false);
        }
    }

    private IEnumerator RepeatSTT()
    {
        while (true)
        {
            if(stt != null)
            {
                stt.StartSTT();
            }
            countDown.CountDownStart(intervalSeconds);
            yield return new WaitForSeconds(intervalSeconds);
        }        
    }
}
