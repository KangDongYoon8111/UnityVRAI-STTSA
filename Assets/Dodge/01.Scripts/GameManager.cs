using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI 관련 라이브러리
using UnityEngine.SceneManagement;
using TMPro; // 씬 관리 관련 라이브러리

public class GameManager : MonoBehaviour 
{
    public static GameManager instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = FindAnyObjectByType<GameManager>();
            }
            return m_instance;
        }
    }

    private static GameManager m_instance;

    //public GameObject gameoverText; // 게임오버시 활성화 할 텍스트 게임 오브젝트
    //public Text timeText; // 생존 시간을 표시할 텍스트 컴포넌트
    //public Text recordText; // 최고 기록을 표시할 텍스트 컴포넌트

    private float surviveTime; // 생존 시간
    public bool isGameover; // 게임 오버 상태

    // 신규추가
    public GameObject countPanel;
    public GameObject mainPanel;
    public GameObject gameoverPanel;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI totalScoreText;
    public GameObject swordsRack;
    public GameObject[] bulletSpawners;

    private bool isGameStart = false;
    private int count = 5;
    private int score = 0;
    private int hp = 3;

    private void Awake()
    {
        if(instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start() {
        // 생존 시간과 게임 오버 상태를 초기화
        surviveTime = 0;
        isGameover = false;
        isGameStart = false;
        count = 5;
        score = 0;
        hp = 3;
    }

    void Update() 
    {
        if (!isGameStart) return;

        // 게임 오버가 아닌 동안
        if (!isGameover)
        {
            // 생존 시간 갱신
            surviveTime += Time.deltaTime;
            // 갱신한 생존 시간을 timeText 텍스트 컴포넌트를 통해 표시
            timeText.text = "Time : " + (int)surviveTime;
        }
        else
        {
            mainPanel.SetActive(false);
            gameoverPanel.SetActive(true);
            totalScoreText.text = "score : " + score;
        }
    }

    // 현재 게임을 게임 오버 상태로 변경하는 메서드
    //public void EndGame() {
    //    // 현재 상태를 게임 오버 상태로 전환
    //    isGameover = true;
    //    // 게임 오버 텍스트 게임 오브젝트를 활성화
    //    gameoverText.SetActive(true);

    //    // BestTime 키로 저장된, 이전까지의 최고 기록 가져오기
    //    float bestTime = PlayerPrefs.GetFloat("BestTime");

    //    // 이전까지의 최고 기록보다 현재 생존 시간이 더 크다면
    //    if (surviveTime > bestTime)
    //    {
    //        // 최고 기록의 값을 현재 생존 시간의 값으로 변경 
    //        bestTime = surviveTime;
    //        // 변경된 최고 기록을 BestTime 키로 저장
    //        PlayerPrefs.SetFloat("BestTime", bestTime);
    //    }

    //    // 최고 기록을 recordText 텍스트 컴포넌트를 통해 표시
    //    recordText.text = "Best Time: " + (int) bestTime;
    //}

    //public void GameCount()
    //{
    //    while (!isGameStart)
    //    {
    //        Debug.Log("While In!");
    //        Debug.Log("Count : " + count);
    //        float currentTime = Time.time;
    //        count -= Time.deltaTime;
    //        countText.text = ((int)count).ToString();

    //        if (count <= 0)
    //        {
    //            isGameStart = true;
    //            GameStart();
    //        }
    //    }        
    //}

    public void CountDownStart()
    {
        countPanel.SetActive(true);
        StartCoroutine(GameCount());
    }

    IEnumerator GameCount()
    {
        TextMeshProUGUI countText = countPanel.GetComponentInChildren<TextMeshProUGUI>();

        for (int i = count; i >= 0; i--)
        {
            countText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        GameStart();
    }

    public void GameStart()
    {
        swordsRack.SetActive(false);
        countPanel.SetActive(false);
        mainPanel.SetActive(true);
        isGameStart = true;
        Debug.Log("Game Start!");
        
        foreach(GameObject i in bulletSpawners)
        {
            i.SetActive(true);
        }
    }

    public void AddScore()
    {
        scoreText.text = $"score : {++score}";
    }

    public void Damage()
    {
        hpText.text = $"HP : {--hp}";

        if(hp <= 0)
        {
            isGameover = true;
        }
    }

    public void ReStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}