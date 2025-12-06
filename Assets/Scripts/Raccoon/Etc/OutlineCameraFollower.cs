using UnityEngine;

/// <summary>
/// 카메라가 특정 타겟을 따라가며, 타겟의 크기에 맞게 카메라의 위치와 크기를 조정하는 기능을 제공
/// 외곽선 렌더링용 카메라에 사용
/// - 타겟의 모든 Renderer를 고려하여 Bounds 계산
/// - 패딩 적용 가능
/// - 카메라 위치와 Orthographic Size 즉시 업데이트
/// 등의 기능이 있음
/// </summary>
public class OutlineCameraFollower : MonoBehaviour
{
    public Camera outlineCam;
    public Transform target;
    
    [Range(0f, 1f)]
    public float padding = 0.2f;

    private void LateUpdate()
    {
        if (target != null && outlineCam != null)
        {
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// 카메라 위치와 크기를 즉시 업데이트
    /// </summary>
    public void UpdateCameraPosition()
    {
        if (target == null || outlineCam == null)
            return;

        // 타겟의 바운드(경계) 계산
        Bounds bounds = CalculateBounds(target);

        if (bounds.size == Vector3.zero)
        {
            // Bounds가 0이면 기본 크기 사용
            bounds = new Bounds(target.position, Vector3.one * 2f);
        }

        // 패딩 적용
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
        float paddedSize = maxSize * (1f + padding * 2f);

        // 카메라 위치 설정 (타겟 중심에서 앞쪽으로)
        Vector3 cameraPosition = bounds.center;
        cameraPosition.z = bounds.center.z - 10f; // 타겟 앞쪽 10유닛

        outlineCam.transform.position = cameraPosition;
        
        // Orthographic Size 설정
        outlineCam.orthographicSize = paddedSize / 2f;

        // 카메라가 정면을 바라보도록 설정
        outlineCam.transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// 타겟의 모든 Renderer를 고려한 Bounds 계산
    /// 이거때문에 스프라이트 렌더러가 없어도, 바운드를 계산하여 외곽선을 그릴 수 있음
    /// </summary>
    private Bounds CalculateBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // Renderer가 없으면 기본 크기 반환
            return new Bounds(target.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}