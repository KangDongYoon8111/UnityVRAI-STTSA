using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {
    public float speed = 8f; // 총알 이동 속력
    public AudioClip hitClip;
    public AudioClip attackClip;
    public AudioClip shieldClip;
    private Rigidbody bulletRigidbody; // 이동에 사용할 리지드바디 컴포넌트

    void Start() {
        // 게임 오브젝트에서 Rigidbody 컴포넌트를 찾아 bulletRigidbody에 할당
        bulletRigidbody = GetComponent<Rigidbody>();
        GetComponent<AudioSource>().Play();
        // 리지드바디의 속도 = 앞쪽 방향 * 이동 속력
        bulletRigidbody.linearVelocity = transform.forward * speed;

        // 3초 뒤에 자신의 게임 오브젝트 파괴
        Destroy(gameObject, 5f);
    }

    // 트리거 충돌시 자동으로 실행되는 메서드
    void OnTriggerEnter(Collider other) {
        // 충돌한 상대방 게임 오브젝트가 Player 태그를 가진 경우
        if (other.tag == "Player")
        {
            // 상대방 게임 오브젝트에서 PlayerController 컴포넌트를 가져오기
            PlayerController playerController
                = other.GetComponent<PlayerController>();

            // 상대방으로부터 PlayerController 컴포넌트를 가져오는대 성공했다면
            if (playerController != null)
            {
                // 상대방 PlayerController 컴포넌트의 Die() 메서드 실행
                playerController.Die();
            }
        }

        switch (other.tag)
        {
            case "Player":
                GameManager.instance.Damage();
                SoundManager.instance.PlayFXSound(hitClip);
                Destroy(gameObject);
                break;
            case "Sword":
                GameManager.instance.AddScore();
                SoundManager.instance.PlayFXSound(attackClip);
                Destroy(gameObject);
                break;
            case "Shield":
                SoundManager.instance.PlayFXSound(shieldClip);
                Destroy(gameObject);
                break;
        }
    }
}