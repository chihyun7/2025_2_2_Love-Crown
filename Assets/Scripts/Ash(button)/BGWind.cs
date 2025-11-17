using UnityEngine;

public class BGWind : MonoBehaviour
{
    Transform tr;
    public float amplitude = 0.1f;   // 3D 공간 기준이므로 값 작게!
    public float speed = 0.5f;

    Vector3 startPos;

    void Awake()
    {
        tr = transform;  // 3D Transform 가져오기
    }

    void Start()
    {
        startPos = tr.localPosition;
    }

    void Update()
    {
        float offsetX = Mathf.Sin(Time.time * speed) * amplitude;
        float offsetY = Mathf.Cos(Time.time * speed * 0.8f) * amplitude * 0.3f;

        tr.localPosition = startPos + new Vector3(offsetX, offsetY, 0);
    }
}
