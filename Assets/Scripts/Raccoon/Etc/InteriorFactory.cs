using UnityEngine;
using System;

/// <summary>
/// 인테리어 오브젝트를 동적으로 생성하는 클래스
/// InteriorData를 기반으로 필요한 컴포넌트를 자동으로 추가함
/// </summary>
public static class InteriorFactory
{
    /// <summary>
    /// InteriorData로부터 완전한 인테리어 GameObject를 생성합니다.
    /// </summary>
    /// <returns>생성된 인테리어 GameObject</returns>
    public static GameObject CreateInterior(InteriorData interiorData, Vector3 position, Transform parent = null)
    {
        if (interiorData == null || interiorData.interior_sprite == null)
        {
            return null;
        }

        GameObject interiorObj = new GameObject($"{interiorData.Interior_Name}_{interiorData.interior_id}");
        interiorObj.transform.position = position;
        
        if (parent != null)
        {
            interiorObj.transform.SetParent(parent);
        }

        // 기본 컴포넌트 추가
        AddBasicComponents(interiorObj, interiorData);

        return interiorObj;
    }

    /// <summary>
    /// 모든 인테리어에 공통으로 필요한 기본 컴포넌트 추가
    /// </summary>
    private static void AddBasicComponents(GameObject interiorObj, InteriorData interiorData)
    {
        // SpriteRenderer 추가, 스프라이트 세팅
        SpriteRenderer spriteRenderer = interiorObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = interiorData.interior_sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 3; // 건물보다 위에 표시되도록

        // BoxCollider2D 추가 (드래그 앤 드롭을 위해 필요)
        BoxCollider2D collider = interiorObj.AddComponent<BoxCollider2D>();
        if (interiorData.tileSize.x > 0 && interiorData.tileSize.y > 0)
        {
            collider.size = new Vector2(interiorData.tileSize.x, interiorData.tileSize.y);
        }

        // InteriorBase 컴포넌트 추가 및 필드 할당
        InteriorBase interiorBase = interiorObj.AddComponent<InteriorBase>();
        AssignInteriorBaseFields(interiorBase, interiorData);
    }

    /// <summary>
    /// InteriorBase의 필드들을 자동으로 할당시키는 메소드
    /// </summary>
    private static void AssignInteriorBaseFields(InteriorBase interiorBase, InteriorData interiorData)
    {
        if (interiorBase == null || interiorData == null) return;

        // Reflection을 사용하여 private/protected 필드 할당
        var type = typeof(InteriorBase);

        // interiorId 할당
        SetField(type, interiorBase, "interiorId", interiorData.interior_id);

        // InteriorSprite 할당
        SetField(type, interiorBase, "InteriorSprite", interiorData.interior_sprite);

        // tileSize 할당
        SetField(type, interiorBase, "tileSize", interiorData.tileSize);

        // DragDropController 할당
        DragDropController dragDropController = UnityEngine.Object.FindObjectOfType<DragDropController>();
        SetField(type, interiorBase, "dragDropController", dragDropController);
    }

    /// <summary>
    /// Reflection을 사용하여 private/protected 필드에 값 할당
    /// </summary>
    private static void SetField(Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(instance, value);
        }
        else
        {
            Debug.LogError($"[InteriorFactory.SetField] FAILED - Field '{fieldName}' not found in {type.Name}");
        }
    }
}

