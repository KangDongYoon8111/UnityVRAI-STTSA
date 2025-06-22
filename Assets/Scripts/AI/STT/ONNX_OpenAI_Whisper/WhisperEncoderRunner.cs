// WhisperEncoderRunner.cs - 정확한 입력/출력 이름 기반 수정본
// input_features → last_hidden_state

using UnityEngine;
using Unity.InferenceEngine;
using System.Threading.Tasks;

public class WhisperEncoderRunner : MonoBehaviour
{
    [Header("ONNX 모델")]
    public ModelAsset encoderModelAsset;

    private Model encoderModel;
    private Worker encoderWorker;
    private BackendType backend;

    void Start()
    {
        // GPU 지원되면 GPUCompute, 아니면 CPU
        backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
        encoderModel = ModelLoader.Load(encoderModelAsset);
        encoderWorker = new Worker(encoderModel, backend);
    }

    public async Task<Tensor> RunEncoderAsync(Tensor melSpectrogram)
    {
        encoderWorker.SetInput("input_features", melSpectrogram);
        encoderWorker.Schedule();

        var output = encoderWorker.PeekOutput("last_hidden_state");

        // ✅ 출력 형태 확인 로그만 표시 (데이터 값 직접 접근은 차단되어 있음)
        Debug.Log($"[WhisperEncoder] 출력 Tensor shape: {output.shape}");

        // 실제 디코더에서 사용할 수 있도록 복제된 Tensor 반환
        return await output.ReadbackAndCloneAsync();
    }

    void OnDestroy()
    {
        encoderWorker?.Dispose();
    }
}