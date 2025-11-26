using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CocktailSystem : MonoBehaviour
{
    private DataManager _dataManager;

    private CocktailData cocktailData;
    private CocktailRecipeScript recipeData;
    private Dictionary<int, Ingridiant> Ingridiants = new Dictionary<int, Ingridiant>(); // 제작 시 user가 선택한 재료

    private int selectedTechnique = -1; // -1 = 미선택
    private int selectedGlassId = -1;   // -1 = 미선택
    private CocktailRecipeScript recipeData;
    private Dictionary<int, Ingridiant> Ingridiants = new Dictionary<int, Ingridiant>(); // 제작 시 user가 선택한 재료

    private int selectedTechnique = -1; // -1 = 미선택
    private int selectedGlassId = -1;   // -1 = 미선택

    public void Awake()
    {


    }


    private void Start()
    {
        // DataManager 인스턴스를 가져옵니다.
        _dataManager = DataManager.Instance;
    }

    #region Public Methods
    /// <summary>
    /// UI에서 사용자가 선택한 기법을 설정합니다.
    /// </summary>
    public void SetSelectedTechnique(int techniqueId)
    {
        selectedTechnique = techniqueId;
    }

    /// <summary>
    /// UI에서 사용자가 선택한 잔을 설정합니다.
    /// </summary>
    public void SetSelectedGlass(int glassId)
    {
        selectedGlassId = glassId;
    }

    /// <summary>
    /// UI에서 사용자가 추가한 재료를 저장합니다.
    /// </summary>
    public void AddIngredient(int ingredientId, Ingridiant ingredient)
    {
        Ingridiants[ingredientId] = ingredient;
    }

    /// <summary>
    /// 저장된 재료 목록을 초기화합니다.
    /// </summary>
    public void ClearIngredients()
    {
        Ingridiants.Clear();
        selectedTechnique = -1;
        selectedGlassId = -1;
    }
    #endregion


    #region Public Methods
    /// <summary>
    /// UI에서 사용자가 선택한 기법을 설정합니다.
    /// </summary>
    public void SetSelectedTechnique(int techniqueId)
    {
        selectedTechnique = techniqueId;
    }

    /// <summary>
    /// UI에서 사용자가 선택한 잔을 설정합니다.
    /// </summary>
    public void SetSelectedGlass(int glassId)
    {
        selectedGlassId = glassId;
    }

    /// <summary>
    /// UI에서 사용자가 추가한 재료를 저장합니다.
    /// </summary>
    public void AddIngredient(int ingredientId, Ingridiant ingredient)
    {
        Ingridiants[ingredientId] = ingredient;
    }

    /// <summary>
    /// 저장된 재료 목록을 초기화합니다.
    /// </summary>
    public void ClearIngredients()
    {
        Ingridiants.Clear();
        selectedTechnique = -1;
        selectedGlassId = -1;
    }
    #endregion

    #region CheckSum
    /// <summary>
    /// 사용자가 제작한 칵테일과 주문된 칵테일을 비교하여 유사도 점수를 반환합니다.
    /// (OrderedCocktail 객체를 직접 받는 오버로드 - 권장)
    /// </summary>
    /// <param name="orderedCocktail">주문된 칵테일 객체</param>
    /// <returns>유사도 점수 (0~100)</returns>
    public float CheckCocktailToRecipe(OrderedCocktail orderedCocktail)
    {
        if (orderedCocktail == null)
        {
            Debug.LogError("OrderedCocktail이 null입니다.");
            return 0.0f;
        }

        float totalScore = 0.0f;

        totalScore += CalculateIngredientScore(orderedCocktail.Recipe);
        totalScore += CalculateGlassScore(orderedCocktail.GlassId);
        totalScore += CalculateTechniqueScore(orderedCocktail.Technique);

        Debug.Log($"칵테일 제작 완료 - 총점: {totalScore}점 / 성공 기준: {orderedCocktail.SimilarityThreshold}점");

        return totalScore;
    }

    /// <summary>
    /// 사용자가 제작한 칵테일과 레시피를 비교하여 유사도 점수를 반환
    /// </summary>
    /// <param name="orderId">손님이 주문한 칵테일 ID</param>
    /// <returns>유사도 점수 (0~100)</returns>
    public float CheckCocktailToRecipe(int orderId)
    {
        float totalScore = 0.0f;

        // 1. 주문한 칵테일의 데이터와 레시피 로드
        cocktailData = CocktailRepository.Instance.GetCocktailDataById(orderId);
        recipeData = CocktailRepository.Instance.GetCocktailRecipeByCocktailId(orderId);

        if (cocktailData == null || recipeData == null)
        {
            Debug.LogError($"칵테일 ID '{orderId}'에 대한 데이터를 찾을 수 없습니다.");
            return 0.0f;
        }

        // 2. 재료 점수 계산 (기주 40점 + 부재료 30점 + 가니쉬 5점)
        totalScore += CalculateIngredientScore(recipeData.Recipedict);

        // 3. 잔 점수 계산 (2점)
        totalScore += CalculateGlassScore(cocktailData.glass_id);

        // 4. 기법 점수 계산 (23점)
        totalScore += CalculateTechniqueScore(recipeData.Technique);

        Debug.Log($"칵테일 제작 완료 - 총점: {totalScore}점 / 성공 기준: {cocktailData.similarity_threadhold}점");

        return totalScore;
    }

    /// <summary>
    /// 재료 점수를 계산 (기주 40점 + 부재료 30점 + 가니쉬 5점)
    /// </summary>
    private float CalculateIngredientScore(Dictionary<int, Ingridiant> recipeDict)
    {
        float ingredientScore = 0.0f;

        // 레시피의 각 재료 타입별로 점수 계산
        foreach (var recipeItem in recipeDict)
        {
            int ingredientId = recipeItem.Key;
            Ingridiant recipeIngredient = recipeItem.Value;

            if (recipeIngredient == null) continue;

            // 사용자가 이 재료를 추가했는지 확인
            if (Ingridiants.ContainsKey(ingredientId))
            {
                Ingridiant userIngredient = Ingridiants[ingredientId];

                switch (recipeIngredient.Ingridiant_type)
                {
                    case "Alcohol": // 기주: 종류 30점 + 용량 10점
                        ingredientScore += 30.0f; // 종류가 일치하므로 30점
                        ingredientScore += CalculateVolumeScore(recipeIngredient, userIngredient); // 용량 점수 (0~10점)
                        break;

                    case "Drink": // 부재료(음료): 종류 20점 + 용량 10점
                        ingredientScore += 20.0f; // 종류가 일치하므로 20점
                        ingredientScore += CalculateVolumeScore(recipeIngredient, userIngredient); // 용량 점수 (0~10점)
                        break;

                    case "Garnish": // 가니쉬: 종류 5점 (용량 체크 없음)
                        ingredientScore += 5.0f;
                        break;

                    case "Ice": // 얼음은 점수 계산에 포함하지 않음 (엑셀에 없음)
                        break;
                }
            }
            // 재료를 추가하지 않은 경우 0점 (점수 추가하지 않음)
        }

        return ingredientScore;
    }

    /// <summary>
    /// 용량 차이에 따라 점수를 계산 (0~10점)
    /// </summary>
    private float CalculateVolumeScore(Ingridiant recipeIngredient, Ingridiant userIngredient)
    {
        // Volume이 null인 경우 0점 반환
        if (!recipeIngredient.Volume.HasValue || !userIngredient.Volume.HasValue)
        {
            return 0.0f;
        }

        int recipeVolume = recipeIngredient.Volume.Value;
        int userVolume = userIngredient.Volume.Value;
        int volumeDifference = Mathf.Abs(recipeVolume - userVolume);

        // 용량 차이에 따른 점수 부여
        if (volumeDifference <= 2)
        {
            return 10.0f; // ±0~2ml: 10점
        }
        else if (volumeDifference <= 5)
        {
            return 7.0f; // ±3~5ml: 7점
        }
        else if (volumeDifference <= 8)
        {
            return 4.0f; // ±6~8ml: 4점
        }
        else
        {
            return 0.0f; // ±9ml 이상: 0점
        }
    }

    /// <summary>
    /// 잔 점수를 계산 (2점)
    /// </summary>
    private float CalculateGlassScore(int requiredGlassId)
    {
        // 잔이 일치하면 2점, 아니면 0점
        if (selectedGlassId == requiredGlassId)
        {
            return 2.0f;
        }
        return 0.0f;
    }

    /// <summary>
    /// 기법 점수를 계산 (23점)
    /// </summary>
    private float CalculateTechniqueScore(int requiredTechnique)
    {
        // 기법이 일치하면 23점, 아니면 0점
        if (selectedTechnique == requiredTechnique)
        {
            return 23.0f;
        }
        return 0.0f;
    }
    #endregion
}