using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class DrinkData
{
    public string drinkName; // 술 이름
    public Sprite drinkImage; // 술 이미지
}

public class CocktailData : MonoBehaviour
{
    [SerializeField]
    DrinkData testDrink;
}
