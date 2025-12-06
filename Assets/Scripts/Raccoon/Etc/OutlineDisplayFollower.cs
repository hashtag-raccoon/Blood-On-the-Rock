using System.Collections.Generic;
using UnityEngine;

public struct LayerRestoreData
{
    public GameObject gameObject;
    public int layer;
}

/// <summary>
/// OutlineDisplay가 타겟(알바생)을 따라다니도록 하는 컴포넌트
/// 파괴 시 알바생의 레이어를 원래대로 복원
/// </summary>
public class OutlineDisplayFollower : MonoBehaviour
{
    public Transform target;
    public GameObject arbeitObject;
    public int originalLayer;
    public List<LayerRestoreData> originalLayers;

    private void LateUpdate()
    {
        if (target != null)
        {
            // 타겟의 Bounds 중심을 따라감 (캐릭터 중심점)
            Bounds bounds = CalculateBounds(target);
            transform.position = bounds.center;
        }
    }

    /// <summary>
    /// Transform의 모든 Renderer를 고려한 Bounds 계산
    /// </summary>
    private Bounds CalculateBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            // Renderer가 없으면 타겟 위치 반환
            return new Bounds(target.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void OnDestroy()
    {
        // OutlineDisplay가 제거될 때 알바생의 레이어를 원래대로 복원
        if (originalLayers != null && originalLayers.Count > 0)
        {
            foreach (var data in originalLayers)
            {
                if (data.gameObject != null)
                {
                    data.gameObject.layer = data.layer;
                }
            }
        }
        else if (arbeitObject != null)
        {
            SetLayerRecursively(arbeitObject, originalLayer);
        }
    }

    /// <summary>
    /// 대상 오브젝트와 모든 자식의 레이어를 변경
    /// 재귀함수로 구현함, 깊이 우선 탐색을 위해 Stack(나중에 들어간 데이터가 가장 나중에 빠지는 거)을 사용하지 않음
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
