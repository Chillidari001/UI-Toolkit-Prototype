using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform camera_position;

    private void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        transform.position = camera_position.position;
    }
}