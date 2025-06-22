using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.InferenceEngine;

public class DebugHelperWhisper : MonoBehaviour
{
    [Header("연결된 컴포넌트")]
    public MicRecorderONNX micRecorder;
    public WhisperEncoderRunner encoder;
    public WhisperDecoderRunner decoder;

    [Header("자동 실행 여부")]
    public bool runOnStart = true;

    [Header("WAV 파일 테스트 모드")]
    public bool useWavInsteadOfMic = false;
    public string wavFilename = "mic_output.wav"; // persistentDataPath 기준

    void Start()
    {
        if (runOnStart)
        {
            if (useWavInsteadOfMic)
                RunFromWav();
            else
                RunFromMic();
        }
    }

    public async void RunFromMic()
    {
        if (micRecorder == null)
        {
            Debug.LogError("❌ micRecorder 연결 안됨");
            return;
        }

        float[] pcm = micRecorder.pcmData;
        if (pcm == null || pcm.Length == 0)
        {
            Debug.LogError("❌ PCM 데이터가 비어 있음. 녹음 먼저 수행 필요.");
            return;
        }

        Debug.Log($"🎙 Mic 기반 PCM 길이: {pcm.Length}");
        await RunPipeline(pcm);
    }

    public async void RunFromWav()
    {
        string wavPath = Path.Combine(Application.persistentDataPath, wavFilename);
        if (!File.Exists(wavPath))
        {
            Debug.LogError("❌ WAV 파일 없음: " + wavPath);
            return;
        }

        Debug.Log("📁 WAV 로드 중: " + wavPath);
        float[] pcm = LoadPcmFromWav(wavPath);

        if (pcm == null || pcm.Length == 0)
        {
            Debug.LogError("❌ WAV → PCM 추출 실패");
            return;
        }

        Debug.Log($"📁 WAV 기반 PCM 길이: {pcm.Length}");
        await RunPipeline(pcm);
    }

    private async Task RunPipeline(float[] pcm)
    {
        if (encoder == null || decoder == null)
        {
            Debug.LogError("❌ encoder 또는 decoder 연결 안됨");
            return;
        }

        float[,,] mel = MelSpectrogramGenerator.Generate(pcm); // [1, 80, T]
        mel = PadOrTrim(mel, 3000); // Whisper 기준

        int width = mel.GetLength(2);
        float[] flat = new float[1 * 80 * width];
        int index = 0;
        for (int h = 0; h < 80; h++)
        {
            for (int w = 0; w < width; w++)
                flat[index++] = mel[0, h, w];
        }

        var melTensor = new Tensor<float>(new TensorShape(1, 80, width), flat);
        var encoderOut = await encoder.RunEncoderAsync(melTensor);
        var result = await decoder.RunDecoderAsync(encoderOut);

        Debug.Log("💬 결과 (STT): " + result);

        melTensor.Dispose();
        encoderOut.Dispose();
    }

    private float[] LoadPcmFromWav(string path)
    {
        try
        {
            using var www = new WWW("file://" + path);
            while (!www.isDone) { }

            var clip = www.GetAudioClip(false, false);
            while (clip.loadState != AudioDataLoadState.Loaded) { }

            float[] data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ WAV 로드 실패: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Mel Spectrogram의 time dimension을 Whisper 요구인 3000 frame으로 보정
    /// </summary>
    private float[,,] PadOrTrim(float[,,] mel, int targetWidth)
    {
        int currentWidth = mel.GetLength(2);
        float[,,] result = new float[1, 80, targetWidth];

        for (int m = 0; m < 80; m++)
        {
            for (int t = 0; t < targetWidth; t++)
            {
                if (t < currentWidth)
                    result[0, m, t] = mel[0, m, t];
                else
                    result[0, m, t] = 0f;
            }
        }

        return result;
    }
}
