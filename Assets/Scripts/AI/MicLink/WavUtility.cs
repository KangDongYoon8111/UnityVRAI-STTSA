using System;
using System.IO;
using UnityEngine;

// 본 클래스는 Unity에서 만든 오디오(AudioClip)를 WAV 파일로 저장하는 유틸리티
public static class WavUtility
{
    const int HEADER_SIZE = 44; // WAV 파일 앞부분에 들어가는 헤더의 크기(고정값: 44바이트)
    /* 
     * Header(헤더)란? 파일의 시작 부분에 위치하는 정보 부분.
     * 본 정보는 파일의 본문(실제 오디오 데이터)를 올바르게 읽고 재생하기 위해 반드시 필요
     * 헤더에는 파일형식, 오디오 형식, 채널 수, 샘플링 주파수, 비트당 샘플 수, 전체 데이터 크기 등이 포함됨
     * 즉, 44바이트의 헤더 공간을 미리 확보해놓기 위한 조치로 
     * WAV 파일 표준 규격으로 44바이트 공간
     */

    // AudioClip을 WAV 형식으로 저장하는 메소드
    public static void Save(AudioClip clip, string filepath)
    {
        // 전달받은 오디오가 없는 경우 예외처리(에러를 발생)
        if (clip == null) throw new ArgumentNullException("clip");

        // 파일 경로가 비어있거나 없는 경우 예외처리(에러를 발생)
        if (string.IsNullOrEmpty(filepath)) throw new ArgumentNullException("filepath");

        // 파일을 저장할 폴더가 없으면 새로 생성
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        // 빈 WAV 파일을 먼저 만들고 오디오 데이터를 채우기
        using (var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip); // 실제 오디오 데이터를 파일에 쓰기
            WriteHeader(fileStream, clip); // WAV 파일의 헤더를 작성
        }

        /*
         * using 키워드의 역할
         * 1. using System.IO; 처럼 라이브러리를 가져와 사용할 때 사용
         * 2. 위 처럼 자원을 안전하게 관리하기 위해 사용
         * using 블록 안에 선언된 변수(위 fileStream)는 파일이나 네트워크 등 자원을 사용하는 객체로
         * 해당 자원을 사용한 후에는 반드시 닫아주거나 메모리에서 해제해주는 과정이 필요하다.
         * 이럴 때 자동으로 자원을 정리해주는 기능이 using 키워드의 두 번째 용도
         */

        /*
         * using 키워드 대체 형식
         * var fileStream = CreateEmpty(filepath);
         * ConvertAndWrite(fileStream, clip);
         * WriteHeader(fileStream, clip);
         * fileStream.Dispose(); // 반드시 호출해야 하지만 잊기 쉬움
         */
    }

    // WAV 파일을 먼저 빈 상태로 생성
    private static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create); // 파일을 생성
        byte emptyByte = new byte(); // 빈 바이트 값(0)

        // WAV 파일 헤더를 위한 공간을 미리 0으로 채워놓기
        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream; // 만든 빈 파일을 돌려주기
    }

    // Unity 오디오 데이터를 WAV 형식의 데이터로 변환하여 파일에 기록
    private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        float[] samples = new float[clip.samples]; // 오디오 데이터를 저장할 배열
        clip.GetData(samples, 0); // AudioClip에서 데이터를 배열로 가져옵니다.

        Int16[] intData = new Int16[samples.Length]; // PCM 16비트 정수 데이터를 위한 배열
        Byte[] bytesData = new Byte[samples.Length * 2]; // 16비트는 2바이트이므로 크기 설정

        const float rescaleFactor = 32767; // float 값을 정수로 변환할 때 사용하는 스케일

        // 오디오 데이터를 하나씩 처리해서 WAV 파일 형식에 맞는 바이트로 변환
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor); // float에서 16비트 정수로 변환
            byte[] byteArr = BitConverter.GetBytes(intData[i]); // 정수 데이터를 바이트로 변환
            byteArr.CopyTo(bytesData, i * 2); // 바이트 배열에 복사
        }

        // 바이트 배열에 변환된 데이터를 파일에 쓰기
        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    // WAV 파일의 앞부분(헤더)에 필요한 정보를 작성
    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency; // 오디오의 샘플링 속도(초당 샘플 개수)
        var channels = clip.channels; // 오디오 채널 수(예: 1은 모노, 2는 스테레오)
        var samples = clip.samples; // 전체 오디오 데이터 샘플의 개수

        fileStream.Seek(0, SeekOrigin.Begin); // 파일의 처음으로 이동

        // WAV 파일을 구성하는 헤더 정보를 차례로 작성
        // 파일 형식(RIFF)
        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        // 파일 크기 
        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        // WAV 파일임을 표시
        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        // 포맷 부분 시작
        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        // PCM 포맷 크기(항상 16)
        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        ushort audioFormat = 1;
        ushort numChannels = (ushort)channels;
        byte[] audioFormatBytes = BitConverter.GetBytes(audioFormat);
        byte[] numChannelsBytes = BitConverter.GetBytes(numChannels);
        fileStream.Write(audioFormatBytes, 0, 2); // 오디오 형식(PCM)
        fileStream.Write(numChannelsBytes, 0, 2); // 채널 수

        // 샘플링 속도
        byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        // 초당 데이터 크기
        byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        // 블록 크기
        ushort blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        // 비트 수(16비트)
        ushort bitsPerSample = 16;
        fileStream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

        // 데이터 부분 시작
        byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        // 데이터 전체 길이
        byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
