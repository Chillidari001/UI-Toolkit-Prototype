using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Impact System/Surface Effect", fileName = "Surface Effect")]
public class SurfaceEffect : ScriptableObject
{
    public List<SpawnObjectEffect> spawn_object_effects = new List<SpawnObjectEffect>();
    public List<PlayAudioEffect> play_audio_effects = new List<PlayAudioEffect>();
}
