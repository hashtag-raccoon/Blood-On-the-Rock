using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IngridiantSO", menuName = "Game Data/Ingridiants SO")]
public class IngridiantSO : ScriptableObject
{
    [Header("술 목록")]
    public List<Ingridiant> ingridiants_Alchol = new List<Ingridiant>();

    [Header("음료 목록")]
    public List<Ingridiant> ingridiants_Drink = new List<Ingridiant>();

    [Header("얼음 목록")]
    public List<Ingridiant> ingridiants_Ice = new List<Ingridiant>();

    [Header("가니쉬 목록")]
    public List<Ingridiant> ingridiants_Garnish = new List<Ingridiant>();

}