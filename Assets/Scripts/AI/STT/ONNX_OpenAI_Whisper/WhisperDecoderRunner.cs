// WhisperDecoderRunner.cs - MiniJSON 기반 버전
// vocab.json 파싱을 MiniJSON으로 처리하여 안정성 확보
// MiniJSON 의존성 : https://gist.github.com/darktable/1411710

using UnityEngine;
using Unity.InferenceEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WhisperDecoderRunner : MonoBehaviour
{
    [Header("ONNX 모델")]
    public ModelAsset decoderModelAsset;

    [Header("단어 사전 (vocab2.json)")]
    public TextAsset vocabJsonAsset;

    private Model decoderModel;
    private Worker decoderWorker;
    private Dictionary<int, string> vocab;
    private BackendType backend;

    private const int StartToken = 50257;
    private const int EndToken = 50256;
    private const int MaxTokens = 100;

    void Start()
    {
        try
        {
            backend = SystemInfo.supportsComputeShaders ? BackendType.GPUCompute : BackendType.CPU;
            decoderModel = ModelLoader.Load(decoderModelAsset);
            decoderWorker = new Worker(decoderModel, backend);

            if (vocabJsonAsset == null)
            {
                Debug.LogError("🚨 vocabJsonAsset 파일이 연결되지 않았습니다.");
                return;
            }

            vocab = ParseReversedVocabJson(vocabJsonAsset.text);
            if (vocab == null || vocab.Count == 0)
            {
                Debug.LogError("🚨 vocab.json 파싱에 실패했습니다.");
                return;
            }

            Debug.Log($"[Vocab] 항목 수: {vocab.Count}");
            if (!vocab.ContainsKey(50258)) Debug.LogWarning("❌ vocab에 50258 없음!");
            if (!vocab.ContainsKey(50362)) Debug.LogWarning("❌ vocab에 50362 없음!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ WhisperDecoderRunner 초기화 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public async Task<string> RunDecoderAsync(Tensor encoderOutput)
    {
        List<int> tokenIds = new();
        int lastToken = StartToken;

        for (int i = 0; i < MaxTokens; i++)
        {
            decoderWorker.SetInput("encoder_hidden_states", encoderOutput);
            decoderWorker.SetInput("input_ids", new Tensor<int>(new TensorShape(1, 1), new int[] { lastToken }));
            decoderWorker.Schedule();

            Tensor logits = decoderWorker.PeekOutput("logits");

            if (logits is Tensor<float> logitTensor)
            {
                var readback = await logitTensor.ReadbackAndCloneAsync() as Tensor<float>;
                float[] logitsArray = new float[readback.shape.length];
                for (int j = 0; j < logitsArray.Length; j++)
                    logitsArray[j] = readback[j];

                float[] preview = new float[Mathf.Min(10, logitsArray.Length)];
                Array.Copy(logitsArray, preview, preview.Length);
                Debug.Log($"[Decoder] logits 미리보기: {string.Join(", ", preview)}");

                int nextToken = GetArgMax(logitsArray);
                string vocabStr = vocab.TryGetValue(nextToken, out var str) ? str : "❓(미매핑)";
                Debug.Log($"[Decoder Token {i}] ID: {nextToken}, 문자: {vocabStr}");

                if (nextToken == EndToken)
                    break;

                tokenIds.Add(nextToken);
                lastToken = nextToken;
            }
        }

        return DecodeTokens(tokenIds);
    }

    private int GetArgMax(float[] values)
    {
        int maxIndex = 0;
        float maxValue = float.MinValue;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > maxValue)
            {
                maxValue = values[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    private string DecodeTokens(IEnumerable<int> ids)
    {
        string result = "";
        foreach (var id in ids)
        {
            if (vocab.TryGetValue(id, out var s))
                result += s;
            else
            {
                Debug.LogWarning($"❗ vocab에 없음: {id}");
                result += "?";
            }
        }
        return result;
    }

    /// <summary>
    /// vocab2.json 형식 ("문자" → ID)을 뒤집어서 ("ID" → 문자)로 변환
    /// </summary>
    private Dictionary<int, string> ParseReversedVocabJson(string jsonText)
    {
        var reversed = new Dictionary<int, string>();
        var raw = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
        if (raw == null) return reversed;

        foreach (var kv in raw)
        {
            if (kv.Value is long || kv.Value is int)
            {
                int id = Convert.ToInt32(kv.Value);
                if (!reversed.ContainsKey(id))
                    reversed[id] = kv.Key;
            }
        }

        // 보정: 누락된 언어 토큰 수동 삽입
        if (!reversed.ContainsKey(50258)) reversed[50258] = "<|en|>";
        if (!reversed.ContainsKey(50362)) reversed[50362] = "<|ko|>";

        return reversed;
    }

    void OnDestroy()
    {
        decoderWorker?.Dispose();
    }
}