using System.Collections; // 코루틴 사용을 위해 필요
using UnityEngine;
using UnityEngine.Networking; // UnityWebRequest 네트워크 요청 기능 사용
using TMPro;
using System.Text;
using System;

// OpenAI GPT API를 이용해 텍스트 감정 분석을 수행하는 기능을 담당
public class GPTEmotionAnalyzer : MonoBehaviour
{
    public TextMeshProUGUI debugText; // 디버그 출력용
    public string[] emotions = new string[] { "평범", "행복", "슬픔", "분노", "놀람", "사랑", "지루함", "충격" };
    
    // 응답 원본 JSON 형태를 저장할 문자열(인스펙터에서 디버깅 목적)
    [TextArea] public string resultValue;

    private string apiKey = ""; // OpenAI API 키 입력(개인화 영역 : 개인 키는 외부 노출 금지)

    // 감정 분석용 프롬프트 정의
    private string BuildSystemMessage()
    {
        // 배열을 문자열로 결합
        string emotionList = string.Join(", ", emotions);

        // 최종 시스템 메시지 생성
        return $"당신은 감정 분석 전문가입니다. 다음 문장을 보고 감정을 분석하세요. 감정은 반드시 아래 {emotions.Length}개 중 하나만 출력하세요: {emotionList}";
    }

    /// <summary>
    /// 외부에서 이 함수를 호출해 감정 분석을 시작할 수 있음
    /// </summary>
    /// <param name="inpuText">분석 대상 문장</param>
    /// <param name="onComplete">분석 결과를 콜백으로 넘겨줌</param>
    public void AnalyzeEmotion(string inpuText, System.Action<string> onComplete)
    {
        string systemMessage = BuildSystemMessage(); // 동적으로 메시지 생성
        StartCoroutine(SendToGPT(inpuText, systemMessage, onComplete)); // 코루틴 실행
    }
    /*
     * Action<T> :
     * 1. Action : C#에서 제공하는 내장 델리게이트(delegate) 타입(입력과 출력이 없는 메서드를 담을 수 있는 역할)
     * 2. <T> : 입력 매개변수의 타입으로 Type의 T 약자로 보면된다. 제네릭 표기법으로 코드를 미리 "틀" 처럼 만들어두고,
     *          실제 사용할 때 자료형(타입)을 지정하는 방식
     * 3. Action<T> : 입력만 있고 출력이 없는 메서드를 담을 수 있는 역할
     * 3-1. Func<T, TResult> : 입력도 있고 결과도 있는 메서드를 담을 수 있는 역할
     * 4. Action<string> : 입력의 타입이 string인 메서드를 담을 수 있는 역할
     */

    // GPT API로 분석 요청을 보내고, 응답을 받아서 결과 감정을 콜백으로 넘겨주는 코루틴
    private IEnumerator SendToGPT(string userInput, string systemMessage, System.Action<string> onComplete)
    {
        // 전체 로직 감정분석 처리 소요 시간 측정을 위한 시작 시간 기록
        DateTime gptTotalStartTime = DateTime.Now; // 감정분석 총 시간 타이머 시작

        // OpenAI의 GPT Chat Completion API 주소(참조: https://platform.openai.com/docs/api-reference/chat/create)
        string url = "https://api.openai.com/v1/chat/completions";

        // 서버 감정분석 요청 응답 시간 측정을 위한 시작 시간 기록
        DateTime gptRequestStartTime = DateTime.Now; // 감정분석 응답 시간 타이머 시작

        // GPT 에게 보낼 JSON 데이터를 JsonUtility로 감싸기 위해 구조체를 생성
        string jsonPayload = JsonUtility.ToJson(new GPTMessageWrapper
        {
            model = "gpt-3.5-turbo", // 사용할 모델 이름
            messages = new GPTMessage[]
            {
                new GPTMessage { role = "system", content = systemMessage }, // GPT의 역할 정의
                new GPTMessage { role = "user", content = $"문장: \"{userInput}\"\n답변:" } // 사용자의 입력 문장
            }
        });

        // UnityWebRequest 객체 생성, POST 방식으로 전송할 준비
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        // UTF-8로 문자열을 바이트 배열로 변환 후 업로드 핸들러에 설정
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // 응답 데이터를 버퍼에 담아 처리 (텍스트 형태로 받기 위함)
        request.downloadHandler = new DownloadHandlerBuffer();

        // 요청 헤더 설정: JSON 타입이며, 인증 토큰 추가
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // 실제 요청을 보내고 응답이 올 때까지 기다림(코루틴이 대기)
        yield return request.SendWebRequest();

        // 서버 요청 응답 시간 측정 종료
        TimeSpan gptRequestDuration = DateTime.Now - gptRequestStartTime; // STT 응답 시간 타이머 종료

        // 요청이 실패한 경우
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"GPT 요청 실패: {request.error}");
            debugText.text = $"GPT 요청 실패: {request.error}";
            onComplete?.Invoke("분석 실패"); // 콜백 함수에 "분석 실패" 전달
            /*
             * Invoke : Unity Invoke VS C# delegate.Invoke
             * MonoBehaviour.Invoke()는 "3초 뒤에 함수 실행해줘"
             * Action.Invoke()는 "지금 이 콜백 함수 실행해줘"
             * 즉, 같은 이름이지만 완전히 다른 기능.
             */
        }
        else // 요청 성공한 경우
        {
            string response = request.downloadHandler.text; // 응답 본문(JSON) 가져오기
            resultValue = response; // 인스펙터상 디버깅 목적
            string emotion = ExtractEmotionFromResponse(response); // 응답에서 감정 텍스트만 추출

            Debug.Log($"감정 분석 결과: {emotion}");
            debugText.text = $"감정 분석 결과: {emotion}"; // 디버그 출력

            onComplete?.Invoke(emotion); // 콜백으로 감정 결과 전달

            // 총 처리 시간 계산
            TimeSpan gptTotalDuration = DateTime.Now - gptTotalStartTime; // 총 시간 타이머 종료
            debugText.text += $"\nGPT 응답소요시간: {gptRequestDuration.TotalSeconds:F2}초";
            debugText.text += $"\nGPT 총소요시간: {gptTotalDuration.TotalSeconds:F2}초";
        }
    }

    // GPT 응답 Json에서 "감정 결과" 텍스트만 안전하게 추출하는 메서드
    private string ExtractEmotionFromResponse(string json)
    {
        try
        {
            GPTResponse parsed = JsonUtility.FromJson<GPTResponse>(json); // 응답 전체 구조 파싱
            return parsed.choices[0].message.content.Trim(); // 감정 텍스트만 반환
        }
        catch (Exception e)
        {
            Debug.LogWarning("감정 분석 결과 파싱 실패: " + e.Message); // 예외 처리
            debugText.text = ("감정 분석 결과 파싱 실패: " + e.Message);
            return "분석 실패";
        }
    }

    // GPT 메시지를 구성하기 위한 래퍼 클래스(Chat 형식용)
    [System.Serializable]
    public class GPTMessageWrapper
    {
        public string model; // 사용할 모델 이름
        public GPTMessage[] messages; // system/user 메시지 배열
    }

    // GPT API에 전달할 각 메시지를 나타내는 클래스
    [System.Serializable]
    public class GPTMessage
    {
        public string role; // 역할(system, user 등)
        public string content; // 실제 메시지 내용
    }

    // GPT 응답 JSON 최상위 구조 (choices 배열 포함)
    [System.Serializable]
    public class GPTResponse
    {
        public GPTChoice[] choices; // 응답 목록 배열
    }

    // choices 배열 내 각 항목 구조 (message 포함)
    [System.Serializable]
    public class GPTChoice
    {
        public GPTMessage message; // 감정 텍스트가 포함된 메시지
    }
}
