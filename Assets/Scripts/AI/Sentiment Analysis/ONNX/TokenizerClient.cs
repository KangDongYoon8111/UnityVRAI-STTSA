using UnityEngine; // Unity의 기본 API 사용 (MonoBehaviour 등)
using UnityEngine.Networking; // UnityWebRequest를 통한 HTTP 통신 기능 제공
using System; // Action, DateTime, TimeSpan 등 시스템 기본 클래스 포함
using System.Text; // 문자열을 바이트 배열로 변환하기 위한 Encoding 클래스 사용
using System.Collections; // 코루틴 사용을 위한 네임스페이스
using TMPro; // TextMeshPro UI 디버그 출력용

// 이 클래스는 Unity에서 Flask 기반 형태소 분석 서버에 HTTP POST 요청을 보내
// 입력 문장을 형태소 인덱스 배열(int[])로 받아오는 기능을 담당합니다.
public class TokenizerClient : MonoBehaviour
{
    public TextMeshProUGUI debugText; // 디버그 정보를 UI에 출력할 TextMeshProUGUI 컴포넌트

    [SerializeField] private string serverUrl = "http://140.238.2.178:5000/tokenize"; // 형태소 분석 Flask 서버 주소

    /// <summary>
    /// 외부에서 호출 가능한 형태소 분석 함수. 내부적으로 코루틴을 시작합니다.
    /// </summary>
    /// <param name="inputText">분석할 한국어 문장</param>
    /// <param name="onComplete">형태소 인덱스 배열을 반환받는 콜백 함수</param>
    public void GetTokenIndices(string inputText, Action<int[]> onComplete)
    {
        StartCoroutine(PostToTokenizer(inputText, onComplete)); // 코루틴 실행 (비동기 HTTP 요청)
    }

    /// <summary>
    /// 형태소 분석 서버에 POST 요청을 보내고, 인덱스 배열 결과를 받아오는 코루틴 함수
    /// </summary>
    /// <param name="inputText">입력 문장</param>
    /// <param name="onComplete">분석 결과 콜백</param>
    private IEnumerator PostToTokenizer(string inputText, Action<int[]> onComplete)
    {
        // 입력 문장을 JSON 형태로 변환: { "text": "입력 문장" }
        string json = JsonUtility.ToJson(new InputText { text = inputText });

        // UTF-8 인코딩을 통해 바이트 배열로 변환
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // UnityWebRequest 객체 생성: POST 요청 설정
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw); // POST 본문 설정
        request.downloadHandler = new DownloadHandlerBuffer(); // 응답을 문자열 형태로 받기 위함
        request.SetRequestHeader("Content-Type", "application/json"); // 요청 헤더 설정

        DateTime requestStartTime = DateTime.Now; // 요청 시작 시간 저장 (응답 속도 측정용)

        yield return request.SendWebRequest(); // 서버에 요청 전송 및 응답 대기 (비동기)

        TimeSpan RequestDuration = DateTime.Now - requestStartTime; // 응답 완료 시점까지 시간 계산
        debugText.text += $"토큰화 응답소요시간: {RequestDuration.TotalSeconds:F2}초";

        // 요청 성공 여부 확인
        if (request.result == UnityWebRequest.Result.Success)
        {
            // 응답 본문(JSON 문자열) → 예: { "input": [101, 43, 56, ...] }
            var jsonResult = request.downloadHandler.text;

            // JSON 문자열을 TokenResponse 객체로 역직렬화
            TokenResponse result = JsonUtility.FromJson<TokenResponse>(jsonResult);

            // 결과 콜백 호출 (형태소 인덱스 배열 전달)
            onComplete?.Invoke(result.input);
        }
        else
        {
            // 네트워크 오류나 서버 오류 발생 시 경고 출력
            Debug.LogWarning($"Tokenizer 요청 실패: {request.error}");
            debugText.text += $"Tokenizer 요청 실패: {request.error}";
            onComplete?.Invoke(null); // null 전달
        }
    }

    // 서버에 전송할 JSON 객체 구조 정의 (직렬화용)
    [Serializable] public class InputText { public string text; } // 입력 문장 키: "text"

    // 서버에서 받을 JSON 응답 구조 정의 (역직렬화용)
    [Serializable] public class TokenResponse { public int[] input; } // 출력 키: "input"
}
