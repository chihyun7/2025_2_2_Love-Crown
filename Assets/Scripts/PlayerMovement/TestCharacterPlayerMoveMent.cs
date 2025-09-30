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
    private CharacterController characterPlaeyrCharacterController;
    //private PhotonView photonView;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private bool isMoving = false;
    private bool networkIsMoving = false;
    private void Awake()
    {
        //if (!photonView) photonView = GetComponent<PhotonView>();
        characterPlaeyrCharacterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (characterPlayerCamera == null)
        {
            Camera characterPlayerCamera = GetComponentInChildren<Camera>(true);

            if (characterPlayerCamera == null)
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

        isMoving = CharacterPlayerMoveMent();

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 1. ������ �۽� (���� �����̴� �÷��̾��� ��)
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isMoving);
        }
        else
        {
            // 2. ������ ���� (�ٸ� �÷��̾��� ��)
            // ��Ʈ��ũ�� ���� ���� �����͸� ����.

            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();

            // ���� ���� �� ����(Interpolation)�� ���� �߰� �۾� (���� ����)
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += (Vector3)(networkPosition * lag); // ������ ���� ����
        }
    }

    private void LateUpdate()
    {
        // �� ĳ���Ͱ� �ƴ� ���� ��Ʈ��ũ ��ġ�� ������Ʈ�մϴ�.
        if (!photonView.IsMine)
        {
            // �ε巯�� �������� ���� ����(Lerp) ����
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

            if (characterPlaeyrCharacterController != null)
                characterPlaeyrCharacterController.Move(characterPlayerMove);

            // ȸ�� ����
            Quaternion characterPlayerRate = Quaternion.LookRotation(characterPlayerDerection);
            transform.rotation = Quaternion.Slerp(transform.rotation, characterPlayerRate, characterPlayerRoateSpeed * Time.deltaTime);
        }

        return currentlyMoving;
    }
}


