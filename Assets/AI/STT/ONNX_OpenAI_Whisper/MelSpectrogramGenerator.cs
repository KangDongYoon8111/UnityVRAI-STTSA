///* 
// * ✅ 왜 MelSpectrogram 변환 클래스가 필요한가요?
// * Whisper 모델은 "WAV 파일을 직접 처리하지 않습니다."
// * 대신, WAV → PCM(float[]) → Mel Spectrogram → Tensor 입력 과정을 필요로 합니다.
// * 
// * 🔧 그렇다면 WAV → MelSpectrogram을 왜 해야 하나?
// * 마이크 데이터는 Unity에서는 WAV로 저장하는 것이 가장 쉽고 안정적입니다.
// * 하지만 Whisper는 그 WAV 안의 음파를 Mel Spectrogram으로 수학적으로 가공한 것만 사용합니다.
// * 따라서 Unity 내에서 WAV → Mel 변환을 해주는 스크립트가 반드시 필요합니다.
// * 
// * ✔️ 주요 기능
// * 1. WAV 파일을 읽어 PCM float[]로 변환
// * 2. 25ms 프레임, 10ms 간격으로 나눠 STFT(단기 푸리에 변환) 수행
// * 3. 80개 Mel 필터 적용 → (80, N) 스펙트로그램 생성
// * 4. TensorFloat로 감싸 Whisper 모델 입력으로 변환
// */

//using System;
//using System.IO;
//using System.Numerics;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Sentis;

//public class MelSpectrogramGenerator
//{
//    /// <summary>
//    /// 1단계: WAV -> float[]
//    /// </summary>

//    // Whisper 기본 사양
//    const int SAMPLE_RATE = 16000;

//    /// <summary>
//    /// WAV 파일을 읽어서 PCM float[] 배열로 변환합니다.
//    /// (16-bit PCM mono WAV만 지원)
//    /// </summary>
//    public static float[] LoadWavAsPcm(string filePath)
//    {
//        if (!File.Exists(filePath))
//        {
//            Debug.LogError($"WAV 파일을 찾을 수 없습니다: {filePath}");
//            UIDebugController.instance.DebugLog($"WAV 파일을 찾을 수 없습니다: {filePath}");
//            return null;
//        }

//        byte[] wav = File.ReadAllBytes(filePath);

//        // WAV 헤더 파싱
//        int channels = BitConverter.ToInt16(wav, 22); // 채널 수
//        int sampleRate = BitConverter.ToInt32(wav, 24); // 샘플링 레이트
//        int bitsPerSample = BitConverter.ToInt16(wav, 34); // 비트 깊이

//        if (bitsPerSample != 16)
//        {
//            Debug.LogError("16-bit PCM WAV만 지원됩니다.");
//            UIDebugController.instance.DebugLog("16-bit PCM WAV만 지원됩니다.");
//            return null;
//        }

//        if (channels != 1)
//        {
//            Debug.LogWarning("현재는 mono 파일만 지원됩니다. stereo는 추후 확장 필요.");
//            UIDebugController.instance.DebugLog("현재는 mono 파일만 지원됩니다. stereo는 추후 확장 필요.");
//            return null;
//        }

//        if (sampleRate != SAMPLE_RATE)
//        {
//            Debug.LogWarning($"샘플링레이트가 {SAMPLE_RATE}Hz가 아닙니다. STT 정확도가 떨어질 수 있습니다.");
//            UIDebugController.instance.DebugLog($"샘플링레이트가 {SAMPLE_RATE}Hz가 아닙니다. STT 정확도가 떨어질 수 있습니다.");
//        }

//        // PCM 데이터 추출
//        int dataStartIndex = 44; // WAV 헤더 크기
//        int sampleCount = (wav.Length - dataStartIndex) / 2; // 2 bytes per sample (16-bit)
//        float[] samples = new float[sampleCount];

//        for (int i = 0; i < sampleCount; i++)
//        {
//            short sample = BitConverter.ToInt16(wav, dataStartIndex + i * 2);
//            samples[i] = sample / 32768f; // 16-bit 정규화
//        }

//        return samples;
//    }



//    /// <summary>
//    /// 2단계: STFT + Hamming
//    /// </summary>

//    const int N_FFT = 400;         // 25ms
//    const int HOP_LENGTH = 160;    // 10ms

//    /// <summary>
//    /// Hamming 윈도우를 적용한 STFT 결과 (복소수 스펙트럼)를 생성합니다.
//    /// </summary>
//    public static List<Complex[]> ComputeSTFT(float[] pcm)
//    {
//        int totalFrames = 1 + (pcm.Length - N_FFT) / HOP_LENGTH;
//        List<Complex[]> stftFrames = new List<Complex[]>(totalFrames);

//        float[] window = GenerateHammingWindow(N_FFT);

//        for (int frame = 0; frame < totalFrames; frame++)
//        {
//            int startIdx = frame * HOP_LENGTH;
//            float[] frameSamples = new float[N_FFT];

//            for (int i = 0; i < N_FFT; i++)
//            {
//                frameSamples[i] = pcm[startIdx + i] * window[i];
//            }

//            // FFT 적용
//            Complex[] spectrum = FFT(frameSamples);
//            stftFrames.Add(spectrum);
//        }

//        return stftFrames;
//    }

//    /// <summary>
//    /// Hamming 윈도우 생성
//    /// </summary>
//    private static float[] GenerateHammingWindow(int size)
//    {
//        float[] window = new float[size];
//        for (int i = 0; i < size; i++)
//        {
//            window[i] = 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * i / (size - 1));
//        }
//        return window;
//    }

//    /// <summary>
//    /// 실수형 배열에 대한 FFT (Zero-padding 없음)
//    /// </summary>
//    private static Complex[] FFT(float[] frame)
//    {
//        int n = frame.Length;
//        Complex[] input = new Complex[n];
//        for (int i = 0; i < n; i++)
//        {
//            input[i] = new Complex(frame[i], 0);
//        }

//        // Cooley-Tukey FFT (Recursive)
//        return RecursiveFFT(input);
//    }

//    private static Complex[] RecursiveFFT(Complex[] x)
//    {
//        int N = x.Length;
//        if (N <= 1)
//            return new Complex[] { x[0] };

//        Complex[] even = new Complex[N / 2];
//        Complex[] odd = new Complex[N / 2];

//        for (int i = 0; i < N / 2; i++)
//        {
//            even[i] = x[2 * i];
//            odd[i] = x[2 * i + 1];
//        }

//        Complex[] Feven = RecursiveFFT(even);
//        Complex[] Fodd = RecursiveFFT(odd);

//        Complex[] spectrum = new Complex[N];
//        for (int k = 0; k < N / 2; k++)
//        {
//            Complex t = Complex.FromPolarCoordinates(1, -2 * Math.PI * k / N) * Fodd[k];
//            spectrum[k] = Feven[k] + t;
//            spectrum[k + N / 2] = Feven[k] - t;
//        }

//        return spectrum;
//    }



//    /// <summary>
//    /// 3단계: Mel 필터 적용 + log-mel 스펙트로그램 생성
//    /// </summary>

//    const int N_MELS = 80;
//    const int MEL_MIN_HZ = 0;
//    const int MEL_MAX_HZ = 8000;

//    // 전체 흐름을 처리하는 메인 함수
//    public static Tensor Generate(string wavPath)
//    {
//        float[] pcm = LoadWavAsPcm(wavPath);
//        if (pcm == null || pcm.Length == 0)
//            return null;

//        var stft = ComputeSTFT(pcm);
//        float[,] melSpec = ApplyMelFilterbank(stft);
//        return ToTensor(melSpec);
//    }

//    /// <summary>
//    /// Mel 필터뱅크 적용 + log-mel 변환
//    /// 출력: [n_mels, n_frames]
//    /// </summary>
//    private static float[,] ApplyMelFilterbank(List<Complex[]> stft)
//    {
//        int nFrames = stft.Count;
//        int nFftBins = N_FFT / 2 + 1;

//        float[,] melSpec = new float[N_MELS, nFrames];

//        float[][] melFilters = CreateMelFilterbank();

//        for (int t = 0; t < nFrames; t++)
//        {
//            // 파워 스펙트럼 계산 (|X|^2)
//            float[] power = new float[nFftBins];
//            for (int i = 0; i < nFftBins; i++)
//            {
//                power[i] = (float)(Math.Pow(stft[t][i].Real, 2) + Math.Pow(stft[t][i].Imaginary, 2));
//            }

//            // Mel 필터 적용
//            for (int m = 0; m < N_MELS; m++)
//            {
//                float sum = 0f;
//                for (int k = 0; k < nFftBins; k++)
//                {
//                    sum += melFilters[m][k] * power[k];
//                }
//                melSpec[m, t] = Mathf.Log10(sum + 1e-10f); // log-mel
//            }
//        }

//        return melSpec;
//    }

//    /// <summary>
//    /// Whisper에 맞는 Mel 필터 뱅크 생성 (shape: [n_mels][n_fft/2+1])
//    /// </summary>
//    private static float[][] CreateMelFilterbank()
//    {
//        int nFftBins = N_FFT / 2 + 1;

//        float melMin = HertzToMel(MEL_MIN_HZ);
//        float melMax = HertzToMel(MEL_MAX_HZ);
//        float melStep = (melMax - melMin) / (N_MELS + 1);

//        float[] melPoints = new float[N_MELS + 2];
//        for (int i = 0; i < melPoints.Length; i++)
//            melPoints[i] = melMin + melStep * i;

//        float[] hzPoints = new float[melPoints.Length];
//        for (int i = 0; i < hzPoints.Length; i++)
//            hzPoints[i] = MelToHertz(melPoints[i]);

//        int[] bin = new int[hzPoints.Length];
//        for (int i = 0; i < hzPoints.Length; i++)
//            bin[i] = Mathf.FloorToInt((N_FFT + 1) * hzPoints[i] / SAMPLE_RATE);

//        float[][] filterbank = new float[N_MELS][];
//        for (int m = 0; m < N_MELS; m++)
//        {
//            filterbank[m] = new float[nFftBins];

//            int start = bin[m];
//            int center = bin[m + 1];
//            int end = bin[m + 2];

//            for (int k = start; k < center && k < nFftBins; k++)
//                filterbank[m][k] = (k - start) / (float)(center - start);
//            for (int k = center; k < end && k < nFftBins; k++)
//                filterbank[m][k] = (end - k) / (float)(end - center);
//        }

//        return filterbank;
//    }

//    private static float HertzToMel(float hz)
//    {
//        return 2595f * Mathf.Log10(1 + hz / 700f);
//    }

//    private static float MelToHertz(float mel)
//    {
//        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1);
//    }

//    /// <summary>
//    /// float[,] → TensorFloat (1, 80, N) 변환
//    /// </summary>
//    private static TensorFloat ToTensor(float[,] mel)
//    {
//        int mels = mel.GetLength(0);    // 80
//        int frames = mel.GetLength(1);  // 예: 3000

//        float[] data = new float[mels * frames];
//        for (int t = 0; t < frames; t++)
//        {
//            for (int m = 0; m < mels; m++)
//            {
//                data[t * mels + m] = mel[m, t]; // Transposed
//            }
//        }

//        TensorShape shape = new TensorShape(1, mels, frames); // Whisper expects (1, 80, N)
//        return tensor;
//    }
//}
