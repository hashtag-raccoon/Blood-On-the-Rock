using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ArbeitPrefabToSpritePair
{
    [Header("알바 초상화/프리팹")]
    [Tooltip("알바 초상화 스프라이트 리스트")]
    public Sprite PairPortrait;
    [Tooltip("알바 프리팹 리스트")]
    public GameObject PairPrefab;
}

[CreateAssetMenu(fileName = "ArbeitReference", menuName = "Game Data/Arbeit Prefab Reference")]
public class ArbeitSpriteReference : ScriptableObject
{
    [Header("알바 초상화/프리팹 매핑")]
    public List<ArbeitPrefabToSpritePair> Human_Pairs = new List<ArbeitPrefabToSpritePair>();
    public List<ArbeitPrefabToSpritePair> Oak_Pairs = new List<ArbeitPrefabToSpritePair>();
    public List<ArbeitPrefabToSpritePair> Vampire_Pairs = new List<ArbeitPrefabToSpritePair>();

    [Header("알바 초상화 리스트 (구인소용)")]
    [Tooltip("Human 종족의 초상화 스프라이트 리스트")]
    public List<Sprite> Human_portraits = new List<Sprite>();
    [Tooltip("Oak 종족의 초상화 스프라이트 리스트")]
    public List<Sprite> Oak_portraits = new List<Sprite>();
    [Tooltip("Vampire 종족의 초상화 스프라이트 리스트")]
    public List<Sprite> Vampire_portraits = new List<Sprite>();
}