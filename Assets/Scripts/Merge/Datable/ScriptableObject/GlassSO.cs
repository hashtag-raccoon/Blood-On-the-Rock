using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlassSO", menuName = "ScriptableObject/GlassSO")]
public class GlassSO : ScriptableObject
{
    public List<Glass> glasses;
}
