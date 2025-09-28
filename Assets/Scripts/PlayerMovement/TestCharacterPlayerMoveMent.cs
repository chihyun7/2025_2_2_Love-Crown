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
                Debug.LogError("PlayerPrefab ���ο� 'PlayerCamera' ������Ʈ�� ã�� �� �����ϴ�! ī�޶� ������ Ȯ���ϼ���."); 
                return;
            }
               


        }
        if (photonView.IsMine)      // �� ĳ�������� Ȯ��
        {
            characterPlayerCamera.gameObject.SetActive(true);                    // ���� ���� 1��Ī ����
            Debug.Log("���� ������ �� �ִ� �÷��̾� (1��Ī ���� Ȱ��ȭ).");
            
        }
               
        else
        {
            characterPlayerCamera.gameObject.SetActive(false);                    // ���� ���� 1��Ī ����
            Debug.Log("�ٸ� �÷��̾� ī�޶� �� ���� ��Ȱ��ȭ.");
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;                         // �� ���� �ƴ϶�� ��� �Լ� ����
      
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
