using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D;
using System.Collections.Generic;

/// <summary>
/// 여러 캐릭터를 자동으로 생성하고 리깅 설정하는 스크립트
/// Editor에서만 사용됩니다.
/// </summary>
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D.Animation;
using UnityEditor.Animations;

public class MultiCharacterRigSetup : EditorWindow
{
    [MenuItem("Tools/Multi Character Rig Setup")]
    public static void ShowWindow()
    {
        GetWindow<MultiCharacterRigSetup>("Multi Character Setup");
    }
    
    private string[] characterNames = { "Human2", "Human3", "Oak1", "Oak2", "Oak3", "Player1", "Vampire1", "Vampire2", "Vampire3" };
    private bool[] characterToggles = new bool[9];
    private Vector2 scrollPosition;
    
    private void OnGUI()
    {
        GUILayout.Label("다중 캐릭터 리깅 자동 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("생성할 캐릭터를 선택하고 '모든 캐릭터 생성' 버튼을 클릭하세요.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < characterNames.Length; i++)
        {
            characterToggles[i] = EditorGUILayout.Toggle(characterNames[i], characterToggles[i]);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("모든 캐릭터 생성", GUILayout.Height(30)))
        {
            CreateAllCharacters();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("선택된 캐릭터만 생성", GUILayout.Height(30)))
        {
            CreateSelectedCharacters();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("참고: 스켈레톤과 스프라이트 스킨 설정은 Unity 에디터의 2D Animation 창에서 수동으로 설정해야 합니다.", MessageType.Info);
    }
    
    private void CreateAllCharacters()
    {
        for (int i = 0; i < characterNames.Length; i++)
        {
            CreateCharacter(characterNames[i]);
        }
        Debug.Log("모든 캐릭터 생성 완료!");
    }
    
    private void CreateSelectedCharacters()
    {
        for (int i = 0; i < characterNames.Length; i++)
        {
            if (characterToggles[i])
            {
                CreateCharacter(characterNames[i]);
            }
        }
        Debug.Log("선택된 캐릭터 생성 완료!");
    }
    
    private void CreateCharacter(string characterName)
    {
        string resourcePath = $"Image/Charactor/{characterName}";
        
        // 스프라이트 로드
        Dictionary<string, Sprite> sprites = LoadCharacterSprites(characterName, resourcePath);
        
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning($"{characterName}: 스프라이트를 찾을 수 없습니다.");
            return;
        }
        
        // 씬에 캐릭터 루트 생성
        GameObject characterRoot = new GameObject(characterName);
        characterRoot.transform.position = Vector3.zero;
        
        // 캐릭터 구조 생성
        CreateCharacterStructure(characterRoot, sprites, characterName);
        
        // Sorting Order 설정
        SetSortingOrders(characterRoot);
        
        // Animator 컴포넌트 추가
        Animator animator = characterRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = characterRoot.AddComponent<Animator>();
        }
        
        // Animator Controller 생성 및 할당
        RuntimeAnimatorController controller = CreateAnimatorController(characterName);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }
        
        Debug.Log($"{characterName} 캐릭터 생성 완료!");
        Debug.Log($"다음 단계: Unity 에디터에서 {characterName}을 선택하고 Window > 2D > Animation에서 스켈레톤을 생성하세요.");
    }
    
    private Dictionary<string, Sprite> LoadCharacterSprites(string characterName, string resourcePath)
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        
        // 파일 이름 패턴 처리 (Oak1의 경우 bodypng)
        string bodyFileName = characterName == "Oak1" ? $"{characterName}_bodypng" : $"{characterName}_body";
        
        string[] spriteNames = {
            bodyFileName,
            $"{characterName}_bodyback",
            $"{characterName}_head",
            $"{characterName}_headback",
            $"{characterName}_hair",
            $"{characterName}_hairback",
            $"{characterName}_armfront",
            $"{characterName}_armback",
            $"{characterName}_legfront",
            $"{characterName}_legback"
        };
        
        foreach (string spriteName in spriteNames)
        {
            Sprite sprite = Resources.Load<Sprite>($"{resourcePath}/{spriteName}");
            if (sprite != null)
            {
                string key = spriteName.Replace($"{characterName}_", "");
                if (key == "bodypng") key = "body";
                sprites[key] = sprite;
            }
            else
            {
                Debug.LogWarning($"{characterName}: {spriteName} 스프라이트를 찾을 수 없습니다.");
            }
        }
        
        return sprites;
    }
    
    private void CreateCharacterStructure(GameObject characterRoot, Dictionary<string, Sprite> sprites, string characterName)
    {
        // Body 계층 구조 생성
        if (sprites.ContainsKey("bodyback"))
            CreateSpritePart("BodyBack", sprites["bodyback"], characterRoot.transform, new Vector3(0, 0, 0), 0);
        if (sprites.ContainsKey("body"))
            CreateSpritePart("Body", sprites["body"], characterRoot.transform, new Vector3(0, 0, 0), 2);
        
        // Head 계층 구조
        GameObject headParent = new GameObject("HeadParent");
        headParent.transform.SetParent(characterRoot.transform);
        headParent.transform.localPosition = new Vector3(0, 0.5f, 0);
        
        if (sprites.ContainsKey("headback"))
            CreateSpritePart("HeadBack", sprites["headback"], headParent.transform, Vector3.zero, 3);
        if (sprites.ContainsKey("head"))
            CreateSpritePart("Head", sprites["head"], headParent.transform, Vector3.zero, 6);
        if (sprites.ContainsKey("hairback"))
            CreateSpritePart("HairBack", sprites["hairback"], headParent.transform, Vector3.zero, 4);
        if (sprites.ContainsKey("hair"))
            CreateSpritePart("Hair", sprites["hair"], headParent.transform, Vector3.zero, 7);
        
        // Arm 계층 구조
        if (sprites.ContainsKey("armback"))
        {
            CreateSpritePart("ArmBack_L", sprites["armback"], characterRoot.transform, new Vector3(-0.3f, 0.3f, 0), 5);
            CreateSpritePart("ArmBack_R", sprites["armback"], characterRoot.transform, new Vector3(0.3f, 0.3f, 0), 5);
        }
        
        if (sprites.ContainsKey("armfront"))
        {
            CreateSpritePart("ArmFront_L", sprites["armfront"], characterRoot.transform, new Vector3(-0.3f, 0.3f, 0), 8);
            CreateSpritePart("ArmFront_R", sprites["armfront"], characterRoot.transform, new Vector3(0.3f, 0.3f, 0), 8);
        }
        
        // Leg 계층 구조
        if (sprites.ContainsKey("legback"))
        {
            CreateSpritePart("LegBack_L", sprites["legback"], characterRoot.transform, new Vector3(-0.15f, -0.5f, 0), 1);
            CreateSpritePart("LegBack_R", sprites["legback"], characterRoot.transform, new Vector3(0.15f, -0.5f, 0), 1);
        }
        
        if (sprites.ContainsKey("legfront"))
        {
            CreateSpritePart("LegFront_L", sprites["legfront"], characterRoot.transform, new Vector3(-0.15f, -0.5f, 0), 9);
            CreateSpritePart("LegFront_R", sprites["legfront"], characterRoot.transform, new Vector3(0.15f, -0.5f, 0), 9);
        }
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
    
    private void SetSortingOrders(GameObject characterRoot)
    {
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
    }
    
    private RuntimeAnimatorController CreateAnimatorController(string characterName)
    {
        string controllerPath = $"Assets/Animation/{characterName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log($"{characterName} Animator Controller 생성: {controllerPath}");
        }
        
        return controller;
    }
}
#endif

