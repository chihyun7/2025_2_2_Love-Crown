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
    public float walkSpeed = 3f;
    public float runSpeed = 5f;

    public bool canMove = true;
    public bool canAttack = true;

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
    private int animID_IsFallingDown;
    private int animeID_isAttack;
    private bool currentIsFallingDown = false;
    private bool currentIsAttack = false;
    private Vector3 moveDirection;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
            {
                // 카메라 없음 처리
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
            enabled = false;
            return;
        }

        animID_IsMoving = Animator.StringToHash("IsMoving");
        animID_IsRunning = Animator.StringToHash("IsRunning");
        animID_IsFallingDown = Animator.StringToHash("IsFallingDown");
        animeID_isAttack = Animator.StringToHash("IsAttack");

        if (UIManager.instance != null)
        {
            uiManager = UIManager.instance;
        }
        else
        {
            // UIManager 싱글톤 참조 실패
        }
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
        }
        else
        {
            isMoving = false;
            moveDirection = Vector3.zero;
            animator.SetBool(animID_IsMoving, false);
            animator.SetBool(animID_IsRunning, false);
        }

        if (canMove && canAttack && Input.GetMouseButtonDown(1))
        {
            photonView.RPC("RequestAttackRPC", RpcTarget.All);
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
        animator.SetBool(animID_IsFallingDown, currentIsFallingDown);
        animator.SetBool(animeID_isAttack, currentIsAttack); // 네트워크 공격 애니메이션 동기화
    }


    [PunRPC]
    public void RequestAttackRPC()
    {
        // 모든 클라이언트에서 공격 코루틴 실행
        StartCoroutine(PlayerAttackStart());
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
            stream.SendNext(currentIsAttack); // 공격 상태 동기화
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();
            networkIsRunning = (bool)stream.ReceiveNext();
            currentIsFallingDown = (bool)stream.ReceiveNext();
            currentIsAttack = (bool)stream.ReceiveNext(); // 공격 상태 수신
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
        if (!photonView.IsMine) return;

        if (other.CompareTag("DownPlayerSpeed"))
        {
            StartCoroutine(DownPlayerSpeed());
            Destroy(other.gameObject);
        }

        if (other.CompareTag("PlayerAttact"))
        {
            photonView.RPC("RPCHitByAttack", RpcTarget.All);
        }

        if (other.CompareTag("FullingDownObject"))
        {
            photonView.RPC("RPCPlayerFullDown", RpcTarget.All);
            Destroy(other.gameObject);
        }
    }

    [PunRPC]
    public void RPCPlayerFullDown()
    {
        StartCoroutine(PlayerFullDown());
    }

    [PunRPC]
    public void RPCHitByAttack()
    {
        StartCoroutine(PlayerFullDown());
    }


    IEnumerator DownPlayerSpeed()
    {
        walkSpeed = 1f;
        runSpeed = 2.5f;
        yield return new WaitForSeconds(15f);
        walkSpeed = 3f;
        runSpeed = 5f;
    }

    IEnumerator PlayerAttackStart()
    {
        float cooldownDuration = 30f;
        float attackDuration = 1.5f;

        canAttack = false;
        currentIsAttack = true;
        animator.SetBool(animeID_isAttack, currentIsAttack);

        if (photonView.IsMine && UIManager.instance != null)
        {
            UIManager.instance.StartAttackCooldown(cooldownDuration);
        }

        player_AttackObject.gameObject.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        player_AttackObject.gameObject.SetActive(false);
        currentIsAttack = false;
        animator.SetBool(animeID_isAttack, currentIsAttack);

        yield return new WaitForSeconds(cooldownDuration - attackDuration);

        canAttack = true;
    }

    IEnumerator PlayerFullDown()
    {
        float originalWalkSpeed = walkSpeed;
        float originalRunSpeed = runSpeed;

        walkSpeed = 0f;
        runSpeed = 0f;

        currentIsFallingDown = true;

        if (photonView.IsMine)
        {
            isMouseRock = true;
            transform.rotation = Quaternion.Euler(-90, 0, 0);
        }

        yield return new WaitForSeconds(2f);

        currentIsFallingDown = false;

        if (photonView.IsMine)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            isMouseRock = false;
            walkSpeed = originalWalkSpeed;
            runSpeed = originalRunSpeed;
        }
    }
}