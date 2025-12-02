using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

/// <summary>
/// 지능형 이름 생성기 
/// 음운론적 규칙과 패턴 학습을 통해 자연스러운 이름 생성
/// </summary>
public static class IntelligentNameGenerator
{
    // 종족별 이름 데이터베이스 (실제 판타지 이름에서 학습한 패턴 => AI가 뽑아냄)
    private static Dictionary<string, NamePattern> racePatterns = new Dictionary<string, NamePattern>
    {
        {
            "Human", new NamePattern
            {
                // 실제 판타지 이름 패턴
                prefixes = new[] { "아", "엘", "세", "레", "알", "카", "베", "이", "테", "루" },
                middles = new[] { "리", "나", "론", "드", "빈", "르", "린", "사", "벨", "니" },
                suffixes = new[] { "스", "아", "엘", "드", "온", "라", "안", "린", "리", "나" },
                
                // 자연스러운 음운 조합 규칙
                goodCombinations = new[]
                {
                    ("아", "리", "엘"), ("세", "바", "스"), ("레", "온", "나"),
                    ("엘", "리", "아"), ("카", "린", "다"), ("베", "론", "드"),
                    ("이", "사", "벨"), ("테", "레", "사"), ("알", "빈", "스")
                },
                
                // 피해야 할 조합
                avoidPatterns = new[] { "ㅡㅡ", "ㅣㅣ", "ㄱㄱ", "ㅂㅂ" }
            }
        },
        {
            "Orc", new NamePattern
            {
                prefixes = new[] { "그", "크", "가", "고", "투", "부", "자", "모", "우", "쿠" },
                middles = new[] { "로", "라", "루", "르", "아", "오", "쉬", "크", "즈", "그" },
                suffixes = new[] { "쉬", "크", "쿠", "그", "즈", "락", "쉬", "둠", "간", "타" },
                
                goodCombinations = new[]
                {
                    ("그", "로", "쉬"), ("크", "루", "크"), ("가", "로", "쉬"),
                    ("투", "르", "크"), ("부", "락", "간"), ("자", "고", "쉬"),
                    ("쿠", "로", "그"), ("우", "르", "둠")
                },
                
                avoidPatterns = new[] { "ㅏㅏ", "ㅓㅓ", "ㅣㅐ" }
            }
        },
        {
            "Vampire", new NamePattern
            {
                prefixes = new[] { "블", "드", "세", "카", "알", "루", "비", "라", "밀", "베" },
                middles = new[] { "라", "루", "카", "미", "리", "르", "레", "시", "나", "드" },
                suffixes = new[] { "드", "스", "아", "에", "르", "라", "안", "린", "카", "니" },
                
                goodCombinations = new[]
                {
                    ("블", "라", "드"), ("드", "라", "큐"), ("세", "레", "나"),
                    ("카", "르", "밀"), ("알", "루", "카"), ("루", "시", "안"),
                    ("비", "올", "레"), ("라", "미", "아")
                },
                
                avoidPatterns = new[] { "ㅡㅡ", "ㄹㄹ" }
            }
        }
    };
    
    // 최근 생성된 이름 캐시 (중복 방지)
    private static Dictionary<string, HashSet<string>> recentNames = new Dictionary<string, HashSet<string>>();
    private const int MAX_CACHE_SIZE = 50;
    
    /// <summary>
    /// AI 스타일로 종족에 맞는 지능형 이름 생성
    /// </summary>
    public static string Generate(string race, int minLength = 2, int maxLength = 4)
    {
        if (!racePatterns.ContainsKey(race))
        {
            return "알 수 없음";
        }
        
        var pattern = racePatterns[race];
        string generatedName;
        int attempts = 0;
        const int maxAttempts = 20;
        
        do
        {
            generatedName = GenerateWithPattern(pattern, minLength, maxLength);
            attempts++;
        }
        while (IsNameInCache(race, generatedName) && attempts < maxAttempts);
        
        // 캐시에 추가
        AddToCache(race, generatedName);
        
        return generatedName;
    }
    
    /// <summary>
    /// 패턴 기반 지능형 이름 생성
    /// </summary>
    private static string GenerateWithPattern(NamePattern pattern, int minLength, int maxLength)
    {
        // 30% 확률로 학습된 좋은 조합 사용
        if (Random.value < 0.3f && pattern.goodCombinations.Length > 0)
        {
            var combo = pattern.goodCombinations[Random.Range(0, pattern.goodCombinations.Length)];
            return combo.Item1 + combo.Item2 + combo.Item3;
        }
        
        StringBuilder sb = new StringBuilder();
        int targetLength = Random.Range(minLength, maxLength + 1);
        
        // 1. 시작 음절 (접두사)
        string prefix = pattern.prefixes[Random.Range(0, pattern.prefixes.Length)];
        sb.Append(prefix);
        
        // 2. 중간 음절들
        for (int i = 1; i < targetLength - 1; i++)
        {
            string middle = pattern.middles[Random.Range(0, pattern.middles.Length)];
            
            // 피해야 할 패턴 체크
            if (!HasBadPattern(sb.ToString() + middle, pattern))
            {
                sb.Append(middle);
            }
            else
            {
                // 다른 음절 시도
                middle = pattern.middles[Random.Range(0, pattern.middles.Length)];
                sb.Append(middle);
            }
        }
        
        // 3. 끝 음절 (접미사)
        if (targetLength >= 2)
        {
            string suffix = pattern.suffixes[Random.Range(0, pattern.suffixes.Length)];
            sb.Append(suffix);
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 부자연스러운 패턴 체크
    /// </summary>
    private static bool HasBadPattern(string name, NamePattern pattern)
    {
        foreach (var badPattern in pattern.avoidPatterns)
        {
            if (name.Contains(badPattern))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 캐시에 이름 추가 (중복 방지용)
    /// </summary>
    private static void AddToCache(string race, string name)
    {
        if (!recentNames.ContainsKey(race))
        {
            recentNames[race] = new HashSet<string>();
        }
        
        recentNames[race].Add(name);
        
        // 캐시 크기 제한
        if (recentNames[race].Count > MAX_CACHE_SIZE)
        {
            var oldest = recentNames[race].First();
            recentNames[race].Remove(oldest);
        }
    }
    
    /// <summary>
    /// 캐시에 이름이 있는지 확인
    /// </summary>
    private static bool IsNameInCache(string race, string name)
    {
        return recentNames.ContainsKey(race) && recentNames[race].Contains(name);
    }
    
    /// <summary>
    /// 특정 스타일로 이름 생성 (추가 옵션)
    /// </summary>
    public static string GenerateWithStyle(string race, NameStyle style)
    {
        string baseName = Generate(race);
        
        switch (style)
        {
            case NameStyle.Noble: // 귀족풍
                return baseName + (Random.value > 0.5f ? " 경" : " 공");
                
            case NameStyle.Common: // 평민풍
                return baseName;
                
            case NameStyle.Ancient: // 고대풍
                return "고대의 " + baseName;
                
            case NameStyle.Warrior: // 전사풍
                if (race == "Orc")
                    return baseName + " 파괴자";
                return baseName + " 용맹";
                
            default:
                return baseName;
        }
    }
    
    /// <summary>
    /// 여러 개의 이름을 한번에 생성 (선택지 제공용)
    /// </summary>
    public static List<string> GenerateMultiple(string race, int count)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < count; i++)
        {
            names.Add(Generate(race));
        }
        return names;
    }
}

// ============================================
// 데이터 구조
// ============================================

public class NamePattern
{
    public string[] prefixes;      // 시작 음절
    public string[] middles;       // 중간 음절
    public string[] suffixes;      // 끝 음절
    public (string, string, string)[] goodCombinations;  // 학습된 좋은 조합
    public string[] avoidPatterns; // 피해야 할 패턴
}

public enum NameStyle
{
    Common,   // 평민
    Noble,    // 귀족
    Ancient,  // 고대
    Warrior   // 전사
}

// ============================================
// 사용 예시
// ============================================

public class NameGeneratorDemo : MonoBehaviour
{
    void Start()
    {
        // 기본 사용법
        Debug.Log("=== 기본 이름 생성 ===");
        Debug.Log("Human: " + IntelligentNameGenerator.Generate("Human"));
        Debug.Log("Orc: " + IntelligentNameGenerator.Generate("Orc"));
        Debug.Log("Vampire: " + IntelligentNameGenerator.Generate("Vampire"));
        
        // 스타일 적용
        Debug.Log("\n=== 스타일 적용 ===");
        Debug.Log("귀족 Human: " + IntelligentNameGenerator.GenerateWithStyle("Human", NameStyle.Noble));
        Debug.Log("전사 Orc: " + IntelligentNameGenerator.GenerateWithStyle("Orc", NameStyle.Warrior));
        
        // 여러 개 생성 (선택지 제공)
        Debug.Log("\n=== 다중 생성 (선택지) ===");
        var vampireNames = IntelligentNameGenerator.GenerateMultiple("Vampire", 5);
        for (int i = 0; i < vampireNames.Count; i++)
        {
            Debug.Log($"후보 {i + 1}: {vampireNames[i]}");
        }
    }
}