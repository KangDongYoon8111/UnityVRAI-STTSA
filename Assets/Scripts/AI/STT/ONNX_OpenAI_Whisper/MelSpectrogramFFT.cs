// MelSpectrogramFFT.cs
// MathNet.Numerics 기반 FFT + Mel 필터 구현
// MathNet.Numerics 라이브러리 의존(설치 필요 : https://www.nuget.org/packages/MathNet.Numerics)

using System;
using System.Linq;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using UnityEngine;

public static class MelSpectrogramFFT
{
    // FFT -> Power Spectrum
    public static float[] ComputePowerSpectrum(float[] frame)
    {
        int N = frame.Length;

        // 1. 실수 입력을 복소수 배열로 변환
        Complex[] fftBuffer = new Complex[N];
        for (int i = 0; i < N; i++)
        {
            fftBuffer[i] = new Complex(frame[i], 0); // 허수부는 0
        }

        // 2. FFT 실행 (in-place)
        Fourier.Forward(fftBuffer, FourierOptions.Matlab);

        // 3. 절댓값 제곱 (Power Spectrum)
        float[] power = new float[N / 2];
        for (int i = 0; i < N / 2; i++)
        {
            power[i] = (float)(fftBuffer[i].Magnitude * fftBuffer[i].Magnitude);
        }

        return power;
    }

    // Mel 필터 적용
    public static float[] ApplyMelFilter(float[] powerSpec, int sampleRate, int melFilterCount = 80, int fftSize = 400)
    {
        // 1. FFT Bin → 실제 주파수 (Hz)
        float freqResolution = (float)sampleRate / fftSize;

        // 2. Mel 기준점 설정
        float melMin = HzToMel(0);
        float melMax = HzToMel(sampleRate / 2);

        float[] melPoints = new float[melFilterCount + 2];
        for (int i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = MelToHz(melMin + (melMax - melMin) * i / (melFilterCount + 1));
        }

        int[] binPoints = melPoints.Select(f => Mathf.FloorToInt(f / freqResolution)).ToArray();

        // 3. 삼각 필터 적용
        float[] melEnergies = new float[melFilterCount];

        for (int m = 1; m <= melFilterCount; m++)
        {
            float energy = 0;
            for (int k = binPoints[m - 1]; k < binPoints[m]; k++)
            {
                energy += (float)(powerSpec[k] * (k - binPoints[m - 1]) / (binPoints[m] - binPoints[m - 1]));
            }
            for (int k = binPoints[m]; k < binPoints[m + 1]; k++)
            {
                energy += (float)(powerSpec[k] * (binPoints[m + 1] - k) / (binPoints[m + 1] - binPoints[m]));
            }
            melEnergies[m - 1] = energy;
        }

        return melEnergies;
    }

    // 주파수(Hz) → Mel 변환
    private static float HzToMel(float hz)
    {
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    // Mel → 주파수(Hz) 변환
    private static float MelToHz(float mel)
    {
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }
}
