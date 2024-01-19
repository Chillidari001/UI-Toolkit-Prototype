using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Impact System/Spawn Object Effect", fileName = "Spawn Object Effect")]

public class SpawnObjectEffect : ScriptableObject
{
    public GameObject prefab;
    public float chance = 1;
    public bool random_rotation;
    [Tooltip("Zero values will lock rotation on that axis, values up to 360 for each X Y Z")]
    public Vector3 random_rotation_multiplier = Vector3.zero;
}
