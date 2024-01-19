using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Trail Config", menuName = "Weapons/Weapon Trail Configuration", order =4)]
public class TrailConfigurabeScriptableObject : ScriptableObject
{
    public Material material;
    public AnimationCurve width_curve;
    public float duration = 0.5f;
    public float min_vertex_distance = 0.1f;
    public Gradient colour;

    public float miss_distance = 100f;
    public float simulation_speed = 100f;
}
