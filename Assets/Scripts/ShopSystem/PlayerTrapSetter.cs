using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PlayerTrapSetter : MonoBehaviourPun
{
    [Header("함정 프리팹 이름 (Resources 폴더)")]
    public string fallTrapName = "BananaTrap"; // 넘어짐 함정
    public string slowTrapName = "SlowTrap";   // 이속 감소 함정

    [Header("설정")]
    public float trapCooldown = 35f; // 쿨타임

    private bool canUseSkill2 = true;
    private bool canUseSkill3 = true;

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (canUseSkill2)
            {
                SpawnTrap(fallTrapName, 2);
            }
            else
            {
                Debug.Log("2번 스킬 쿨타임 중입니다.");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (canUseSkill3)
            {
                SpawnTrap(slowTrapName, 3);
            }
            else
            {
                Debug.Log("3번 스킬 쿨타임 중입니다.");
            }
        }
    }

    void SpawnTrap(string prefabName, int skillNum)
    {
        Vector3 spawnPos = transform.position - (transform.forward * 2.0f);
        spawnPos.y += 0.1f;

        GameObject trapObj = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
        Debug.Log($"함정 설치 완료: {prefabName}");

        StartCoroutine(CooldownRoutine(skillNum));
        if (UIManager.instance != null)
        {
            if (skillNum == 2) UIManager.instance.StartSkill02Cooldown();
            if (skillNum == 3) UIManager.instance.StartSkill03Cooldown();
        }

        StartCoroutine(TemporaryIgnoreCollision(trapObj));
    }

    IEnumerator TemporaryIgnoreCollision(GameObject trapObj)
    {
        Collider playerCol = GetComponent<Collider>();
        Collider trapCol = trapObj.GetComponent<Collider>();

        if (playerCol != null && trapCol != null)
        {
            Physics.IgnoreCollision(playerCol, trapCol, true);

            yield return new WaitForSeconds(2.0f);

            if (trapObj != null)
            {
                Physics.IgnoreCollision(playerCol, trapCol, false);
            }
        }
    }

    IEnumerator CooldownRoutine(int skillNum)
    {
        if (skillNum == 2) canUseSkill2 = false;
        if (skillNum == 3) canUseSkill3 = false;

        yield return new WaitForSeconds(trapCooldown);

        if (skillNum == 2) canUseSkill2 = true;
        if (skillNum == 3) canUseSkill3 = true;

        Debug.Log($"스킬 {skillNum}번 쿨타임 종료.");
    }
}