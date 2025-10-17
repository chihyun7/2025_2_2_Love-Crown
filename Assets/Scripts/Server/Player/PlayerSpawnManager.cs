using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager instance;

    [Header("���� ����Ʈ")]
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
            Debug.LogError("���� ����Ʈ�� �������� �ʾҽ��ϴ�.");
            return Vector3.zero;
        }

        int index = ((playerIndex % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;

        if (spawnPoints[index] == null)
        {
            Debug.LogWarning($"���� ����Ʈ {index}�� null�Դϴ�. �⺻ ��ġ ��ȯ");
            return Vector3.zero;
        }

        return spawnPoints[index].position;
    }

    public void SetSpawnPoints(Transform[] newSpawnPoints)
    {
        if (newSpawnPoints == null || newSpawnPoints.Length == 0)
        {
            Debug.LogWarning("���ο� ���� ����Ʈ�� ���ų� null�Դϴ�. ���� ����Ʈ ����");
            return;
        }

        spawnPoints = newSpawnPoints;
        Debug.Log($"���� ����Ʈ�� {spawnPoints.Length}���� ���ŵǾ����ϴ�.");
    }
}