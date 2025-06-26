using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicPlayer1;
    [SerializeField] private AudioClip musicPlayer2;

    private void Awake()
    {
        // Garantir que só exista um MusicManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayMusicForPlayer(int playerIndex)
    {
        if (audioSource == null) return;

        audioSource.Stop();

        audioSource.clip = (playerIndex == 0) ? musicPlayer1 : musicPlayer2;
        audioSource.Play();
    }
}