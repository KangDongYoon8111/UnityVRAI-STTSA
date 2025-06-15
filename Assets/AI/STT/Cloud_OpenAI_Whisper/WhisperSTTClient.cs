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
            Debug.LogWarning("WAV ������ �����ϴ�: " + filePath);
            debugText.text = "WAV ������ �����ϴ�: " + filePath;
            yield break;
        }

        DateTime sttTotalStartTime = DateTime.Now; // STT �� �ð� Ÿ�̸� ����

        byte[] wavData = File.ReadAllBytes(filePath);

        DateTime sttRequestStartTime = DateTime.Now; // STT ���� �ð� Ÿ�̸� ����

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "mic_output.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        isProcessing = false;

        TimeSpan sttRequestDuration = DateTime.Now - sttRequestStartTime; // STT ���� �ð� Ÿ�̸� ����

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("STT ��û ����: " + request.error);
            debugText.text = "STT ��û ����: " + request.error;
            debugText.text += $"\n{repeatingSTT.intervalSeconds}�� ��� �� ��õ� �մϴ�.";
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("STT ����: " + json);
            debugText.text = "STT ����: " + json;

            string text = JsonUtility.FromJson<WhisperResponse>(json)?.text;
            recognizedText = text;
            chatText.text = text;

            TimeSpan sttTotalDuration = DateTime.Now - sttTotalStartTime; // STT �� �ð� Ÿ�̸� ����
            debugText.text += $"\nSTT ����ҿ�ð�: {sttRequestDuration.TotalSeconds:F2}��";
            debugText.text += $"\nSTT �Ѽҿ�ð�: {sttTotalDuration.TotalSeconds:F2}��";

            repeatingSTT.StopRepeatingSTT();
        }
    }

    [System.Serializable]
    public class WhisperResponse
    {
        public string text;
    }
}
