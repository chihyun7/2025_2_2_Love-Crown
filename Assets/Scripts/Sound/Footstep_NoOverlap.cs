using UnityEngine;
using Photon.Pun;

public class Footstep_NoOverlap : MonoBehaviourPun
{
    public AudioClip[] clips;
    public float walkInterval = 0.45f;
    public float runInterval = 0.30f;

    private float timer = 0f;
    private AudioSource audioSource;
    private PlayerMove move;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        move = GetComponent<PlayerMove>();
        audioSource.loop = false; // 중복 방지
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isMoving = (h != 0 || v != 0);

        // 멈추면 즉시 발소리 중단
        if (!isMoving)
        {
            timer = 0f;
            audioSource.Stop();
            return;
        }

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float interval = isRunning ? runInterval : walkInterval;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            PlayStep();
            timer = 0f;
        }
    }

    void PlayStep()
    {
        if (clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        audioSource.clip = clips[index];

        // 이미 재생 중이면 다시 재생 X (중첩 방지)
        if (!audioSource.isPlaying)
            audioSource.Play();
    }
}
