using System; // 날짜, 시간 등 시스템 기능 사용을 위해 
using System.Collections; // 코루틴 사용을 위해
using System.IO; // 파일 경로 및 파일 읽기/쓰기 기능 사용을 위해
using UnityEngine;
using UnityEngine.Networking; // UnityWebRequest 관련 기능 사용
using TMPro;

// OpenAI Whisper API를 활용해 WAV 음성 파일을 한국어 텍스트로 변환(STT)하는 기능을 담당
public class WhisperSTTClient : MonoBehaviour
{
    public TextMeshProUGUI debugText; // 디버그 출력용
    public TextMeshProUGUI chatText; // 최종적으로 인식된 텍스트를 보여줄 채팅 UI
    
    // 인식된 텍스트 결과를 저장할 문자열(인스펙터에서 디버깅 목적)
    [TextArea] public string recognizedText;

    private string apiKey = ""; // API Key
    private bool isProcessing = false; // 현재 STT 처리 중인지 여부를 표현
    private RepeatingSTT repeatingSTT; // 반복 STT 실행 컨트롤용(디버깅용)

    private void Start()
    {
        // 현재 씬 내의 RepeatingSTT 오브젝트를 찾아서 참조 저장
        repeatingSTT = FindAnyObjectByType<RepeatingSTT>();
        // STT 작업 초기 상태는 false (아무 작업도 안 함)
        isProcessing = false;
    }

    // 외부에서 STT 처리를 시작할 때 호출하는 메서드
    public void StartSTT()
    {
        // STT가 이미 처리 중이 아니라면 새로 시작
        if (!isProcessing)
        {
            // Whisper API로 WAV 파일을 전송하는 코루틴 실행
            StartCoroutine(SendWevToWhisper());
        }        
    }

    // 실제로 STT 처리를 수행하는 코루틴
    private IEnumerator SendWevToWhisper()
    {
        // 처리 중 상태로 전환하여 중복 요청 방지
        isProcessing = true;

        // 저장된 WAV 파일 경로 설정
        string filePath = Path.Combine(Application.persistentDataPath, "mic_output.wav");

        // 해당 WAV 파일이 존재하지 않을 경우 경고 출력하고 종료
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("WAV 파일이 없습니다: " + filePath);
            debugText.text = "WAV 파일이 없습니다: " + filePath;
            yield break; // 코루틴 종료
        }

        // 전체 로직 STT 처리 소요 시간 측정을 위한 시작 시간 기록
        DateTime sttTotalStartTime = DateTime.Now; // STT 총 시간 타이머 시작

        // WAV 파일 데이터를 바이트 배열로 모두 읽어오기
        byte[] wavData = File.ReadAllBytes(filePath);

        // 서버 STT 요청 응답 시간 측정을 위한 시작 시간 기록
        DateTime sttRequestStartTime = DateTime.Now; // STT 응답 시간 타이머 시작

        // HTTP POST 요청을 위한 폼 생성 : Whisper API에 전송할 데이터를 준비하는 ‘바구니’를 만들고 있는 중
        WWWForm form = new WWWForm();
        /* 
         * HTTP 요청이란? 웹사이트에 접속하거나 서버에 데이터를 요청할 때, HTTP라는 통신 규약을 사용.
         * 이 때 서버에 정보를 보내는 방법에는 POST(제출), GET(요청), PUT(교체), DELETE(삭제) 등 등이 있음.
         * 폼은 서버로 데이터를 보낼 때 특정 구조(데이터 컨테이너)를 만들어야함.
         * WWWForm이 위 역할을 수행
         * 최종 정리 : form은 Whisper API 서버에 보내줄 “파일과 문자열 데이터의 꾸러미”입니다.
         */

        // 비유 : 보낼 물건을 하나의 바구니에 차곡차곡 담는 과정
        // WAV 파일 데이터 첨부 (필드 이름: file, 파일 이름: mic_output.wav, MIME 타입: audio/wav)
        form.AddBinaryData("file", wavData, "mic_output.wav", "audio/wav");
        // Whisper 모델 이름 지정 (현재 OpenAI의 whisper-1 사용)
        form.AddField("model", "whisper-1");

        // OpenAI Whisper API 요청 생성
        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        /*
         * UnityWebRequest : Unity에서 웹 통신을 처리하는 클래스
         * 비유 : 정리된 바구니(form)를 택배 상자로 포장하는 과정
         */
        // 인증용 헤더 설정 (Bearer + API 키)
        // 비유 : 포장된 택배 상자에 송장(인증서)을 붙이는 과정
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // 요청 전송 및 응답 대기
        /* 비유 :
         * 1. 요청 전송 : 포장된 택배를 기사에게 넘겨서 배송 시작. 
         * 2. 응답 : 서버에서 택배의 내용물을 확인하고 남겨진 공간에 결과값을 채워서 다시 요청한 측에 배송.
         */
        yield return request.SendWebRequest();

        isProcessing = false; // STT 처리 완료 → isProcessing 상태 false로 복구

        // 서버 요청 응답 시간 측정 종료
        TimeSpan sttRequestDuration = DateTime.Now - sttRequestStartTime; // STT 응답 시간 타이머 종료

        // 요청 실패 시
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("STT 요청 실패: " + request.error);
            debugText.text = "STT 요청 실패: " + request.error;
            debugText.text += $"\n{repeatingSTT.intervalSeconds}초 대기 후 재시도 합니다.";
        }
        else // 요청 성공 시
        {
            // 응답 본문(JSON 문자열)을 가져옴
            string json = request.downloadHandler.text;
            Debug.Log("STT 응답: " + json);
            debugText.text = "STT 응답: " + json;

            // 응답 JSON을 WhisperResponse 타입으로 파싱하여 텍스트 추출
            string text = JsonUtility.FromJson<WhisperResponse>(json)?.text;
            /*
             * ?. : null 조건 연산자 앞의 객체가 null인지 먼저 확인한 후, 
             * null이 아니면 .text에 접근. null이면 그냥 null을 반환.
             */
            recognizedText = text; // 인식된 텍스트 저장
            chatText.text = text; // 채팅 UI에 인식된 텍스트 출력

            // 총 처리 시간 계산
            TimeSpan sttTotalDuration = DateTime.Now - sttTotalStartTime; // STT 총 시간 타이머 종료
            debugText.text += $"\nSTT 응답소요시간: {sttRequestDuration.TotalSeconds:F2}초";
            debugText.text += $"\nSTT 총소요시간: {sttTotalDuration.TotalSeconds:F2}초";

            repeatingSTT.StopRepeatingSTT(); // 반복 STT 로직을 중단 (한 번만 인식하기 위한 처리)
        }
    }

    // OpenAI Whisper API의 JSON 응답 구조 정의 클래스
    [System.Serializable]
    public class WhisperResponse
    {
        public string text; // JSON 응답 예시: { "text": "안녕하세요." }
    }
    /*
     * 왜 필요하나? Json의 경우 Key - Value 형식으로 데이터가 넘어온다.
     * 이를 그대로 사용하려면 매번 문자열 파싱(split, substring, regex 등) 처리 필요.
     * 번거롭기 때문에 Unity에서는 Key 항목에 자동으로 매핑하여 변환할 수 있는 기능을 제공
     * 자동 매핑의 기준이 되는 데이터 컨테이너(구조)로써 작동
     * 단점 : 응답 형식과 1:1로 맞춘 “분류 기준(스키마)” 맞춰야 된다.
     */

    /*
     * [System.Serializable] : 이 클래스는 데이터를 "직렬화" 할 수 있습니다. 알려주는 속성
     * 직렬화(Serialize)란? 객체(Object)의 내용을 한 줄의 데이터(텍스트, 바이너리 등)로 펼쳐서 저장하거나 전송할 수 있는 형식으로 변환하는 과정
     * 왜 데이터를 한 줄(직렬화) 해야 되느냐? 객체(Object)는 컴퓨터 입장에서는 복잡한 하나의 덩어리이다 보니,
     * 이러한 데이터를 저장하거나 전송하려면, "바이트 단위" 또는 "문자열(텍스트) 단위"로 변환해야 함 이러한 변환작업을 직렬화라고 하며,
     * 저장소(디스크)와 네트워크 통신에서는 표준 규격상으로도 문자열 데이터로 한 줄로 처리되어 전송을 규정하고 있다.
     */
}
