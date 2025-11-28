using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using Unity.Mathematics;

[RequireComponent(typeof(PhotonView))]
public class TestCharacterPlayerMoveMent : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    public bool canMove = true;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float mouseSensitivity = 250f;
    public float cameraUpDownLimit = 80f;

    [Header("캐릭터 캡슐 내부 오브젝트")]
    public GameObject CharacterObject;
    public GameObject player_AttackObject;

    public UIManager uiManager;

    private float cameraRotationX = 0f;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private const float FIXED_LERP_RATE = 0.3f;

    private bool isMoving = false;
    private bool networkIsMoving = false;
    private bool networkIsRunning = false;
    private bool isMouseRock;
    private Animator animator;
    private int animID_IsMoving;
    private int animID_IsRunning;
    private bool currentIsFallingDown = false;
    private Vector3 moveDirection;
    private bool isNotPlayrAttact;

    private void Start()
    {

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
            {
                Debug.LogError("PlayerPrefab 내부에 'PlayerCamera'를 찾을 수 없습니다! 카메라 설정 확인하세요.");
            }
        }

        if (photonView.IsMine)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
            }
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.SerializationRate = 50;
            PhotonNetwork.SendRate = 50;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator 없음! 애니메이션 사용 불가");
            enabled = false;
            return;
        }

        animID_IsMoving = Animator.StringToHash("IsMoving");
        animID_IsRunning = Animator.StringToHash("IsRunning");

        if (!uiManager)
            uiManager = GetComponent<UIManager>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (canMove)
        {
            if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || isMouseRock))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

                transform.Rotate(0, mouseX, 0);

                cameraRotationX -= mouseY;
                cameraRotationX = Mathf.Clamp(cameraRotationX, -cameraUpDownLimit, cameraUpDownLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camRight = playerCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * v + camRight * h).normalized;
            isMoving = moveDirection.magnitude > 0.1f;

            bool currentIsRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isMoving;

            animator.SetBool(animID_IsMoving, isMoving);
            animator.SetBool(animID_IsRunning, currentIsRunning);

            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("플레이어 공격");
                photonView.RPC("RequestAttackRPC", RpcTarget.All);
                uiManager.isPlayerAttact = true;
            }
        }
        else
        {
            isMoving = false;
            moveDirection = Vector3.zero;
            animator.SetBool(animID_IsMoving, false);
            animator.SetBool(animID_IsRunning, false);
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            Move();
            return;
        }
        transform.position = Vector3.Lerp(transform.position, networkPosition, FIXED_LERP_RATE);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, FIXED_LERP_RATE);

        animator.SetBool(animID_IsMoving, networkIsMoving);
        animator.SetBool(animID_IsRunning, networkIsRunning);
        animator.SetBool("IsFallingDown", currentIsFallingDown);
    }


    [PunRPC]
    public void RequestAttackRPC()
    {
        StartCoroutine(PlayerAttackStart());
        Debug.Log("플레이어 공격 동기화 완료");
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
            stream.SendNext(currentIsFallingDown);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();
            currentIsFallingDown = (bool)stream.ReceiveNext();
        }
    }

    void Move()
    {
        if (!isMoving) return;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed;

        Vector3 move = moveDirection * currentSpeed * Time.fixedDeltaTime;
        transform.position += move;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DownPlayerSpeed") && photonView.IsMine)
        {
            StartCoroutine(DownPlayerSpeed());
            Debug.Log("방해 오브젝트와 충돌! 15초 동안 플레이어 속도 감소");
        }

        if (other.CompareTag("PlayerAttact") && photonView.IsMine)
        {
            photonView.RPC("RPCPlayerAttack", RpcTarget.All);
           
            Debug.Log("20초동안 플레이어 정지");
        }

        if (other.CompareTag("FullingDownObject") && photonView.IsMine)
        {
            Debug.Log("방해 오브젝트 충돌");
            currentIsFallingDown = true;
            photonView.RPC("RPCPlayerFullDown", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPCPlayerFullDown()
    {
        StartCoroutine(PlayerFullDown());

        Debug.Log("동기화 진행");
        if (photonView.IsMine)
        {
            isMouseRock = true;
            transform.rotation = Quaternion.Euler(-90, 0, 0);
        }
    }

    [PunRPC]
    public void RPCPlayerAttack()
    {
        StartCoroutine(PlayerFullDown());
        Debug.Log("동기화 진행");
        if (photonView.IsMine)
        {
            isMouseRock = true;
            transform.rotation = Quaternion.Euler(-90, 0, 0);
        }
    }


    IEnumerator DownPlayerSpeed()
    {
        walkSpeed = 2f;
        runSpeed = 3f;
        yield return new WaitForSeconds(15f);
        walkSpeed = 5f;
        runSpeed = 10f;
    }

    IEnumerator PlayerAttackStart()
    {
        player_AttackObject.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        player_AttackObject.gameObject.SetActive(false);
    }

    IEnumerator PlayerAttackCollTime()
    {
        Debug.Log("콜 타임 시간 종료 까지 공격 못함...알아서 해^^");
        yield return new WaitForSeconds(30f);
        Debug.Log("콜 타임 종료");
        isNotPlayrAttact = false;
    }

    IEnumerator PlayerFullDown()
    {
        walkSpeed = 0f;
        runSpeed = 0f;

        animator.SetBool("IsFallingDown", true);
        yield return new WaitForSeconds(2f);
        animator.SetBool("IsFallingDown", false);
        uiManager.isPlayerAttact = false;

        if (photonView.IsMine)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
        }

            currentIsFallingDown = false;
            isMouseRock = false;
            walkSpeed = 5;
            runSpeed = 10;
        }
    }
