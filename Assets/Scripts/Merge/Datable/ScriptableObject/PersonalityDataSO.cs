using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PersonalityData", menuName = "ScriptableObjects/PersonalityData", order = 1)]
public class PersonalityDataSO : ScriptableObject
{
    public List<Personality> personalities;
}