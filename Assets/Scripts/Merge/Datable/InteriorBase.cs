using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인테리어 오브젝트의 기본 동작을 정의하는 컴포넌트
/// BuildingBase와 유사하지만 인테리어는 UI나 카메라 기능이 필요 없으므로 더 간단함
/// </summary>
public class InteriorBase : MonoBehaviour, IPointerDownHandler
{
    [Header("인테리어 데이터")]
    [SerializeField] protected int interiorId; // Interior ID
    [SerializeField] protected Sprite InteriorSprite;
    
    [Header("타일맵 크기 설정")]
    [SerializeField] protected Vector2Int tileSize = new Vector2Int(1, 1); // 가로 x 세로로, 인테리어가 차지하는 타일 크기
    [SerializeField] private DragDropController dragDropController;
    
    public Vector2Int TileSize => tileSize;
    public int InteriorId => interiorId;

    protected virtual void Start()
    {
        // DragDropController 자동 할당 (Inspector에 할당되지 않았을 경우를 위함)
        if (dragDropController == null)
            dragDropController = FindObjectOfType<DragDropController>();
    }

    /// <summary>
    /// 인테리어 클릭 시 처리 (편집 모드에서만 반응)
    /// </summary>
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetMouseButtonDown(0) && !DragDropController.Instance.isUI)
        {
            if (dragDropController != null && dragDropController.IsEditMode)
            {
                // 편집 모드에서는 클릭 이벤트를 무시 (드래그만 허용)
                return;
            }
            
            // 인테리어는 건물과 달리 UI가 없으므로 클릭 시 특별한 동작 없음
            // 필요시 여기에 추가 기능 구현 가능
        }
    }
}



