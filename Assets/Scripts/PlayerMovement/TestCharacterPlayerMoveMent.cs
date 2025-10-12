using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class TestCharacterPlayerMoveMent : MonoBehaviourPunCallbacks, IPunObservable
{
    public float characterPlayerWolkSpeed = 5f;
    public float characterPolayerRunSpeed = 10f;
    public float characterPlayerRoateSpeed = 10.0f;

    public Camera characterPlayerCamera;
    //private PhotonView photonView;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private bool isMoving = false;
    private bool networkIsMoving = false;

    private void Start()
    {

        if (characterPlayerCamera == null)
        {
            Camera characterPlayerCamera = GetComponentInChildren<Camera>(true);

            if (characterPlayerCamera == null)
            {
                Debug.LogError("PlayerPrefab 내부에 'PlayerCamera' 오브젝트를 찾을 수 없습니다! 카메라 설정을 확인하세요.");
                return;
            }



        }
        if (photonView.IsMine)      // 내 캐릭터인지 확인
        {
            characterPlayerCamera.gameObject.SetActive(true);                    // 이제 나의 1인칭 시점
            Debug.Log("내가 조작할 수 있는 플레이어 (1인칭 시점 활성화).");

        }

        else
        {
            characterPlayerCamera.gameObject.SetActive(false);                    // 이제 나의 1인칭 시점
            Debug.Log("다른 플레이어 카메라 및 조작 비활성화.");
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;                         // 내 것이 아니라면 즉시 함수 종료

        isMoving = CharacterPlayerMoveMent();

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 1. 데이터 송신 (내가 움직이는 플레이어일 때)
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isMoving);
        }
        else
        {
            // 2. 데이터 수신 (다른 플레이어일 때)
            // 네트워크를 통해 받은 데이터를 저장.

            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();

            // 핑이 높을 때 보간(Interpolation)을 위한 추가 작업 (선택 사항)
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += (Vector3)(networkPosition * lag); // 간단한 보간 보정
        }
    }

    private void LateUpdate()
    {
        // 내 캐릭터가 아닐 때만 네트워크 위치로 업데이트합니다.
        if (!photonView.IsMine)
        {
            // 부드러운 움직임을 위해 보간(Lerp) 적용
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * characterPlayerRoateSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * characterPlayerRoateSpeed);
        }
    }

    bool CharacterPlayerMoveMent()
    {
        float characterPlayerHorizontalInput = Input.GetAxis("Horizontal");
        float characterPlayoerVerticalInput = Input.GetAxis("Vertical");

        Vector3 characterPlayerDerection = new Vector3(characterPlayerHorizontalInput, 0, characterPlayoerVerticalInput).normalized;

        bool currentlyMoving = characterPlayerDerection.magnitude > 0.1f;

        if (currentlyMoving)
        {
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? characterPolayerRunSpeed : characterPlayerWolkSpeed;

            Vector3 characterPlayerMove = characterPlayerDerection * currentSpeed * Time.deltaTime;

            transform.position += characterPlayerMove;

            // 회전 로직
            Quaternion characterPlayerRate = Quaternion.LookRotation(characterPlayerDerection);
            transform.rotation = Quaternion.Slerp(transform.rotation, characterPlayerRate, characterPlayerRoateSpeed * Time.deltaTime);
        }

        return currentlyMoving;
    }
}


