using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

[RequireComponent(typeof(PhotonView))]
public class TestCharacterPlayerMoveMent : MonoBehaviourPunCallbacks, IPunObservable
{
    public float characterPlayerWolkSpeed = 5f;
    public float characterPolayerRunSpeed = 10f;
    public float characterPlayerRoateSpeed = 10.0f;
    public Camera characterPlayerCamera;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private double timeAtReceive;
    private const float FIXED_LERP_RATE = 0.3f;

    private bool isMoving = false;
    private bool networkIsMoving = false;
    private bool networkIsRunning = false;

    private Animator animator;
    private int animID_IsMoving;
    private int animID_IsRunning;

    private Vector3 characterPlayerDerection;

    private void Start()
    {
        if (characterPlayerCamera == null)
        {
            characterPlayerCamera = GetComponentInChildren<Camera>(true);
            if (characterPlayerCamera == null)
            {
                Debug.LogError("PlayerPrefab 내부에 'PlayerCamera' 오브젝트를 찾을 수 없습니다! 카메라 설정을 확인하세요.");
            }
        }

        if (photonView.IsMine)
        {
            if (characterPlayerCamera != null)
            {
                characterPlayerCamera.gameObject.SetActive(true);
            }
            Debug.Log("내가 조작할 수 있는 플레이어 (1인칭 시점 활성화).");
        }
        else
        {
            if (characterPlayerCamera != null)
            {
                characterPlayerCamera.gameObject.SetActive(false);
            }
            Debug.Log("다른 플레이어 카메라 및 조작 비활성화.");
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.SerializationRate = 50;
            PhotonNetwork.SendRate = 50;
            Debug.Log("Photon 전송 속도를 최대로 설정.");
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("플레이어 프리팹에서 Animator 컴포넌트를 찾을 수 없습니다. 애니메이션 코드가 비활성화됩니다.");

            enabled = false;
            return;
        }
        animID_IsMoving = Animator.StringToHash("IsMoving");
        animID_IsRunning = Animator.StringToHash("IsRunning");
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            float characterPlayerHorizontalInput = Input.GetAxis("Horizontal");
            float characterPlayoerVerticalInput = Input.GetAxis("Vertical");

            characterPlayerDerection = new Vector3(characterPlayerHorizontalInput, 0, characterPlayoerVerticalInput).normalized;

            isMoving = characterPlayerDerection.magnitude > 0.1f;

            bool currentIsRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

            animator.SetBool(animID_IsMoving, isMoving);
            animator.SetBool(animID_IsRunning, currentIsRunning);
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            CharacterPlayerMoveMentExecute();
            return;
        }

        transform.position = Vector3.Lerp(transform.position, networkPosition, FIXED_LERP_RATE);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, FIXED_LERP_RATE);

        animator.SetBool(animID_IsMoving, networkIsMoving);
        animator.SetBool(animID_IsRunning, networkIsRunning);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        bool currentIsRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isMoving);
            stream.SendNext(currentIsRunning);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();

            timeAtReceive = info.SentServerTime;
        }
    }

    void CharacterPlayerMoveMentExecute()
    {
        if (isMoving)
        {
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? characterPolayerRunSpeed : characterPlayerWolkSpeed;

            Vector3 characterPlayerMove = characterPlayerDerection * currentSpeed * Time.fixedDeltaTime;

            transform.position += characterPlayerMove;

            Quaternion characterPlayerRate = Quaternion.LookRotation(characterPlayerDerection);
            transform.rotation = Quaternion.Slerp(transform.rotation, characterPlayerRate, characterPlayerRoateSpeed * Time.fixedDeltaTime);
        }
    }
}