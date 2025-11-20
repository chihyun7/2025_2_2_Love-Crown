using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

[RequireComponent(typeof(PhotonView))]
public class TestCharacterPlayerMoveMent : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float mouseSensitivity = 250f;
    public float cameraUpDownLimit = 80f;

    public GameObject player_AttackObject;

    private float cameraRotationX = 0f;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private const float FIXED_LERP_RATE = 0.3f;

    private bool isMoving = false;
    private bool networkIsMoving = false;
    private bool networkIsRunning = false;

    private Animator animator;
    private int animID_IsMoving;
    private int animID_IsRunning;

    private Vector3 moveDirection;

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
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            transform.Rotate(0, mouseX, 0);

            cameraRotationX -= mouseY;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -cameraUpDownLimit, cameraUpDownLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
        }

        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(PlayerAttackStart());
            Debug.Log("플레이어 공격");
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
        if(other.CompareTag("DownPlayerSpeed") && photonView.IsMine)
        {
            StartCoroutine(DownPlayerSpeed());
            Debug.Log("방해 오브젝트와 충돌! 15초 동안 플레이어 속도 감소");
        }

        if(other.CompareTag("PlayerAttact") && photonView.IsMine)
        {
            StartCoroutine(PlayerAttact());
            Debug.Log("15초동안 플레이어 정지");
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

    IEnumerator PlayerAttact()
    {
        walkSpeed = 0f;
        runSpeed = 0f;
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
}
