using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class IslandData : ScriptableObject
{
    int island_id;
    public string island_name;
    int island_level;
    int construction_slots;
    int current_building_count;
    int max_bulding_count;
    int required_gold;
    int required_reputaion;
}

public class IslandManager : MonoBehaviour
{
    [Header("데이터 매니저 할당")]
    [SerializeField] private DataManager dataManager;

    [Header("하루 시간 설정(시)")]
    [SerializeField] private float SetDayTime = 3f; // 시간 기준으로 설정할 것
    [SerializeField] private float wait_convertedDayTime = 0;

    [Header("화면 블러 패널")]
    public GameObject BlurUI;

    private Coroutine dayCoroutine;

    [Header("가게 오픈 버튼")]
    [SerializeField] private Button StoreOpenButton;
    [Header("인벤토리 버튼")]
    [SerializeField] private Button InventoryButton;
    [SerializeField] private InventoryUI inventoryUI;


    [Header("좌우 UI들<Build Button 용>")]
    public List<GameObject> leftUI = new List<GameObject>();
    public List<GameObject> rightUI = new List<GameObject>();

    void Start()
    {
        wait_convertedDayTime = SetDayTime * 60 * 60f; // 초 단위로 변환
        StoreOpenButton.onClick.AddListener(StoreOpenButtonClicked);
        InventoryButton.onClick.AddListener(InventoryOpenButton);
        // 낮 -> 밤 코루틴 시작
        dayCoroutine = StartCoroutine(DayCoroutine());
    }

    void Update()
    {
        if (dataManager.storeFavor <= 0)
        {
            // 게임종료
            Debug.Log("가게 호감도가 0이 되어 게임이 종료됩니다.");
        }
    }

    // 낮 -> 밤 코루틴
    IEnumerator DayCoroutine()
    {
        yield return new WaitForSeconds(wait_convertedDayTime);
        OnDayEnd();
    }

    // 하루 종료 시 호출될 함수
    private void OnDayEnd()
    {
        Debug.Log("하루가 종료됨");
        // 다음 씬으로 넘어감
        //SceneManager.LoadScene("BarScene");
    }

    // 하루 종료 후 바로 바로 넘어가기 위한 함수
    private void StoreOpenButtonClicked()
    {
        if (dayCoroutine != null)
        {
            StopCoroutine(dayCoroutine);
            dayCoroutine = null;

            OnDayEnd();
        }
    }

    private void InventoryOpenButton()
    {
        inventoryUI.setactiveInventory();
        inventoryUI.inventroypanel.SetActive(inventoryUI.getactiveInventory());

    }

}
