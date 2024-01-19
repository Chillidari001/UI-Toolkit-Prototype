using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWeaponSelector : MonoBehaviour
{
    [SerializeField]
    private WeaponType _weapon;
    [SerializeField]
    private Transform weapon_parent;
    [SerializeField]
    private List<WeaponScriptableObject> weapons;

    [Space]
    [Header("Runtime Filled")]
    public WeaponScriptableObject active_weapon;

    private void Start()
    {
        WeaponScriptableObject weapon = weapons.Find(weapon => weapon.type == _weapon);
        if(weapon == null)
        {
            Debug.LogError($"No WeaponTypeScriptableObject found for WeaponType: {weapon}");
        }

        active_weapon = weapon;
        weapon.Spawn(weapon_parent, this);
    }
}
