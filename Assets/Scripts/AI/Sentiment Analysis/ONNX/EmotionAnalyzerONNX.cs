// EmotionAnalyzerONNX.cs
// 이 스크립트는 Unity에서 ONNX 모델을 사용하여 감정 분석을 수행하는 컴포넌트입니다.
// 입력: 형태소 인덱스 배열(int[]), 출력: 감정 라벨(string)

using System; // DateTime, TimeSpan, Action 등의 시스템 기능 사용
using System.Threading.Tasks; // async/await 비동기 처리를 위한 네임스페이스
using UnityEngine; // Unity의 기본 기능 (MonoBehaviour 등)
using Unity.InferenceEngine; // Unity AI Inference Engine 관련 클래스 (ModelLoader, Worker, Tensor 등)
using TMPro; // TextMeshPro UI 텍스트 출력을 위한 네임스페이스

public class EmotionAnalyzerONNX : MonoBehaviour
{
    public TextMeshProUGUI debugText; // 분석 상태 및 결과를 출력할 UI 텍스트 (Debug용)

    [Header("AI 감정 분석 ONNX 모델 (ai.onnx)")]
    public ModelAsset modelAsset; // Unity 에디터에서 드래그하여 설정할 ONNX 모델 파일

    private Worker worker; // 모델을 실행할 추론 엔진의 핵심 클래스 (CPU/GPU 백엔드 가능)

    [Header("ONNX 모델의 입출력 이름")]
    public string inputName = "input"; // ONNX 모델에서 입력 텐서 이름
    public string outputName = "output"; // ONNX 모델에서 출력 텐서 이름

    // ONNX 모델에서 예측한 결과값을 해석하기 위한 감정 라벨 배열
    // 모델 학습 시 사용된 라벨 순서와 반드시 일치해야 정확한 감정 결과 출력 가능
    private readonly string[] emotionLabels = new string[]
    {
        "불안", "슬픔", "당황", "기쁨", "분노", "상처"
    };

    private void Start()
    {
        // 모델 파일(modelAsset)을 메모리에 로드하여 실행 가능한 형식(runtimeModel)으로 변환
        var runtimeModel = ModelLoader.Load(modelAsset);

        // 로드된 모델을 기반으로 추론 워커(worker)를 생성 (BackendType: CPU 사용)
        worker = new Worker(runtimeModel, BackendType.CPU);
        // GPU 사용 시 BackendType.GPUCompute 등으로 변경 가능
    }

    /// <summary>
    /// 형태소 분석 결과인 인덱스 배열을 입력으로 받아 감정 분석을 실행
    /// </summary>
    /// <param name="inputIndices">예: [12, 430, 5, ...] 형태의 단어 인덱스 배열 (최대 길이 30)</param>
    /// <param name="onComplete">감정 분석 결과 문자열을 비동기적으로 반환하는 콜백 함수</param>
    public async void AnalyzeEmotion(int[] inputIndices, Action<string> onComplete)
    {
        /*
         * async(어싱크) : 비동기 메서드 지시어로서, "이 메서드는 기다릴 수 있어요" 라고 알려주는 표시(역할)
         * 목적 1. await(어웨잇)를 사용하기 위해
         * 목적 2. 메서드가 비동기적으로 작동한다는 신호(결과를 나중에 줄 수 도 있다는 약속)
         */
        DateTime ONNXStartTime = DateTime.Now; // 감정 분석 시작 시각 저장 (시간 측정용)

        // 입력 배열이 null이거나 비어있으면 실행 중단 및 경고 표시
        if (inputIndices == null || inputIndices.Length == 0)
        {
            Debug.LogWarning("입력 인덱스 배열이 비어 있습니다.");
            debugText.text = "입력 인덱스 배열이 비어 있습니다.";
            onComplete?.Invoke(null); // 콜백에 null 전달
            return;
        }

        // ONNX 입력 텐서의 모양 정의 (배치 크기 1, 시퀀스 길이: inputIndices.Length)
        var inputShape = new TensorShape(1, inputIndices.Length);
        /*
         * Tensor(텐서) : 인공지능과 딥러닝의 핵심 개념으로 숫자를 표처럼 담아두는 상자로 이해하면 됩니다.
         * 이 때, 담는 숫자는 1개("5" : 스칼라(0차원 텐서)), 여러 숫자([1, 2, 3] : 벡터(1차원 텐서)), 
         * 행과 열이 있는 표의 숫자([[1, 2], [3, 4]] : 행렬(2차원 텐서)), 그 이상의 숫자 또는 이미지 등([[[...]]] : 다차원 텐서)
         * 왜? AI는 사람이 이해하는 문장, 이미지, 소리 등을 숫자의 덩어리로 바꿔서 처리합니다.
         * 그 숫자의 덩어리를 담는게 바로 텐서입니다.
         * 
         * Unity에서는 AI 모델에 입력을 줄 때, 그리고 결과를 받을 때 모두 Tensor 형식으로 처리합니다.
         */

        // 입력 데이터를 담을 텐서 생성 (정수 타입)
        using var inputTensor = new Tensor<int>(inputShape);

        // 입력 배열의 각 값을 텐서에 복사
        for (int i = 0; i < inputIndices.Length; i++)
        {
            inputTensor[0, i] = inputIndices[i];
        }

        // 추론 엔진에 입력 설정 (SetInput 함수 사용)
        worker.SetInput(inputName, inputTensor);

        // 추론 예약 (Schedule 함수 호출 → 내부적으로 GPU/CPU에 작업 요청)
        worker.Schedule();

        // 추론이 끝나고 결과가 준비될 때까지 GPU 버퍼에서 CPU 메모리로 복사 (Readback)
        var outputRaw = worker.PeekOutput(outputName) as Tensor<float>; // 출력 텐서 참조 (float형)
        var outputTensor = await outputRaw.ReadbackAndCloneAsync(); // 비동기 복사 완료 대기
        /*
         * await(어웨잇) : 비동기 처리 대기 연산자, "이 작업 끝날 때까지 잠깐 기다릴께요"라는 기다림 명령어
         */

        // 출력된 감정 점수 중 가장 높은 값을 가진 인덱스를 찾음 (감정 분류)
        int bestIndex = 0; // 최고 점수 인덱스 저장용
        float bestScore = float.MinValue; // 최고 점수 초기값 설정

        for (int i = 0; i < emotionLabels.Length; i++)
        {
            float score = outputTensor[0, i]; // 각 감정에 대한 점수
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i; // 더 높은 점수를 발견하면 해당 인덱스 저장
            }
        }

        outputTensor.Dispose(); // 출력 텐서 메모리 해제 (수동 해제 필수)

        // 분석 결과(감정 문자열)를 콜백 함수로 전달
        onComplete?.Invoke(emotionLabels[bestIndex]);

        // 분석에 걸린 시간 측정 및 디버그 출력
        TimeSpan ONNXDuration = DateTime.Now - ONNXStartTime;
        debugText.text += $"\nONNX 소요시간: {ONNXDuration.TotalSeconds:F2}초";
    }

    private void OnDestroy()
    {
        // Unity 오브젝트가 삭제되거나 씬이 닫힐 때 워커 해제
        worker?.Dispose();
    }
}
