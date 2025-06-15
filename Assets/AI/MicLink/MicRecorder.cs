using UnityEngine;
using System.IO;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

// �� Ŭ������ ����ũ���� �Ҹ��� �����ϰ� WAV ���Ϸ� ������ ��, �ٽ� ����� �� �ֵ��� ó����
public class MicRecorder : MonoBehaviour
{
    [Header("Debug UI")]
    public TextMeshProUGUI debugText;

    public int sampleRate = 16000; // ���� ǰ�� (�������� ǰ���� ����)
    public int maxRecordTime = 5; // ������ �ִ� �ð� (�� ����)

    private AudioClip recordedClip; // ������ �Ҹ��� �����ϴ� AudioClip
    private string micDevice; // ����� ����ũ �̸�
    private bool isRecording = false; // ���� ���� ������ ���¸� ����

    private AudioSource audioSource; // ������ �Ҹ��� ����ϱ� ���� ����� �ҽ�
    private string wavPath; // WAV ������ ����� ���

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR // Quest ��⿡�� ����� ���
        if (Microphone.devices.Length > 0) // ��� ������ ����ũ�� �ִٸ�
        {
            micDevice = Microphone.devices[0]; // ù ��° ����ũ ���

            Debug.Log("Quest ����ũ: " +  micDevice);
            debugText.text = "Quest ����ũ: " + micDevice;
        }
        else // ����ũ�� ���ٸ�
        {
            Debug.LogWarning("Quest ����ũ�� ã�� �� �����ϴ�.");
            debugText.text = "Quest ����ũ�� ã�� �� �����ϴ�.";
        }
#else // PC Editor�󿡼� ����� ���(Quest Link ����)
        if (Microphone.devices.Length > 0) // ��� ������ ����ũ�� �ִٸ�
        {
            micDevice = Microphone.devices[0]; // ù ��° ����ũ ���

            Debug.Log("PC ����ũ: " + micDevice);
            debugText.text = "PC ����ũ: " + micDevice;
        }
        else // ����ũ�� ���ٸ�
        {
            Debug.LogWarning("PC ����ũ�� ã�� �� �����ϴ�.");
            debugText.text = "PC ����ũ�� ã�� �� �����ϴ�.";
        }
#endif

        // WAV ������ ����� ��� ����(�����̳� ���� ������ ����)
        wavPath = Path.Combine(Application.persistentDataPath, "mic_output.wav");
        audioSource = GetComponent<AudioSource>(); // ����� �ҽ� ������Ʈ ��������
    }

    // ���� ����
    public void StartRecording()
    {
        // �̹� ���� ���̰ų� ����ũ�� ������ �������� ����
        if (isRecording || micDevice == null) return;

        // ���� ����
        recordedClip = Microphone.Start(micDevice, false, maxRecordTime, sampleRate);
        isRecording = true; // ���� ���¸� true�� ����

        Debug.Log("���� ����");
        debugText.text = "���� ����";
    }

    // ������ �����ϰ� WAV ���Ϸ� ����
    public void StopRecording()
    {
        // ���� ���� �ƴϸ� �������� ����
        if(!isRecording) return;

        Microphone.End(micDevice); // ���� ����
        isRecording = false; // ���� ���¸� false�� ����

        Debug.Log("���� ����");

        SaveClipAsWav(recordedClip); // ������ ������� WAV ���Ϸ� ����
    }

    // AudioClip�� WAV ���Ϸ� ����
    private void SaveClipAsWav(AudioClip clip)
    {
        WavUtility.Save(clip, wavPath); // WAV ���Ϸ� ����

        Debug.Log("WAV ���� �Ϸ�: " + wavPath);
        debugText.text = "WAV ���� �Ϸ�: " + wavPath;
    }

    // ����� WAV ������ ���
    public void PlaySavedWav()
    {
        if (!File.Exists(wavPath)) // ��ο� WAV ������ ������
        {
            Debug.LogWarning("WAV ������ �������� �ʽ��ϴ�: " + wavPath);
            debugText.text = "WAV ������ �������� �ʽ��ϴ�: " + wavPath;
            return;
        }

        StartCoroutine(LoadAndPlayWav(wavPath)); // WAV ���� �ε� �� ���
    }

    // WAV ������ �ε��Ͽ� ����� �ҽ��� ����ϴ� �ڷ�ƾ
    private IEnumerator LoadAndPlayWav(string path)
    {
        string url = "file://" + path; // ���� ���� ��� ����

        // WAV ���� �ε� ��û
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return www.SendWebRequest(); // ��û �Ϸ���� ���

            if(www.result != UnityWebRequest.Result.Success) // �ε� ���� ��
            {
                Debug.LogError("WAV �ε� ����: " + www.error);
                debugText.text = "WAV �ε� ����: " + www.error;
            }
            else // �ε� ����
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www); // �ε�� ����� Ŭ�� ��������
                audioSource.clip = clip; // ����� �ҽ��� Ŭ�� ����
                audioSource.Play(); // ��� ����
            }
        }
    }
}
