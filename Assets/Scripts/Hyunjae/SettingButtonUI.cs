using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 항목 버튼 UI
/// BuildInteriorButtonUI와 유사한 구조로 설정 항목을 표시하는 버튼
/// </summary>
public class SettingButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image settingIconImage;
    [SerializeField] private TextMeshProUGUI settingNameText;
    [SerializeField] private Button settingButton;

    private object settingData;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        settingData = data;

        var setting = data as SettingData;

        if (settingNameText != null)
        {
            settingNameText.text = setting.Setting_Name;
        }

        if (settingIconImage != null && setting.icon != null)
        {
            settingIconImage.sprite = setting.icon;
        }

        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners();
            settingButton.onClick.AddListener(() =>
            {
                onClickCallback?.Invoke(this);
            });
        }
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)settingData;
    }
}

