using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// 캐릭터 리깅 설정을 자동화하는 헬퍼 스크립트
/// Editor에서만 사용됩니다.
/// </summary>
#if UNITY_EDITOR
using UnityEditor;

public class CharacterRigSetupHelper : EditorWindow
{
    [MenuItem("Tools/Character Rig Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<CharacterRigSetupHelper>("Character Rig Setup");
    }
    
    private GameObject characterRoot;
    private Sprite bodySprite;
    private Sprite bodyBackSprite;
    private Sprite headSprite;
    private Sprite headBackSprite;
    private Sprite hairSprite;
    private Sprite hairBackSprite;
    private Sprite armFrontSprite;
    private Sprite armBackSprite;
    private Sprite legFrontSprite;
    private Sprite legBackSprite;
    
    private void OnGUI()
    {
        GUILayout.Label("캐릭터 리깅 자동 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        characterRoot = EditorGUILayout.ObjectField("캐릭터 루트", characterRoot, typeof(GameObject), true) as GameObject;
        
        EditorGUILayout.Space();
        GUILayout.Label("스프라이트 할당", EditorStyles.boldLabel);
        
        bodySprite = EditorGUILayout.ObjectField("Body", bodySprite, typeof(Sprite), false) as Sprite;
        bodyBackSprite = EditorGUILayout.ObjectField("Body Back", bodyBackSprite, typeof(Sprite), false) as Sprite;
        headSprite = EditorGUILayout.ObjectField("Head", headSprite, typeof(Sprite), false) as Sprite;
        headBackSprite = EditorGUILayout.ObjectField("Head Back", headBackSprite, typeof(Sprite), false) as Sprite;
        hairSprite = EditorGUILayout.ObjectField("Hair", hairSprite, typeof(Sprite), false) as Sprite;
        hairBackSprite = EditorGUILayout.ObjectField("Hair Back", hairBackSprite, typeof(Sprite), false) as Sprite;
        armFrontSprite = EditorGUILayout.ObjectField("Arm Front", armFrontSprite, typeof(Sprite), false) as Sprite;
        armBackSprite = EditorGUILayout.ObjectField("Arm Back", armBackSprite, typeof(Sprite), false) as Sprite;
        legFrontSprite = EditorGUILayout.ObjectField("Leg Front", legFrontSprite, typeof(Sprite), false) as Sprite;
        legBackSprite = EditorGUILayout.ObjectField("Leg Back", legBackSprite, typeof(Sprite), false) as Sprite;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("캐릭터 구조 생성"))
        {
            CreateCharacterStructure();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Sorting Order 자동 설정"))
        {
            SetSortingOrders();
        }
    }
    
    private void CreateCharacterStructure()
    {
        if (characterRoot == null)
        {
            Debug.LogError("캐릭터 루트를 설정해주세요.");
            return;
        }
        
        // 기존 자식 제거
        while (characterRoot.transform.childCount > 0)
        {
            DestroyImmediate(characterRoot.transform.GetChild(0).gameObject);
        }
        
        // Body 계층 구조 생성
        CreateSpritePart("BodyBack", bodyBackSprite, characterRoot.transform, new Vector3(0, 0, 0), 0);
        CreateSpritePart("Body", bodySprite, characterRoot.transform, new Vector3(0, 0, 0), 2);
        
        // Head 계층 구조
        GameObject headParent = new GameObject("HeadParent");
        headParent.transform.SetParent(characterRoot.transform);
        headParent.transform.localPosition = new Vector3(0, 0.5f, 0);
        
        CreateSpritePart("HeadBack", headBackSprite, headParent.transform, Vector3.zero, 3);
        CreateSpritePart("Head", headSprite, headParent.transform, Vector3.zero, 6);
        CreateSpritePart("HairBack", hairBackSprite, headParent.transform, Vector3.zero, 4);
        CreateSpritePart("Hair", hairSprite, headParent.transform, Vector3.zero, 7);
        
        // Arm 계층 구조
        GameObject armL = CreateSpritePart("ArmBack_L", armBackSprite, characterRoot.transform, new Vector3(-0.3f, 0.3f, 0), 5);
        GameObject armR = CreateSpritePart("ArmBack_R", armBackSprite, characterRoot.transform, new Vector3(0.3f, 0.3f, 0), 5);
        
        GameObject armFrontL = CreateSpritePart("ArmFront_L", armFrontSprite, characterRoot.transform, new Vector3(-0.3f, 0.3f, 0), 8);
        GameObject armFrontR = CreateSpritePart("ArmFront_R", armFrontSprite, characterRoot.transform, new Vector3(0.3f, 0.3f, 0), 8);
        
        // Leg 계층 구조
        CreateSpritePart("LegBack_L", legBackSprite, characterRoot.transform, new Vector3(-0.15f, -0.5f, 0), 1);
        CreateSpritePart("LegBack_R", legBackSprite, characterRoot.transform, new Vector3(0.15f, -0.5f, 0), 1);
        CreateSpritePart("LegFront_L", legFrontSprite, characterRoot.transform, new Vector3(-0.15f, -0.5f, 0), 9);
        CreateSpritePart("LegFront_R", legFrontSprite, characterRoot.transform, new Vector3(0.15f, -0.5f, 0), 9);
        
        Debug.Log("캐릭터 구조 생성 완료!");
    }
    
    private GameObject CreateSpritePart(string name, Sprite sprite, Transform parent, Vector3 localPos, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Character";
        sr.sortingOrder = sortingOrder;
        
        return obj;
    }
    
    private void SetSortingOrders()
    {
        if (characterRoot == null) return;
        
        SpriteRenderer[] renderers = characterRoot.GetComponentsInChildren<SpriteRenderer>();
        
        foreach (var renderer in renderers)
        {
            string name = renderer.name.ToLower();
            
            if (name.Contains("back"))
            {
                if (name.Contains("leg")) renderer.sortingOrder = 1;
                else if (name.Contains("body")) renderer.sortingOrder = 0;
                else if (name.Contains("head")) renderer.sortingOrder = 3;
                else if (name.Contains("hair")) renderer.sortingOrder = 4;
                else if (name.Contains("arm")) renderer.sortingOrder = 5;
            }
            else if (name.Contains("body"))
            {
                renderer.sortingOrder = 2;
            }
            else if (name.Contains("head"))
            {
                renderer.sortingOrder = 6;
            }
            else if (name.Contains("hair"))
            {
                renderer.sortingOrder = 7;
            }
            else if (name.Contains("arm"))
            {
                renderer.sortingOrder = 8;
            }
            else if (name.Contains("leg"))
            {
                renderer.sortingOrder = 9;
            }
        }
        
        Debug.Log("Sorting Order 설정 완료!");
    }
}
#endif

