using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Weapons", menuName = "Weapons/Weapons", order = 0)]
public class WeaponScriptableObject : ScriptableObject
{
    public ImpactType impact_type;
    public WeaponType type;
    public string _name;
    public GameObject model_prefab;
    public Vector3 spawn_point;
    public Vector3 spawn_rotation;

    public ShootConfigurationScriptableObject shoot_config;
    public TrailConfigurabeScriptableObject trail_conifg;

    private MonoBehaviour active_mono_behaviour;
    private GameObject model;
    private float last_shoot_time;
    private ParticleSystem shoot_system;
    private ObjectPool<TrailRenderer> trail_pool;

    public void Spawn(Transform parent, MonoBehaviour active_mono_behaviour)
    {
        this.active_mono_behaviour = active_mono_behaviour;
        last_shoot_time = 0; //scriptable objects are mutated in game mode but are not reset unless built
        trail_pool = new ObjectPool<TrailRenderer>(CreateTrail);
        model = Instantiate(model_prefab);
        model.transform.SetParent(parent, false);
        model.transform.localPosition = spawn_point;
        model.transform.localRotation = Quaternion.Euler(spawn_rotation);

        shoot_system = model.GetComponentInChildren<ParticleSystem>();
    }

    public void Shoot()
    {
        if(Time.time > shoot_config.fire_rate + last_shoot_time)
        {
            last_shoot_time = Time.time;
            shoot_system.Play();
            Vector3 shoot_direction = shoot_system.transform.forward + new Vector3(
                Random.Range(-shoot_config.spread.x, shoot_config.spread.x), Random.Range(-shoot_config.spread.y, shoot_config.spread.y),
                Random.Range(-shoot_config.spread.z, shoot_config.spread.z));
            shoot_direction.Normalize();

            if(Physics.Raycast(shoot_system.transform.position, shoot_direction, out RaycastHit hit, float.MaxValue, shoot_config.hit_mask))
            {
                active_mono_behaviour.StartCoroutine(PlayTrail(shoot_system.transform.position, hit.point, hit));
            }
            else
            {
                active_mono_behaviour.StartCoroutine(PlayTrail(shoot_system.transform.position, shoot_system.transform.position
                    + (shoot_direction * trail_conifg.miss_distance), new RaycastHit()));
            }
        }
    }

    private IEnumerator PlayTrail(Vector3 start_point, Vector3 end_point, RaycastHit hit)
    {
        TrailRenderer instance = trail_pool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = start_point;
        yield return null;

        instance.emitting = true;

        float distance = Vector3.Distance(start_point, end_point);
        float remaining_distance = distance;
        while(remaining_distance > 0)
        {
            instance.transform.position = Vector3.Lerp(start_point, end_point, Mathf.Clamp01(1 - (remaining_distance / distance)));
            remaining_distance -= trail_conifg.simulation_speed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = end_point;

        if(hit.collider != null)
        {
            SurfaceManager.Instance.HandleImpact(
                hit.transform.gameObject,
                end_point,
                hit.normal,
                impact_type,
                0);
        }

        yield return new WaitForSeconds(trail_conifg.duration);
        yield return null;
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        trail_pool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = trail_conifg.colour;
        trail.material = trail_conifg.material;
        trail.widthCurve = trail_conifg.width_curve;
        trail.time = trail_conifg.duration;
        trail.minVertexDistance = trail_conifg.min_vertex_distance;

        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }
}
