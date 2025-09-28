using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager instance;

    [Header("스폰 포인트")]
    public Transform[] spawnPoints;

    private void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); // 중복 제거
        }
    }

    /// <summary>
    /// 플레이어 인덱스에 맞는 안전한 스폰 위치 반환
    /// </summary>
    /// <param name="playerIndex">플레이어 인덱스 (음수도 가능)</param>
    /// <returns>스폰 위치(Vector3)</returns>
    public Vector3 GetSpawnPosition(int playerIndex)
    {
        // 스폰 포인트 배열 체크
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("스폰 포인트가 설정되지 않았습니다.");
            return Vector3.zero;
        }

        // 음수 인덱스 안전 처리
        int index = ((playerIndex % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;

        // 해당 인덱스가 null인지 확인
        if (spawnPoints[index] == null)
        {
            Debug.LogWarning($"스폰 포인트 {index}가 null입니다. 기본 위치 반환");
            return Vector3.zero;
        }

        return spawnPoints[index].position;
    }

    /// <summary>
    /// 씬 전환 후 스폰 포인트 갱신 가능
    /// </summary>
    /// <param name="newSpawnPoints">새로운 스폰 포인트 배열</param>
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