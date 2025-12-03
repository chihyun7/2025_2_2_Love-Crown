using UnityEngine;
using Photon.Pun;

public class FootstepLoop : MonoBehaviourPun
{
    public AudioClip[] clips;
    public float interval = 0.45f;

    private float timer;
    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (rb == null) return;

        // 움직이고 있을 때만 소리 재생
        if (rb.velocity.magnitude > 0.1f)
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                PlayStep();
                timer = 0;
            }
        }
    }

    void PlayStep()
    {
        if (clips.Length == 0) return;
        int idx = Random.Range(0, clips.Length);
        audioSource.PlayOneShot(clips[idx]);
    }
}
