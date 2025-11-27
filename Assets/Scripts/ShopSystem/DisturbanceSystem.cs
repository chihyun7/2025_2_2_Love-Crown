using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.UI;
using Photon.Pun.Demo.Procedural;

public class DisturbanceSystem : MonoBehaviourPunCallbacks
{
    public static DisturbanceSystem Instance;
    public UIManager UIManager;
    public GameObject player1EyePatch;
    public GameObject player2EyePatch;

    private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();

    private readonly float EFFECT_DURATION = 5f;
    private readonly float COOLDOWN_DURATION = 35f;
    private bool isUesItem01;

    private PhotonView pv;

    private class PlayerState
    {
        public bool IsUseItem = false;
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            pv = GetComponent<PhotonView>();
        }
        else
        {
            return;
        }
    }

    private void Start() 
    {   if(!player1EyePatch)
            player1EyePatch = GameObject.Find("UI_Player1_EyePatch");

        if(!player2EyePatch)
            player2EyePatch = GameObject.Find("UI_Player2_EyePatch");

        if (player1EyePatch != null) player1EyePatch.SetActive(false);
        if (player2EyePatch != null) player2EyePatch.SetActive(false);

        if(!UIManager)
        {
            UIManager = GetComponent<UIManager>();
        }

        if (UIManager.UseItem01 != null)
        {
            UIManager.UseItem01.maxValue = EFFECT_DURATION;
            UIManager.UseItem01.value = 0f;
        }
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom && pv != null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                UseEyePatch_InputCheck(PhotonNetwork.LocalPlayer);
            }
        }
    }

    void UseEyePatch_InputCheck(Player localPlayer)
    {
        if (!playerStates.ContainsKey(localPlayer.ActorNumber))
        {
            playerStates[localPlayer.ActorNumber] = new PlayerState();
        }

        PlayerState state = playerStates[localPlayer.ActorNumber];
        

        if (!state.IsUseItem)
        {
            Player[] playerlist = PhotonNetwork.PlayerList;

  
                Player targetPlayer = playerlist.FirstOrDefault(p => p.ActorNumber != localPlayer.ActorNumber);

                if (targetPlayer != null)
                {
                    pv.RPC("RpcActivateEffectForTarget", targetPlayer, localPlayer.ActorNumber);


                    StartCoroutine(NextUseTime(localPlayer.ActorNumber));
                    StartCoroutine(IsUseItem01());
                    state.IsUseItem = true;
                }
                else
                {
                    Debug.LogWarning("타겟 플레이어 (나 외의 다른 플레이어)를 찾을 수 없습니다.");
                }
        }
        else
        {
            Debug.Log($"사용 후 {COOLDOWN_DURATION}초 후에 다시 사용할 수 있습니다.");
        }
    }

    [PunRPC]
    public void RpcActivateEffectForTarget(int senderActorNumber)
    {
        StartCoroutine(Eye_patchCoroution(PhotonNetwork.LocalPlayer.ActorNumber));
    }

    IEnumerator Eye_patchCoroution(int targetActorNumber)
    {
        GameObject eyePatch = GetEyePatchUI(targetActorNumber);

        if (eyePatch == null)
        {
            Debug.LogError($"[RPC Error] 타겟 ({targetActorNumber})의 eye_patch가 연결되지 않았습니다.");
            yield break;
        }

        Debug.Log("안대 사용 (효과 적용)");
        eyePatch.gameObject.SetActive(true);
        yield return new WaitForSeconds(EFFECT_DURATION);
        eyePatch.gameObject.SetActive(false);
    }

    private GameObject GetEyePatchUI(int actorNumber)
    {
        if (actorNumber == 1) return player1EyePatch;
        if (actorNumber == 2) return player2EyePatch;
        return null;
    }

    IEnumerator NextUseTime(int actorNumber)
    {
        yield return new WaitForSeconds(COOLDOWN_DURATION);

        if (playerStates.ContainsKey(actorNumber))
        {
            playerStates[actorNumber].IsUseItem = false;
            isUesItem01 = false;
            Debug.Log($"쿨타임 종료. 플레이어 {actorNumber} 다시 사용할 수 있습니다.");
        }
    }

    private IEnumerator IsUseItem01()
    {
        if(photonView.IsMine && isUesItem01)
        {
            float timer = EFFECT_DURATION;
            UIManager.UseItem01.value = timer;

            while(timer > 0f)
            {
                timer -= Time.deltaTime;

                UIManager.UseItem01.value = timer;

                yield return null;
            }
        }
    }

    //private bool IsUseItem02()
    //{

    //}

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!playerStates.ContainsKey(newPlayer.ActorNumber))
        {
            playerStates[newPlayer.ActorNumber] = new PlayerState();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerStates.ContainsKey(otherPlayer.ActorNumber))
        {
            playerStates.Remove(otherPlayer.ActorNumber);
        }
    }

    public override void OnLeftRoom()
    {
        if (Instance == this) Instance = null;
        Destroy(gameObject);
    }
}