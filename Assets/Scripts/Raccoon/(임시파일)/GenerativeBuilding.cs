using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerativeBuilding : BuildingBase
{
    [Header("최대 생산 슬롯 수")]
    [SerializeField] private int maxProductionSlots = 4;

    // 현재 활성화된 생산 데이터 리스트
    private List<BuildingProductionData> activeProductions = new List<BuildingProductionData>();
    
    // 슬롯 당 생산 타이머 (초 단위)
    private List<float> productionTimers = new List<float>();
    
    private GenerativeBuildingScrollUI scrollUI;

    protected override void Start()
    {
        for (int i = 0; i < maxProductionSlots; i++)
        {
            activeProductions.Add(null);
            productionTimers.Add(0f);
        }
        
        scrollUI = BuildingUI?.GetComponent<GenerativeBuildingScrollUI>();
        if (scrollUI != null)
        {
            scrollUI.Initialize(this);
        }
    }

    void Update()
    {
        UpdateAllProductionSlots();
    }

    // 모든 생산 슬롯 업데이트
    private void UpdateAllProductionSlots()
    {
        for (int i = 0; i < maxProductionSlots; i++)
        {
            // 활성화된 생산 슬롯인지 확인
            if (activeProductions[i] != null)
            {
                // 슬롯 당 생산 타이머 업데이트
                productionTimers[i] += Time.deltaTime;

                // 생산 완료 여부 확인 (minutes -> seconds 변환)
                float productionTimeInSeconds = activeProductions[i].base_production_time_minutes * 60f;
                if (productionTimers[i] >= productionTimeInSeconds)
                {
                    CompleteProduction(i);
                }
            }
        }
    }

    // 생산 시작
    public bool StartProduction(BuildingProductionData productionData, int slotIndex)
    {
        // 슬롯 인덱스 유효성 검사
        if (slotIndex < 0 || slotIndex >= maxProductionSlots)
        {
            Debug.LogWarning($"유효하지 않은 슬롯 인덱스: {slotIndex}");
            return false;
        }

        // 슬롯이 이미 사용 중인지 확인
        if (activeProductions[slotIndex] != null)
        {
            Debug.LogWarning($"슬롯 {slotIndex}는 이미 사용 중입니다.");
            return false;
        }

        // 자원 소비 실패 시 디버깅 (생산 데이터에 따라 다름)
        if (!ConsumeResources(productionData))
        {
            Debug.LogWarning("자원 소비에 실패했습니다.");
            return false;
        }

        // 슬롯에 생산 데이터 할당
        activeProductions[slotIndex] = productionData;
        productionTimers[slotIndex] = 0f;

        Debug.Log($"슬롯 {slotIndex}에 생산 데이터 할당: {productionData.building_type}");
        return true;
    }

    // 자원 소비 함수 (생산 데이터에 따라 다름)
    private bool ConsumeResources(BuildingProductionData productionData)
    {
        // 추후 구현 예정
        return true;
    }

    // 생산 완료 처리
    private void CompleteProduction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxProductionSlots)
            return;

        BuildingProductionData productionData = activeProductions[slotIndex];
        
        if (productionData != null)
        {
            // 생산 자원 추가
            goodsData resource = DataManager.instance.GetResourceById(productionData.resource_id);
            if (resource != null)
            {
                resource.amount += productionData.output_amount;
                Debug.Log($"{resource.goodsName} {productionData.output_amount}�� ���� �Ϸ�!");
            }
        }

        ClearSlot(slotIndex);

        if (scrollUI != null)
        {
            scrollUI.OnProductionCompleted(slotIndex);
        }
    }

    // 생산 취소 처리
    public void CancelProduction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxProductionSlots)
            return;

        BuildingProductionData productionData = activeProductions[slotIndex];
        
        if (productionData != null)
        {
            // 진행 상황 가져오기
            float progress = GetProductionProgress(slotIndex);

            // 자원 환급
            RefundResources(productionData, progress);
            
            Debug.Log($"슬롯 {slotIndex} 생산 취소 (진행 상황: {progress:P0})");
            
            // 슬롯 초기화
            ClearSlot(slotIndex);
        }
    }

    // 자원 환급 처리
    private void RefundResources(BuildingProductionData productionData, float progress)
    {
        // 추후 구현 예정
        return;
    }

    private void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxProductionSlots)
        {
            activeProductions[slotIndex] = null;
            productionTimers[slotIndex] = 0f;
        }
    }

    // 생산 데이터 가져오기
    public BuildingProductionData GetProductionData(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxProductionSlots)
        {
            return activeProductions[slotIndex];
        }
        return null;
    }

    // 생산 진행 상황 가져오기 (0.0 ~ 1.0)
    public float GetProductionProgress(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxProductionSlots)
        {
            BuildingProductionData productionData = activeProductions[slotIndex];
            if (productionData != null)
            {
                float productionTimeInSeconds = productionData.base_production_time_minutes * 60f;
                return Mathf.Clamp01(productionTimers[slotIndex] / productionTimeInSeconds);
            }
        }
        return 0f;
    }

    // 생산 데이터 가져오기 (슬롯 당 남은 시간 반환)
    public float GetRemainingTime(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxProductionSlots)
        {
            BuildingProductionData productionData = activeProductions[slotIndex];
            if (productionData != null)
            {
                float productionTimeInSeconds = productionData.base_production_time_minutes * 60f;
                return Mathf.Max(0, productionTimeInSeconds - productionTimers[slotIndex]);
            }
        }
        return 0f;
    }

    // 생산 슬롯 활성화 여부 확인
    public bool IsSlotActive(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxProductionSlots)
        {
            return activeProductions[slotIndex] != null;
        }
        return false;
    }

    // 최대 생산 슬롯 수 가져오기
    public int GetMaxProductionSlots() => maxProductionSlots;

    // 활성화된 생산 슬롯 수 가져오기
    public int GetActiveProductionCount()
    {
        int count = 0;
        for (int i = 0; i < maxProductionSlots; i++)
        {
            if (activeProductions[i] != null)
                count++;
        }
        return count;
    }

    // 빈 슬롯 찾기 (최초 발견 슬롯 인덱스 반환, 없으면 -1)
    public int FindEmptySlot()
    {
        for (int i = 0; i < maxProductionSlots; i++)
        {
            if (activeProductions[i] == null)
                return i;
        }
        return -1; // 빈 슬롯 없음
    }

    // 모든 슬롯 정보 출력 (디버그용)
    public void PrintAllSlots()
    {
        for (int i = 0; i < maxProductionSlots; i++)
        {
            if (activeProductions[i] != null)
            {
                Debug.Log($"슬롯 {i}: {activeProductions[i].building_type} - 진행 상황: {GetProductionProgress(i):P0}, 남은 시간: {GetRemainingTime(i):F1}초");
            }
            else
            {
                Debug.Log($"슬롯 {i}: 비어있음");
            }
        }
    }
}
