using UnityEngine;
using System.IO;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

// 본 클래스는 마이크에서 소리를 녹음하고 WAV 파일로 저장한 뒤, 다시 재생할 수 있도록 처리됨
public class MicRecorder : MonoBehaviour
{
    [Header("Debug UI")]
    public TextMeshProUGUI debugText;

    public int sampleRate = 16000; // 녹음 품질 (높을수록 품질이 좋음)
    public int maxRecordTime = 5; // 녹음할 최대 시간 (초 단위)

    private AudioClip recordedClip; // 녹음된 소리를 저장하는 AudioClip
    private string micDevice; // 사용할 마이크 이름
    private bool isRecording = false; // 현재 녹음 중인지 상태를 저장

    private AudioSource audioSource; // 녹음된 소리를 재생하기 위한 오디오 소스
    private string wavPath; // WAV 파일이 저장될 경로

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR // Quest 기기에서 실행될 경우
        if (Microphone.devices.Length > 0) // 사용 가능한 마이크가 있다면
        {
            micDevice = Microphone.devices[0]; // 첫 번째 마이크 사용

            Debug.Log("Quest 마이크: " +  micDevice);
            debugText.text = "Quest 마이크: " + micDevice;
        }
        else // 마이크가 없다면
        {
            Debug.LogWarning("Quest 마이크를 찾을 수 없습니다.");
            debugText.text = "Quest 마이크를 찾을 수 없습니다.";
        }
#else // PC Editor상에서 실행될 경우(Quest Link 포함)
        if (Microphone.devices.Length > 0) // 사용 가능한 마이크가 있다면
        {
            micDevice = Microphone.devices[0]; // 첫 번째 마이크 사용

            Debug.Log("PC 마이크: " + micDevice);
            debugText.text = "PC 마이크: " + micDevice;
        }
        else // 마이크가 없다면
        {
            Debug.LogWarning("PC 마이크를 찾을 수 없습니다.");
            debugText.text = "PC 마이크를 찾을 수 없습니다.";
        }
#endif

        // WAV 파일이 저장될 경로 설정(게임이나 앱의 데이터 폴더)
        wavPath = Path.Combine(Application.persistentDataPath, "mic_output.wav");
        audioSource = GetComponent<AudioSource>(); // 오디오 소스 컴포넌트 가져오기
    }

    // 녹음 시작
    public void StartRecording()
    {
        // 이미 녹음 중이거나 마이크가 없으면 실행하지 않음
        if (isRecording || micDevice == null) return;

        // 녹음 시작
        recordedClip = Microphone.Start(micDevice, false, maxRecordTime, sampleRate);
        isRecording = true; // 녹음 상태를 true로 설정

        Debug.Log("녹음 시작");
        debugText.text = "녹음 시작";
    }

    // 녹음을 중지하고 WAV 파일로 저장
    public void StopRecording()
    {
        // 녹음 중이 아니면 실행하지 않음
        if(!isRecording) return;

        Microphone.End(micDevice); // 녹음 중지
        isRecording = false; // 녹음 상태를 false로 설정

        Debug.Log("녹음 종료");

        SaveClipAsWav(recordedClip); // 녹음된 오디오를 WAV 파일로 저장
    }

    // AudioClip을 WAV 파일로 저장
    private void SaveClipAsWav(AudioClip clip)
    {
        WavUtility.Save(clip, wavPath); // WAV 파일로 저장

        Debug.Log("WAV 저장 완료: " + wavPath);
        debugText.text = "WAV 저장 완료: " + wavPath;
    }

    // 저장된 WAV 파일을 재생
    public void PlaySavedWav()
    {
        if (!File.Exists(wavPath)) // 경로에 WAV 파일이 없으면
        {
            Debug.LogWarning("WAV 파일이 존재하지 않습니다: " + wavPath);
            debugText.text = "WAV 파일이 존재하지 않습니다: " + wavPath;
            return;
        }

        StartCoroutine(LoadAndPlayWav(wavPath)); // WAV 파일 로드 후 재생
    }

    // WAV 파일을 로드하여 오디오 소스로 재생하는 코루틴
    private IEnumerator LoadAndPlayWav(string path)
    {
        string url = "file://" + path; // 로컬 파일 경로 설정

        // WAV 파일 로드 요청
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return www.SendWebRequest(); // 요청 완료까지 대기

            if(www.result != UnityWebRequest.Result.Success) // 로드 실패 시
            {
                Debug.LogError("WAV 로드 실패: " + www.error);
                debugText.text = "WAV 로드 실패: " + www.error;
            }
            else // 로드 성공
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www); // 로드된 오디오 클립 가져오기
                audioSource.clip = clip; // 오디오 소스에 클립 설정
                audioSource.Play(); // 재생 시작
            }
        }
    }
}
