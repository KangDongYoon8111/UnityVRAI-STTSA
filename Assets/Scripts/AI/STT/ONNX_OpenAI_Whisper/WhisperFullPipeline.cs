// WhisperFullPipeline.cs - 수정된 최신 버전 (Flatten 및 Generate() 반영)
// 전체 STT 흐름 통합: Mic → Mel → Encoder → Decoder → UI

using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.InferenceEngine;
using TMPro;

public class WhisperFullPipeline : MonoBehaviour
{
    [Header("연결된 구성 요소")]
    public MicRecorderONNX micRecorder;
    public WhisperEncoderRunner encoderRunner;
    public WhisperDecoderRunner decoderRunner;

    [Header("UI 출력")]
    public TextMeshProUGUI outputText;
    [Header("Debug UI")]
    public TextMeshProUGUI debugText;

    private float[] pcmData;
    private bool isRunning = false;

    public async void StartSTT()
    {
        if (isRunning) return;
        isRunning = true;

        Debug.Log("🎤 녹음 시작");
        debugText.text = "🎤 녹음 시작";
        micRecorder.StartRecording();

        await Task.Delay(5000); // 5초간 녹음

        Debug.Log("🛑 녹음 종료 및 PCM 추출");
        debugText.text = "🛑 녹음 종료 및 PCM 추출";
        micRecorder.StopRecording();
        pcmData = micRecorder.pcmData; // 또는 micRecorder.GetPcmData();

        if (pcmData == null || pcmData.Length == 0)
        {
            Debug.LogError("❌ PCM 데이터가 없습니다.");
            debugText.text = "❌ PCM 데이터가 없습니다.";
            outputText.text = "음성 입력 실패";
            isRunning = false;
            return;
        }

        Debug.Log("🎚️ Mel Spectrogram 생성");
        debugText.text = "🎚️ Mel Spectrogram 생성";
        float[,,] mel = MelSpectrogramGenerator.Generate(pcmData); // FFT 기반 변환
        mel = PadOrTrim(mel, 3000); // ✅ 고정 길이 보정

        Debug.Log("🧠 Encoder 실행");
        debugText.text = "🧠 Encoder 실행";
        int width = mel.GetLength(2);
        var shape = new TensorShape(1, 80, width);
        float[] flattenedMel = Flatten(mel);
        Tensor melTensor = new Tensor<float>(shape, flattenedMel);
        Tensor encoderOutput = await encoderRunner.RunEncoderAsync(melTensor);

        Debug.Log("📜 Decoder 실행");
        debugText.text = "📜 Decoder 실행";
        string result = await decoderRunner.RunDecoderAsync(encoderOutput);

        Debug.Log("💬 결과: " + result);
        debugText.text = "💬 결과: " + result;
        outputText.text = result;

        encoderOutput.Dispose();
        melTensor.Dispose();

        isRunning = false;
    }

    private float[] Flatten(float[,,] input)
    {
        int d0 = input.GetLength(0);
        int d1 = input.GetLength(1);
        int d2 = input.GetLength(2);

        float[] flat = new float[d0 * d1 * d2];
        int index = 0;
        for (int i = 0; i < d0; i++)
            for (int j = 0; j < d1; j++)
                for (int k = 0; k < d2; k++)
                    flat[index++] = input[i, j, k];

        return flat;
    }

    private float[,,] PadOrTrim(float[,,] mel, int targetLength)
    {
        int batch = mel.GetLength(0);
        int height = mel.GetLength(1);
        int width = mel.GetLength(2);

        float[,,] output = new float[batch, height, targetLength];

        for (int b = 0; b < batch; b++)
        {
            for (int h = 0; h < height; h++)
            {
                for (int t = 0; t < targetLength; t++)
                {
                    output[b, h, t] = (t < width) ? mel[b, h, t] : 0f;
                }
            }
        }

        return output;
    }
}
