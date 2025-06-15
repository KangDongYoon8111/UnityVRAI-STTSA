using UnityEngine;

[RequireComponent (typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = FindAnyObjectByType<SoundManager>();
            }
            return m_instance;
        }
    }
    private static SoundManager m_instance;

    private AudioSource audioSource;

    private void Awake()
    {
        if(instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource> ();
    }

    public void PlayFXSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}
