using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인테리어 스크롤 UI를 여는 버튼
/// 이 스크립트를 버튼에 추가하고 InteriorScrollUI를 할당하면 됩니다.
/// </summary>
public class InteriorScrollOpenButton : MonoBehaviour
{
    [Header("인테리어 스크롤 UI")]
    [Tooltip("열고 싶은 InteriorScrollUI GameObject를 여기에 드래그하세요")]
    [SerializeField] private InteriorScrollUI interiorScrollUI;
    
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        
        // InteriorScrollUI를 자동으로 찾기 (할당되지 않은 경우)
        if (interiorScrollUI == null)
        {
            interiorScrollUI = FindObjectOfType<InteriorScrollUI>();
        }
    }

    private void Start()
    {
        if (button != null && interiorScrollUI != null)
        {
            button.onClick.AddListener(OpenInteriorScroll);
        }
        else
        {
            Debug.LogWarning("InteriorScrollOpenButton: Button 또는 InteriorScrollUI가 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 인테리어 스크롤 UI 열기
    /// </summary>
    private void OpenInteriorScroll()
    {
        if (interiorScrollUI != null)
        {
            // InteriorScrollUI 열기
            interiorScrollUI.OpenUI();
        }
    }
}

