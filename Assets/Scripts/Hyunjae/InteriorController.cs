using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가게 인테리어 시스템 컨트롤러
/// 
/// [이 스크립트가 하는 일]
/// 1. 스프라이트 이미지를 "인테리어 부품"으로 등록
/// 2. 등록한 부품을 원하는 위치에 배치 (의자, 테이블, 장식품 등)
/// 3. 배치된 인테리어를 관리 (제거, 정리 등)
/// 
/// [사용 방법]
/// 1. Unity에서 GameObject에 이 컴포넌트 추가
/// 2. Inspector에서 "Interior Parts" 리스트에 스프라이트 설정
/// 3. 게임 실행 후 마우스 왼쪽 클릭으로 배치 (또는 코드로 PlaceInterior 호출)
/// </summary>
public class InteriorController : MonoBehaviour
{
    /// <summary>
    /// 인테리어 부품 정보를 저장하는 클래스
    /// Inspector에서 각 부품의 스프라이트를 설정할 수 있습니다
    /// </summary>
    [System.Serializable]
    public class InteriorPart
    {
        public string partName;           // 부품 이름 (예: "의자", "테이블", "꽃병")
        public Sprite sprite;              // 부품 스프라이트 (실제로 화면에 보이는 이미지)
        public Vector2 size;              // 부품 크기 (선택사항, 현재는 사용 안 함)
        public int sortingOrder;          // 렌더링 순서 (숫자가 클수록 앞에 표시됨)
    }

    [Header("인테리어 부품 설정")]
    [Tooltip("여기에 사용할 인테리어 스프라이트들을 등록하세요. 예: 의자, 테이블, 장식품 등")]
    [SerializeField] private List<InteriorPart> interiorParts = new List<InteriorPart>();
    
    [Header("인테리어 배치 설정")]
    [Tooltip("인테리어들이 배치될 부모 오브젝트 (없으면 자동 생성)")]
    [SerializeField] private Transform interiorParent;
    
    [Tooltip("렌더링 레이어 이름 (기본값: Default)")]
    [SerializeField] private string sortingLayerName = "Default";
    
    [Tooltip("그리드 크기 - 배치 시 이 크기 단위로 정렬됩니다 (기본값: 1.0)")]
    [SerializeField] private float gridSize = 1f;
    
    [Header("현재 배치된 인테리어")]
    [Tooltip("현재 씬에 배치된 모든 인테리어 오브젝트 목록 (읽기 전용)")]
    [SerializeField] private List<GameObject> placedInteriors = new List<GameObject>();

    private void Start()
    {
        // 인테리어 부모가 없으면 자동 생성
        if (interiorParent == null)
        {
            GameObject parentObj = new GameObject("InteriorParent");
            parentObj.transform.SetParent(transform);
            interiorParent = parentObj.transform;
        }
    }

    /// <summary>
    /// 인테리어 부품을 추가합니다.
    /// </summary>
    public void AddInteriorPart(string partName, Sprite sprite, Vector2 size, int sortingOrder = 0)
    {
        InteriorPart newPart = new InteriorPart
        {
            partName = partName,
            sprite = sprite,
            size = size,
            sortingOrder = sortingOrder
        };
        
        interiorParts.Add(newPart);
    }

    /// <summary>
    /// 인테리어 부품을 특정 위치에 배치합니다.
    /// </summary>
    public GameObject PlaceInterior(int partIndex, Vector3 position)
    {
        if (partIndex < 0 || partIndex >= interiorParts.Count)
        {
            Debug.LogWarning($"인테리어 부품 인덱스 {partIndex}가 유효하지 않습니다.");
            return null;
        }

        InteriorPart part = interiorParts[partIndex];
        
        if (part.sprite == null)
        {
            Debug.LogWarning($"인테리어 부품 '{part.partName}'의 스프라이트가 설정되지 않았습니다.");
            return null;
        }

        // 그리드에 맞춰 위치 조정
        Vector3 gridPosition = new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            position.z
        );

        // 인테리어 오브젝트 생성
        GameObject interiorObj = new GameObject($"Interior_{part.partName}_{placedInteriors.Count}");
        interiorObj.transform.SetParent(interiorParent);
        interiorObj.transform.position = gridPosition;

        // SpriteRenderer 컴포넌트 추가
        SpriteRenderer spriteRenderer = interiorObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = part.sprite;
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = part.sortingOrder;

        // 부품 정보 저장을 위한 컴포넌트 추가 (선택사항)
        InteriorItem interiorItem = interiorObj.AddComponent<InteriorItem>();
        interiorItem.Initialize(partIndex, part);

        placedInteriors.Add(interiorObj);
        
        Debug.Log($"인테리어 '{part.partName}'을(를) {gridPosition}에 배치했습니다.");
        
        return interiorObj;
    }

    /// <summary>
    /// 인테리어 부품을 이름으로 찾아 배치합니다.
    /// </summary>
    public GameObject PlaceInteriorByName(string partName, Vector3 position)
    {
        int index = interiorParts.FindIndex(p => p.partName == partName);
        if (index == -1)
        {
            Debug.LogWarning($"인테리어 부품 '{partName}'을(를) 찾을 수 없습니다.");
            return null;
        }
        
        return PlaceInterior(index, position);
    }

    /// <summary>
    /// 배치된 인테리어를 제거합니다.
    /// </summary>
    public void RemoveInterior(GameObject interiorObj)
    {
        if (placedInteriors.Contains(interiorObj))
        {
            placedInteriors.Remove(interiorObj);
            Destroy(interiorObj);
        }
    }

    /// <summary>
    /// 모든 배치된 인테리어를 제거합니다.
    /// </summary>
    public void ClearAllInteriors()
    {
        foreach (GameObject obj in placedInteriors)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        placedInteriors.Clear();
    }

    /// <summary>
    /// 인테리어 부품 목록을 가져옵니다.
    /// </summary>
    public List<InteriorPart> GetInteriorParts()
    {
        return new List<InteriorPart>(interiorParts);
    }

    /// <summary>
    /// 배치된 인테리어 목록을 가져옵니다.
    /// </summary>
    public List<GameObject> GetPlacedInteriors()
    {
        return new List<GameObject>(placedInteriors);
    }

    // ============================================
    // 사용 예시 메서드들 (테스트/데모용)
    // ============================================

    /// <summary>
    /// 예시: 마우스 클릭 위치에 첫 번째 인테리어 부품 배치 (테스트용)
    /// </summary>
    private void Update()
    {
        // 마우스 왼쪽 클릭으로 인테리어 배치 (테스트용)
        if (Input.GetMouseButtonDown(0) && interiorParts.Count > 0)
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // 2D 게임이므로 Z는 0

            // 첫 번째 인테리어 부품을 클릭한 위치에 배치
            PlaceInterior(0, mousePos);
        }

        // 마우스 오른쪽 클릭으로 모든 인테리어 제거 (테스트용)
        if (Input.GetMouseButtonDown(1))
        {
            ClearAllInteriors();
        }
    }

    /// <summary>
    /// 예시: 인테리어를 자동으로 배치하는 데모 (테스트용)
    /// </summary>
    [ContextMenu("데모: 인테리어 자동 배치")]
    public void DemoAutoPlaceInteriors()
    {
        if (interiorParts.Count == 0)
        {
            Debug.LogWarning("배치할 인테리어 부품이 없습니다. Inspector에서 스프라이트를 설정해주세요.");
            return;
        }

        // 기존 인테리어 제거
        ClearAllInteriors();

        // 간단한 격자 패턴으로 인테리어 배치
        int count = 0;
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (count >= interiorParts.Count) break;

                Vector3 position = new Vector3(x * 2f, y * 2f, 0);
                PlaceInterior(count % interiorParts.Count, position);
                count++;
            }
        }

        Debug.Log($"데모: {count}개의 인테리어를 배치했습니다.");
    }
}

/// <summary>
/// 인테리어 아이템 정보를 저장하는 컴포넌트
/// </summary>
public class InteriorItem : MonoBehaviour
{
    public int partIndex;
    public InteriorController.InteriorPart partData;

    public void Initialize(int index, InteriorController.InteriorPart data)
    {
        partIndex = index;
        partData = data;
    }
}
