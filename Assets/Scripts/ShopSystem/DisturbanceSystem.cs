using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.UI;

public class DisturbanceSystem : MonoBehaviourPunCallbacks
{
    public static DisturbanceSystem Instance;

    public UIManager uiManager;
    public GameObject player1EyePatch;
    public GameObject player2EyePatch;

    private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();

    private readonly float EFFECT_DURATION = 5f;
    public readonly float COOLDOWN_DURATION = 35f;

    private PhotonView pv;

    private class PlayerState
    {
        public bool IsUseItem = false;
        public int UseCount = 3;
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
    {
        if (UIManager.instance != null)
        {
            uiManager = UIManager.instance;
        }

        if (!player1EyePatch)
            player1EyePatch = GameObject.Find("UI_Player1_EyePatch");

        if (!player2EyePatch)
            player2EyePatch = GameObject.Find("UI_Player2_EyePatch");

        if (player1EyePatch != null) player1EyePatch.SetActive(false);
        if (player2EyePatch != null) player2EyePatch.SetActive(false);
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom && pv != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
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

           
                if (uiManager != null)
                {
                    uiManager.StartSkill01Cooldown();
                }

                StartCoroutine(NextUseTime(localPlayer.ActorNumber));
                state.IsUseItem = true;
            }
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
            yield break;
        }

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
        }
    }

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