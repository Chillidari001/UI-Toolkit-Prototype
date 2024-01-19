using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Impact System/Play Audio Effect", fileName = "Play Audio Effect")]
public class PlayAudioEffect : ScriptableObject
{
    public AudioSource audio_source_prefab;
    public List<AudioClip> audio_clips = new List<AudioClip>();
    [Tooltip("Value are clamped between 0-1")]
    public Vector2 volume_range = new Vector2(0, 1);
}
