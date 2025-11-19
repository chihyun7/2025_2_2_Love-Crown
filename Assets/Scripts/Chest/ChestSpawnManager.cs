using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ChestSpawnManager : MonoBehaviourPun
{
    [Header("스폰할 상자 프리팹 (Photon용)")]
    public GameObject chestPrefab; // GiftChest 프리팹

    [Header("상자 스폰 포인트들")]
    public Transform[] spawnPoints;

    [Header("한 번에 존재할 상자 개수")]
    public int chestCount = 5;

    private void Start()
    {
        // 마스터만 스폰 관리
        if (!PhotonNetwork.IsMasterClient) return;

        if (chestPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ChestSpawnManager] 설정이 비어있음");
            return;
        }

        // 스폰 포인트 섞기
        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
            indices.Add(i);

        // 단순 셔플
        for (int i = 0; i < indices.Count; i++)
        {
            int swap = Random.Range(i, indices.Count);
            int tmp = indices[i];
            indices[i] = indices[swap];
            indices[swap] = tmp;
        }

        int spawnNum = Mathf.Min(chestCount, spawnPoints.Length);

        for (int i = 0; i < spawnNum; i++)
        {
            Transform p = spawnPoints[indices[i]];
            PhotonNetwork.Instantiate(chestPrefab.name, p.position, p.rotation);
        }
    }
}
