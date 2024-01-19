using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Impact System/Surface", fileName = "Surface")]
public class Surface : ScriptableObject
{
    [Serializable]
    public class SurfaceImpactTypeEffect
    {
        public ImpactType impact_type;
        public SurfaceEffect surface_effect;
    }

    public List<SurfaceImpactTypeEffect> impact_type_effects = new List<SurfaceImpactTypeEffect>();
}
