using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArbeitReference", menuName = "Game Data/Arbeit Sprite Reference")]
public class ArbeitSpriteReference : ScriptableObject
{
    [Header("초상화 목록")]
    [Tooltip("NPC 초상화 스프라이트 리스트")]
    public List<Sprite> Human_portraits = new List<Sprite>();
    public List<Sprite> Oak_portraits = new List<Sprite>();
    public List<Sprite> Vampire_portraits = new List<Sprite>();
    [Header("프리팹 목록")]
    public List<GameObject> Human_Prefabs = new List<GameObject>();
    public List<GameObject> Oak_Prefabs = new List<GameObject>();
    public List<GameObject> Vampire_Prefabs = new List<GameObject>();
}