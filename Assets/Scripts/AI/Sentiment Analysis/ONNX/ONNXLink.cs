using UnityEngine; // Unity 엔진의 기본 기능 제공
using UnityEngine.UI; // UI 이미지(Image) 컴포넌트 사용을 위해 필요
using TMPro; // TextMeshPro UI 텍스트 출력을 위해 필요
using System.Collections; // IEnumerator, Coroutine 사용을 위해 필요
using System; // Action, DateTime 등 기본 시스템 기능 사용

// 이 클래스는 Unity UI 버튼을 통해 STT 결과를 기반으로 형태소 분석 + 감정 분석을 실행하고,
// 결과 감정을 이모티콘 이미지로 표시하는 기능을 담당합니다.
public class ONNXLink : MonoBehaviour
{
    public TextMeshProUGUI debugText; // 디버그 텍스트 출력용 UI 컴포넌트

    public Sprite[] emotionSprites; // 감정별 대응 이모티콘 이미지 배열

    // 감정 분석 결과에 대응하는 라벨 텍스트 배열 (ONNX 모델의 출력과 순서 일치해야 함)
    public string[] emotionLabels = new string[] { "중립", "당황", "슬픔", "분노", "놀람", "기쁨", "불안", "상처" };

    public Image emoticonIamge; // 감정에 해당하는 이모티콘 이미지를 표시할 UI 이미지 컴포넌트
    public TextMeshProUGUI chatUI; // 감정 이모티콘을 출력할 채팅 UI 텍스트

    // 내부 참조용: 각 기능을 수행할 외부 클래스들
    private WhisperSTTClient whisper; // 음성 → 텍스트(STT) 결과를 가져올 클래스
    private TokenizerClient tokenizer; // 형태소 분석 서버와 통신하는 클래스
    private EmotionAnalyzerONNX analyzer; // ONNX 감정 분석기

    private void Start()
    {
        // 씬 내에서 각 클래스 컴포넌트를 자동으로 찾아서 참조 저장
        whisper = FindAnyObjectByType<WhisperSTTClient>();
        tokenizer = FindAnyObjectByType<TokenizerClient>();
        analyzer = FindAnyObjectByType<EmotionAnalyzerONNX>();
    }

    /// <summary>
    /// 버튼 클릭 등 외부에서 호출되는 진입점: STT 결과 기반으로 감정 분석 시작
    /// </summary>
    public void AnalyzeEmotion()
    {
        StartCoroutine(WaitForSTTAndAnalyze()); // 코루틴 실행
    }

    /// <summary>
    /// STT 결과가 준비될 때까지 대기 후 형태소 분석 및 감정 분석 진행
    /// </summary>
    private IEnumerator WaitForSTTAndAnalyze()
    {
        // STT 결과가 null이면 안내 메시지 출력 후 중단
        if (whisper.recognizedText == null)
        {
            Debug.Log("STT 먼저 진행하세요.");
            debugText.text += "STT 먼저 진행하세요.";
            yield break; // 코루틴 종료
        }

        string text = whisper.recognizedText; // STT로부터 인식된 문장 텍스트 가져오기

        // 서버에 문장을 전송해 형태소 인덱스 배열을 요청
        tokenizer.GetTokenIndices(text, (tokenIndices) =>
        {
            if (tokenIndices != null)
            {
                // 인덱스 배열을 ONNX 분석기로 넘겨 감정 분석 실행
                analyzer.AnalyzeEmotion(tokenIndices, (emotion) =>
                {
                    StartCoroutine(ShowEmotionIcon(emotion)); // 결과 감정 이모티콘 표시
                });
            }
            else
            {
                Debug.Log("형태소 분석 실패");
                debugText.text += "형태소 분석 실패";
            }
        });
    }

    /// <summary>
    /// 감정 문자열에 해당하는 이모티콘 이미지를 표시하는 코루틴
    /// </summary>
    /// <param name="emotion">감정 라벨 문자열 (예: "기쁨")</param>
    private IEnumerator ShowEmotionIcon(string emotion)
    {
        for (int i = 0; i < emotionLabels.Length; i++)
        {
            if (emotion == emotionLabels[i])
            {
                emoticonIamge.enabled = true; // 이미지 UI 활성화
                emoticonIamge.sprite = emotionSprites[i]; // 해당 감정 이모티콘 설정
                chatUI.text += $"\n<sprite={i}>"; // 채팅창에 이모티콘 태그 출력
            }
        }

        yield return new WaitForSeconds(5f); // 5초간 유지

        emoticonIamge.sprite = null; // 이미지 초기화
        emoticonIamge.enabled = false; // 이미지 비활성화
    }
}