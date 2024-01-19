using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAction : MonoBehaviour
{
    [SerializeField]
    private PlayerWeaponSelector weapon_selector;

    DebugTools debug_tools;

    private void Start()
    {
        debug_tools = GameObject.FindGameObjectWithTag("DebugTool").GetComponent<DebugTools>();
    }

    private void Update()
    {
        if (debug_tools.GetConsoleState() == false)
        {
            if (Mouse.current.leftButton.isPressed && weapon_selector.active_weapon != null)
            {
                weapon_selector.active_weapon.Shoot();
            }
        }
    }
}
