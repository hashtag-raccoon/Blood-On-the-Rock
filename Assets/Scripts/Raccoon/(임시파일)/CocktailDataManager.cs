using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GlassData
{
    public string id;
    public string glassName;
    public Sprite icon;
    public GameObject glassPrefab;
}

[System.Serializable]
public class BaseSpiritData
{
    public string id;
    public string spiritName;
    public Sprite icon;
    public Color liquidColor = Color.white;
}

[System.Serializable]
public class MixerData
{
    public string id;
    public string mixerName;
    public Sprite icon;
    public Color liquidColor = Color.white;
}

[System.Serializable]
public class GarnishData
{
    public string id;
    public string garnishName;
    public Sprite icon;
    public GameObject garnishPrefab;
}

[System.Serializable]
public class CocktailRecipe
{
    public GlassData selectedGlass;
    public List<BaseSpiritData> selectedSpirits = new List<BaseSpiritData>();
    public List<MixerData> selectedMixers = new List<MixerData>();
    public GarnishData selectedGarnish;

    public bool IsComplete()
    {
        return selectedGlass != null && selectedGarnish != null;
    }
}

public class CocktailDataManager : MonoBehaviour
{
    [Header("Glass Data")]
    public List<GlassData> glasses = new List<GlassData>();

    [Header("Base Spirit Data")]
    public List<BaseSpiritData> baseSpirits = new List<BaseSpiritData>();

    [Header("Mixer Data")]
    public List<MixerData> mixers = new List<MixerData>();

    [Header("Garnish Data")]
    public List<GarnishData> garnishes = new List<GarnishData>();

    [Header("UI Images")]
    public Sprite barSpoonIcon;
    public Sprite shakerIcon;
    public Sprite buttonBackgroundSprite;
    public Sprite panelBackgroundSprite;

    public static CocktailDataManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}