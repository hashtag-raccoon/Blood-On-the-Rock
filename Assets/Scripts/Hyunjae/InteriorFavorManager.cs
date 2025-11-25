using UnityEngine;

/// <summary>
/// 인테리어 배치 관련 호감도 보상을 관리하는 매니저
/// </summary>
public static class InteriorFavorManager
{
    /// <summary>
    /// 인테리어를 ExistingTilemap에 배치했을 때 호출하여 호감도를 증가시킴
    /// </summary>
    /// <param name="amount">증가시킬 호감도 수치</param>
    public static void AddFavorFromPlacement(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[InteriorFavorManager] DataManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        DataManager.Instance.storeFavor += amount;
        Debug.Log($"[InteriorFavorManager] 인테리어 배치 보상: 호감도 +{amount}, 현재 호감도 = {DataManager.Instance.storeFavor}");
    }
}


