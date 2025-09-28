using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    //public float jumpForce = 5f;
    //public LayerMask groundLayer;
    //public Transform groundCheck;
    //public float groundCheckRadius = 0.2f;

    //private Rigidbody rb;
    //private bool isGrounded;

    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        //if (rb == null)
        //{
        //    Debug.LogError("Player object needs a Rigidbody component.");
        //}
    }

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed = runSpeed;
        }

        //if (moveDirection != Vector3.zero)
        //{
        //    rb.MovePosition(rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime);
        //}

        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //}
    }

    //void FixedUpdate()
    //{
    //    isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    //}
}
