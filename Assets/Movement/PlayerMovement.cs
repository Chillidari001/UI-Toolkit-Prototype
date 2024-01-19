using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float move_speed;
    public float ground_drag;
    public float jump_force;
    public float jump_cooldown;
    public float air_mulitplier;
    bool jump_ready;

    [Header("Grounded Check")]
    public float player_height;
    public LayerMask ground;
    bool is_grounded;

    [Header("Keybinds")]
    public KeyCode jump_key = KeyCode.Space;

    DebugTools debug_tools;

    public Transform orientation;

    float horizontal_input;
    float vertical_input;

    Vector3 move_direction;

    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        jump_ready = true;

        debug_tools = GameObject.FindGameObjectWithTag("DebugTool").GetComponent<DebugTools>();
    }

    private void Update()
    {
        is_grounded = Physics.Raycast(transform.position, Vector3.down, player_height * 0.5f + 0.2f, ground);

        if(debug_tools.GetConsoleState() == false)
        {
            MoveInput();
        }

        SpeedCap();

        if(is_grounded)
        {
            rb.drag = ground_drag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MoveInput()
    {
        horizontal_input = Input.GetAxisRaw("Horizontal");
        vertical_input = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jump_key) && jump_ready && is_grounded)
        {
            jump_ready = false;
            Jump();

            Invoke(nameof(ResetJump), jump_cooldown);
        }
    }

    private void MovePlayer()
    {
        move_direction = orientation.forward * vertical_input + orientation.right * horizontal_input;

        if(is_grounded)
        {
            rb.AddForce(move_direction.normalized * move_speed * 10f, ForceMode.Force);
        }
        else if(!is_grounded)
        {
            rb.AddForce(move_direction.normalized * move_speed * 10f * air_mulitplier, ForceMode.Force);
        }
    }

    private void SpeedCap()
    {
        Vector3 flat_velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if(flat_velocity.magnitude > move_speed)
        {
            Vector3 limited_velocity = flat_velocity.normalized * move_speed;
            rb.velocity = new Vector3(limited_velocity.x, rb.velocity.y, limited_velocity.z);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jump_force, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        jump_ready = true;
    }

    public float GetMoveSpeed()
    {
        return move_speed;
    }
    public void SetMoveSpeed(float _move_speed)
    {
        move_speed = _move_speed;
    }
}
