// MelSpectrogramGenerator.cs
// 기존 PCM 데이터를 Whisper용 Mel Spectrogram Tensor 형태로 변환

using System;
using System.Linq;
using UnityEngine;

// MathNet.Numerics 라이브러리 의존(설치 필요 : https://www.nuget.org/packages/MathNet.Numerics)
public static class MelSpectrogramGenerator
{
    public static float[,,] Generate(float[] pcmData, int sampleRate = 16000)
    {
        Normalize(ref pcmData);

        int frameSize = 400;
        int hopLength = 160;
        int frameCount = (pcmData.Length - frameSize) / hopLength + 1;

        float[][] melSpectrogram = new float[frameCount][];

        for (int i = 0; i < frameCount; i++)
        {
            float[] frame = new float[frameSize];
            Array.Copy(pcmData, i * hopLength, frame, 0, frameSize);

            ApplyHammingWindow(frame);

            float[] power = MelSpectrogramFFT.ComputePowerSpectrum(frame);
            float[] mel = MelSpectrogramFFT.ApplyMelFilter(power, sampleRate);

            // 로그 압축
            for (int j = 0; j < mel.Length; j++)
            {
                mel[j] = Mathf.Log10(mel[j] + 1e-10f);
            }

            melSpectrogram[i] = mel;
        }

        // [1, 80, frameCount] 형태로 변환
        float[,,] output = new float[1, 80, frameCount];
        for (int t = 0; t < frameCount; t++)
        {
            for (int m = 0; m < 80; m++)
            {
                output[0, m, t] = melSpectrogram[t][m];
            }
        }

        return output;
    }

    private static void Normalize(ref float[] data)
    {
        float max = data.Max(Mathf.Abs);
        if (max > 0f)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] /= max;
        }
    }

    private static void ApplyHammingWindow(float[] frame)
    {
        int N = frame.Length;
        for (int i = 0; i < N; i++)
        {
            frame[i] *= 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * i / (N - 1));
        }
    }
}
