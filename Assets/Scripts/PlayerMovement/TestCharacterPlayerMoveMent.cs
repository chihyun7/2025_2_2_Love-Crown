using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class TestCharacterPlayerMoveMent : MonoBehaviour
{
    public float characterPlayerWolkSpeed = 5f;
    public float characterPolayerRunSpeed = 10f;
    public float characterPlayerRoateSpeed = 10.0f;

    public Camera characterPlayerCamera;
    private CharacterController characterPlaeyrCharacterController;
    private PhotonView photonView;
    private void Awake()
    {
        if (!photonView) photonView = GetComponent<PhotonView>();
         characterPlaeyrCharacterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (characterPlayerCamera == null)
        {
           Camera characterPlayerCamera = GetComponentInChildren<Camera>(true);

            if(characterPlayerCamera == null)
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
      
        CharacterPlayerMoveMent();

    }

    void CharacterPlayerMoveMent()
    {
        float characterPlayerHorizontalInput = Input.GetAxis("Horizontal");
        float characterPlayoerVerticalInput = Input.GetAxis("Vertical");

        Vector3 characterPlayerDerection = new Vector3(characterPlayerHorizontalInput, 0, characterPlayoerVerticalInput).normalized;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)? characterPolayerRunSpeed: characterPlayerWolkSpeed;
        Vector3 characterPlayerMove = characterPlayerDerection * currentSpeed * Time.deltaTime;
        characterPlaeyrCharacterController.Move(characterPlayerMove);

        if (characterPlayerDerection != Vector3.zero)
        {
            Quaternion characterPlayerRate = Quaternion.LookRotation(characterPlayerDerection);
            transform.rotation = Quaternion.Slerp(transform.rotation,characterPlayerRate, characterPlayerRoateSpeed * Time.deltaTime);
        }  
    }

}
