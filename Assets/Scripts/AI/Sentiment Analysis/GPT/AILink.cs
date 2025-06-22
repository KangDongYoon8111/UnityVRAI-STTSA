using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AILink : MonoBehaviour
{
    public Sprite[] emotionSprites; // 감정에 대응되는 이모티콘 이미지
    public string[] emotionLabels = new string[] { "평범", "행복", "슬픔", "분노", "놀람", "사랑", "지루함", "충격" };
    
    public Image emoticonImage;
    public TextMeshProUGUI chatUI;

    private WhisperSTTClient whisper;
    private GPTEmotionAnalyzer gpt;

    private void Start()
    {
        whisper = FindAnyObjectByType<WhisperSTTClient>();
        gpt = FindAnyObjectByType<GPTEmotionAnalyzer>();
    }

    public void EmotionAnalyzer()
    {
        gpt.AnalyzeEmotion(whisper.recognizedText, (emotion) =>
        {
            Debug.Log("최종 감정 결과: " + emotion);
            StartCoroutine(ShowEmotionIcon(emotion));
        });
    }

    private IEnumerator ShowEmotionIcon(string emotion)
    {
        for(int i = 0; i < emotionLabels.Length; i++)
        {
            if(emotion == emotionLabels[i])
            {
                emoticonImage.enabled = true; // 이미지 UI 활성화
                emoticonImage.sprite = emotionSprites[i]; // 대응 이모티콘 출력                
                chatUI.text += $"\n<sprite={i}>";
            }
        }

        yield return new WaitForSeconds(5f);
        //emoticonImage.sprite = null;
        //emoticonImage.gameObject.SetActive(false);
    }
}
