using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager instance;

    [Header("스폰 포인트")]
    public Transform[] spawnPoints;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 GetSpawnPosition(int playerIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다.");
            return Vector3.zero;
        }

        int index = ((playerIndex % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;

        if (spawnPoints[index] == null)
        {
            Debug.LogWarning($"스폰 포인트 {index}가 null입니다. 기본 위치 반환");
            return Vector3.zero;
        }

        return spawnPoints[index].position;
    }

    public void SetSpawnPoints(Transform[] newSpawnPoints)
    {
        if (newSpawnPoints == null || newSpawnPoints.Length == 0)
        {
            Debug.LogWarning("새로운 스폰 포인트가 없거나 null입니다. 기존 포인트 유지");
            return;
        }

        spawnPoints = newSpawnPoints;
        Debug.Log($"스폰 포인트가 {spawnPoints.Length}개로 갱신되었습니다.");
    }
}