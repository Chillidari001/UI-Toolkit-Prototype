using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float sens_x;
    public float sens_y;

    public Transform orientation;

    float x_rotation;
    float y_rotation;

    DebugTools debug_tools;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //not needed anymore, checking cursor visibility !!
        //debug_tools = GameObject.FindGameObjectWithTag("DebugTool").GetComponent<DebugTools>();
    }

    private void Update()
    {
        if(Cursor.visible == false)
        {
            float mouse_x = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sens_x;
            float mouse_y = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sens_y;

            y_rotation += mouse_x;

            x_rotation -= mouse_y;
            x_rotation = Mathf.Clamp(x_rotation, -90f, 90f);

            transform.rotation = Quaternion.Euler(x_rotation, y_rotation, 0);
            orientation.rotation = Quaternion.Euler(0, y_rotation, 0);
        }
    }
}
