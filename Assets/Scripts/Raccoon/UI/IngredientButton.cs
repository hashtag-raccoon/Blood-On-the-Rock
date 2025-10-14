using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientButton : MonoBehaviour
{
    private Image iconImage;
    private TextMeshProUGUI nameText;
    private Image selectionBorder;
    private Button button;
    private Image buttonImage;

    private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    private Color selectedColor = new Color(1f, 0.9f, 0.3f, 1f);

    private bool isSelected = false;
    private object ingredientData;

    public static IngredientButton Create(Transform parent)
    {
        GameObject btnObj = new GameObject("IngredientButton");
        btnObj.transform.SetParent(parent, false);

        IngredientButton ingredientBtn = btnObj.AddComponent<IngredientButton>();
        ingredientBtn.BuildUI();

        return ingredientBtn;
    }

    private void BuildUI()
    {
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 180);

        // Button 컴포넌트
        button = gameObject.AddComponent<Button>();
        buttonImage = gameObject.AddComponent<Image>();
        buttonImage.color = normalColor;

        // 배경 (약간 어두운 패널)
        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 아이콘 이미지
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 20);
        iconRect.sizeDelta = new Vector2(100, 100);
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;

        // 이름 텍스트
        GameObject textObj = new GameObject("Name");
        textObj.transform.SetParent(transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.anchoredPosition = new Vector2(0, 30);
        textRect.sizeDelta = new Vector2(-20, 40);
        nameText = textObj.AddComponent<TextMeshProUGUI>();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 16;
        nameText.color = Color.white;

        // 선택 테두리
        GameObject borderObj = new GameObject("SelectionBorder");
        borderObj.transform.SetParent(transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-5, -5);
        borderRect.offsetMax = new Vector2(5, 5);
        selectionBorder = borderObj.AddComponent<Image>();
        selectionBorder.color = selectedColor;
        selectionBorder.gameObject.SetActive(false);

        // Outline 컴포넌트로 테두리 효과
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = selectedColor;
        outline.effectDistance = new Vector2(3, 3);
    }

    public void SetupGlass(GlassData data, System.Action<IngredientButton> onClickCallback)
    {
        ingredientData = data;
        nameText.text = data.glassName;
        iconImage.sprite = data.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }

    public void SetupSpirit(BaseSpiritData data, System.Action<IngredientButton> onClickCallback)
    {
        ingredientData = data;
        nameText.text = data.spiritName;
        iconImage.sprite = data.icon;
        iconImage.color = data.liquidColor;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }

    public void SetupMixer(MixerData data, System.Action<IngredientButton> onClickCallback)
    {
        ingredientData = data;
        nameText.text = data.mixerName;
        iconImage.sprite = data.icon;
        iconImage.color = data.liquidColor;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }

    public void SetupGarnish(GarnishData data, System.Action<IngredientButton> onClickCallback)
    {
        ingredientData = data;
        nameText.text = data.garnishName;
        iconImage.sprite = data.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);

        buttonImage.color = selected ? selectedColor : normalColor;
    }

    public T GetData<T>()
    {
        return (T)ingredientData;
    }
}