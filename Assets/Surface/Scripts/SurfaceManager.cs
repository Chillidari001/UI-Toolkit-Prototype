using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

public class SurfaceManager : MonoBehaviour
{
    private static SurfaceManager _instance;

    public static SurfaceManager Instance
    {
        get
        {
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("More than 1 SurfaceManager active in scene. Destroying the latest one" + name);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [SerializeField]
    private List<SurfaceType> surfaces = new List<SurfaceType>();
    [SerializeField]
    private int default_pool_sizes = 10;
    [SerializeField]
    private Surface default_surface;

    public void HandleImpact(GameObject hit_object, Vector3 hit_point, Vector3 hit_normal, ImpactType impact, int tri_index)
    {
        if (hit_object.TryGetComponent<Terrain>(out Terrain terrain))
        {
            List<TextureAlpha> active_textures = GetActiveTexturesFromTerrain(terrain, hit_point);
            foreach (TextureAlpha _active_textures in active_textures)
            {
                SurfaceType surface_type = surfaces.Find(surface => surface.albedo == _active_textures.texture);
                if (surface_type != null)
                {
                    foreach (Surface.SurfaceImpactTypeEffect type_effect in surface_type.surface.impact_type_effects)
                    {
                        if (type_effect.impact_type == impact)
                        {
                            PlayEffects(hit_point, hit_normal, type_effect.surface_effect, _active_textures.alpha);
                        }
                    }
                }


                else
                {
                    foreach (Surface.SurfaceImpactTypeEffect type_effect in default_surface.impact_type_effects)
                    {
                        if (type_effect.impact_type == impact)
                        {
                            PlayEffects(hit_point, hit_normal, type_effect.surface_effect, 1);
                        }
                    }
                }
            }
        }
        else if(hit_object.TryGetComponent<Renderer>(out Renderer renderer))
        {
            Texture active_texture = GetActiveTextureFromRenderer(renderer, tri_index);

            SurfaceType surface_type = surfaces.Find(surface => surface.albedo == active_texture);
            if(surface_type != null)
            {
                foreach(Surface.SurfaceImpactTypeEffect type_effect in surface_type.surface.impact_type_effects)
                {
                    if(type_effect.impact_type == impact)
                    {
                        PlayEffects(hit_point, hit_normal, type_effect.surface_effect, 1);
                    }
                }
            }
        }
    }

    private List<TextureAlpha> GetActiveTexturesFromTerrain(Terrain terrain, Vector3 hit_point)
    {
        Vector3 terrain_position = hit_point - terrain.transform.position;
        Vector3 splat_map_position = new Vector3(terrain_position.x / terrain.terrainData.size.x,
            0,
            terrain_position.z / terrain.terrainData.size.z);

        int x = Mathf.FloorToInt(splat_map_position.x * terrain.terrainData.alphamapWidth);
        int z = Mathf.FloorToInt(splat_map_position.z * terrain.terrainData.alphamapWidth);

        float[,,] alpha_map = terrain.terrainData.GetAlphamaps(x, z, 1, 1);

        List<TextureAlpha> active_textures = new List<TextureAlpha>();
        for (int i = 0; i < alpha_map.Length; i++)
        {
            if (alpha_map[0, 0, i] > 0)
            {
                active_textures.Add(new TextureAlpha()
                {
                    texture = terrain.terrainData.terrainLayers[i].diffuseTexture,
                    alpha = alpha_map[0, 0, i]
                });
            }
        }

        return active_textures;
    }

    private Texture GetActiveTextureFromRenderer(Renderer renderer, int tri_index)
    {
        if (renderer.TryGetComponent<MeshFilter>(out MeshFilter mesh_filter))
        {
            Mesh mesh = mesh_filter.mesh;

            if(mesh.subMeshCount > 1)
            {
                int[] hit_trangle_indices = new int[]
                {
                    mesh.triangles[tri_index * 3],
                    mesh.triangles[tri_index * 3 + 1],
                    mesh.triangles[tri_index * 3 + 2]
                };

                for(int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] submesh_tris = mesh.GetTriangles(i);
                    for(int c = 0; c < submesh_tris.Length; c++)
                    {
                        if (submesh_tris[c] == hit_trangle_indices[0]
                            && submesh_tris[c + 1] == hit_trangle_indices[1]
                            && submesh_tris[c + 2] == hit_trangle_indices[2])
                        {
                            return renderer.sharedMaterials[i].mainTexture;
                        }
                    }
                }
            }
            else
            {
                return renderer.sharedMaterial.mainTexture;
            }
        }

        Debug.LogError($"{renderer.name} has no mesh filter. Using default impact effect instead of texture-specific impact effect as correct texture cannot be found");
        return null;
    }

    private void PlayEffects(Vector3 hit_point, Vector3 hit_normal, SurfaceEffect surface_effect, float sound_offset)
    {
        foreach(SpawnObjectEffect spawn_object_effect in surface_effect.spawn_object_effects)
        {
            if(spawn_object_effect.chance > Random.value)
            {
                ObjectPool pool = ObjectPool.CreateInstance(spawn_object_effect.prefab.GetComponent<PoolableObject>(), default_pool_sizes);
                PoolableObject instance = pool.GetObject(hit_point + hit_normal * 0.001f, Quaternion.LookRotation(hit_normal));

                instance.transform.forward = hit_normal;
                if(spawn_object_effect.random_rotation)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(0, 180 * spawn_object_effect.random_rotation_multiplier.x),
                        Random.Range(0, 180 * spawn_object_effect.random_rotation_multiplier.y),
                        Random.Range(0, 180 * spawn_object_effect.random_rotation_multiplier.z)
                    );

                    instance.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + offset);
                }
            }
        }

        foreach(PlayAudioEffect play_audio_effect in surface_effect.play_audio_effects)
        {
            AudioClip clip = play_audio_effect.audio_clips[Random.Range(0, play_audio_effect.audio_clips.Count)];
            ObjectPool pool = ObjectPool.CreateInstance(play_audio_effect.audio_source_prefab.GetComponent<PoolableObject>(), default_pool_sizes);
            AudioSource audio_source = pool.GetObject().GetComponent<AudioSource>();

            audio_source.transform.position = hit_point;
            audio_source.PlayOneShot(clip, sound_offset * Random.Range(play_audio_effect.volume_range.x, play_audio_effect.volume_range.y));
            StartCoroutine(DisableAudioSource(audio_source, clip.length));
        }
    }

    private IEnumerator DisableAudioSource(AudioSource audio_source, float time)
    {
        yield return new WaitForSeconds(time);
        audio_source.gameObject.SetActive(false);
    }

    private class TextureAlpha
    {
        public float alpha;
        public Texture texture;
    }
}
