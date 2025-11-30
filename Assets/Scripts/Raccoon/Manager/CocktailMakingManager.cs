using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocktailMakingManager : MonoBehaviour
{
    public static CocktailMakingManager _instance;

    private void Awake()
    {
        _instance = this;
    }

    [Header("칵테일 제작 UI 할당")]
    public CocktailMakingUI cocktailMakingUI; // UI 할당

    public void MakingStart()
    {
        cocktailMakingUI.OpenCocktailMakingUI();
    }

    public void MakingStop()
    {
        cocktailMakingUI.CloseCocktailMakingUI();
    }

    public bool GetActiveObject()
    {
        return cocktailMakingUI.GestActive();
    }
}
