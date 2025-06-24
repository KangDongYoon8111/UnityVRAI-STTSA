#if UNITY_EDITOR
using UnityEditor; // Unity 에디터 관련 기능을 사용하기 위한 네임스페이스
using UnityEngine; // Unity의 기본 기능 (MonoBehaviour, Debug 등)

// Unity Editor 전용 창을 생성하여 Tokenizer 테스트를 수행할 수 있는 도구 창 클래스
public class TokenizerEditor : EditorWindow
{
    private string inputText = "오늘 날씨 어때?"; // 테스트용 초기 문장 입력값

    private TokenizerClient tokenizer; // 형태소 분석 서버 호출 객체
    private EmotionAnalyzerONNX emotionAnalyzer; // ONNX 감정 분석 객체

    // Unity 에디터의 메뉴에 "Tools/Tokenizer 테스트" 항목을 추가
    [MenuItem("Tools/Tokenizer 테스트")]
    public static void ShowWindow()
    {
        // 해당 도구 창을 생성하거나 포커스를 맞춤
        GetWindow<TokenizerEditor>("Tokenizer 테스트");
    }

    // 도구 창의 GUI를 구성하는 함수 (에디터 전용)
    private void OnGUI()
    {
        // 에디터가 Play 모드가 아닐 경우, 경고 메시지 출력 후 종료
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ 반드시 Play 모드에서만 테스트 가능합니다!", MessageType.Warning);
            return;
        }

        // 도구 창 UI 상단 제목 출력
        GUILayout.Label("형태소 분석 테스트 (Flask 서버)", EditorStyles.boldLabel);

        // 문장 입력 필드 UI
        inputText = EditorGUILayout.TextField("입력 문장", inputText);

        // 버튼이 클릭되면 형태소 분석 → 감정 분석 로직 실행
        if (GUILayout.Button("형태소 분석 → 감정 분석 실행"))
        {
            FindComponents(); // TokenizerClient와 EmotionAnalyzerONNX 객체 찾기

            if (tokenizer != null && emotionAnalyzer != null)
            {
                // 입력된 문장을 형태소 분석 서버에 전송
                tokenizer.GetTokenIndices(inputText, (tokenIndices) =>
                {
                    if (tokenIndices != null)
                    {
                        Debug.Log($"[형태소 분석 결과] {string.Join(", ", tokenIndices)}");

                        // 분석된 인덱스를 ONNX 감정 분석기로 전달
                        emotionAnalyzer.AnalyzeEmotion(tokenIndices, (emotion) =>
                        {
                            Debug.Log($"[감정 결과] {emotion}"); // 결과 출력
                        });
                    }
                    else
                    {
                        Debug.LogWarning("형태소 분석 실패");
                    }
                });
            }
            else
            {
                Debug.LogError("TokenizerClient 또는 EmotionAnalyzerONNX 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 현재 씬에 존재하는 TokenizerClient, EmotionAnalyzerONNX 컴포넌트를 찾아서 저장
    /// </summary>
    private void FindComponents()
    {
        tokenizer = FindObjectOfType<TokenizerClient>();
        emotionAnalyzer = FindObjectOfType<EmotionAnalyzerONNX>();
    }
}
#endif