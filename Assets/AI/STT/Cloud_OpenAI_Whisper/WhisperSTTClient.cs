using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class WhisperSTTClient : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI chatText;
    [TextArea] public string recognizedText;

    private string apiKey = "";
    private bool isProcessing = false;
    private RepeatingSTT repeatingSTT;

    private void Start()
    {
        repeatingSTT = FindAnyObjectByType<RepeatingSTT>();
        isProcessing = false;
    }

    public void StartSTT()
    {
        if (!isProcessing)
        {
            StartCoroutine(SendWevToWhisper());
        }        
    }

    private IEnumerator SendWevToWhisper()
    {
        isProcessing = true;

        string filePath = Path.Combine(Application.persistentDataPath, "mic_output.wav");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("WAV 파일이 없습니다: " + filePath);
            debugText.text = "WAV 파일이 없습니다: " + filePath;
            yield break;
        }

        DateTime sttTotalStartTime = DateTime.Now; // STT 총 시간 타이머 시작

        byte[] wavData = File.ReadAllBytes(filePath);

        DateTime sttRequestStartTime = DateTime.Now; // STT 응답 시간 타이머 시작

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "mic_output.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        isProcessing = false;

        TimeSpan sttRequestDuration = DateTime.Now - sttRequestStartTime; // STT 응답 시간 타이머 종료

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("STT 요청 실패: " + request.error);
            debugText.text = "STT 요청 실패: " + request.error;
            debugText.text += $"\n{repeatingSTT.intervalSeconds}초 대기 후 재시도 합니다.";
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("STT 응답: " + json);
            debugText.text = "STT 응답: " + json;

            string text = JsonUtility.FromJson<WhisperResponse>(json)?.text;
            recognizedText = text;
            chatText.text = text;

            TimeSpan sttTotalDuration = DateTime.Now - sttTotalStartTime; // STT 총 시간 타이머 종료
            debugText.text += $"\nSTT 응답소요시간: {sttRequestDuration.TotalSeconds:F2}초";
            debugText.text += $"\nSTT 총소요시간: {sttTotalDuration.TotalSeconds:F2}초";

            repeatingSTT.StopRepeatingSTT();
        }
    }

    [System.Serializable]
    public class WhisperResponse
    {
        public string text;
    }
}
