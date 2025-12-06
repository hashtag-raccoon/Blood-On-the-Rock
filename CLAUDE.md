# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022.3.62f2 game project combining island management (merge gameplay) with bar/tavern simulation and cocktail crafting mechanics.

**Unity Version**: 2022.3.62f2 (see [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt))
**Target Framework**: .NET Standard 2.1

## Development Environment

### Opening the Project
1. Install Unity 2022.3.62f2
2. Open the project folder in Unity Hub
3. Open the MainScene or relevant scene in Unity Editor
4. Press Play to test

### No Automated Build/Test Commands
This project uses Unity Editor for development. There are no automated CLI build or test scripts. All testing and building is done through the Unity Editor interface.

## Architecture Overview

### Code Organization by Team/Feature

Scripts are organized into team-based namespaces under `Assets/Scripts/`:

- **Merge/**: Island management, building system, resource production, merge mechanics
- **Raccoon/**: Bar scene, cocktail making, guest/customer management, dialogue, table management
- **Hyunjae/**: Animation management, settings, start scene, interior control
- **Yoon/**: Data serialization utilities, cocktail system

### Core Architectural Patterns

#### 1. Singleton Manager Pattern
Most managers use singleton pattern with `DontDestroyOnLoad`:

```csharp
public class SomeManager : MonoBehaviour
{
    private static SomeManager _instance;
    public static SomeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SomeManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

Key managers:
- [DataManager](Assets/Scripts/Merge/Manager/DataManager.cs) - Central data hub for game state
- [BuildingRepository](Assets/Scripts/Merge/Repository/BuildingRepository.cs) - Building data access
- [CocktailMakingManager](Assets/Scripts/Raccoon/Manager/CocktailMakingManager.cs) - Cocktail crafting
- [DialogueManager](Assets/Scripts/Raccoon/Manager/DialogueManager.cs) - NPC dialogue
- [TableManager](Assets/Scripts/Raccoon/Manager/TableManager.cs) - Bar table management

#### 2. Repository Pattern
Repositories implement `IRepository` interface and are initialized by DataManager:

```csharp
public interface IRepository
{
    bool IsInitialized { get; }
    void Initialize();
}
```

Repositories under [Assets/Scripts/Merge/Repository/](Assets/Scripts/Merge/Repository/):
- BuildingRepository
- CocktailRepository
- ArbeitRepository
- GlassRepository
- ResourceRepository

#### 3. ScriptableObject Data Containers

> **⚠️ IMPORTANT: Serialization Issue**
> 
> ScriptableObject와 Serializable 클래스의 필드는 반드시 `public` 필드 또는 `[SerializeField]`로 선언해야 합니다.
> `{ get; private set; }` 프로퍼티는 Unity Inspector에서 값을 설정할 수 없어 null이 됩니다.
> 
> **❌ 잘못된 예:**
> ```csharp
> public Sprite Icon { get; private set; } // Inspector에서 설정 불가!
> ```
> 
> **✅ 올바른 예:**
> ```csharp
> [SerializeField] public Sprite Icon; // Inspector에서 설정 가능
> ```

Static game data is defined using ScriptableObjects with `[CreateAssetMenu]`:

```csharp
[CreateAssetMenu(fileName = "DataName", menuName = "Game Data/Data Type")]
public class SomeDataSO : ScriptableObject
{
    public List<SomeData> items = new List<SomeData>();
}
```

Examples in [Assets/Scripts/Merge/Datable/ScriptableObject/](Assets/Scripts/Merge/Datable/ScriptableObject/):
- BuildingDataSO - Building definitions
- BuildingProductionInfoSO - Production recipes
- PersonalityDataSO - NPC personality data
- CocktailDataSO - Cocktail recipes

#### 4. UI Event Pattern (Selection Events)
UI 선택 이벤트는 `System.Action<T>` 이벤트를 사용하여 구현합니다:

```csharp
// GlassSelectionUI.cs 패턴 예시
public class GlassSelectionUI : MonoBehaviour
{
    public event System.Action<Glass> OnGlassSelectedEvent;

    private void OnGlassSelected(int glassId)
    {
        Glass glass = GlassRepository.Instance.GetGlassById(glassId);
        OnGlassSelectedEvent?.Invoke(glass);
    }
}

// CocktailMakingUI.cs에서 구독
glassSelectionUI.OnGlassSelectedEvent += OnGlassSelectedInUI;
```

#### 5. Inspector Serialization
For complex types like dictionaries in the Unity Inspector, use [SerializableDictionary](Assets/Scripts/Yoon/SerializableDictionary.cs):

```csharp
using Sherbert.Framework.Generic;

[Serializable]
public class StringIntDictionary : SerializableDictionary<string, int> { }
```

### Data Flow

1. **Static Data**: ScriptableObjects → Repositories (loaded on Initialize)
2. **Runtime Data**: DataManager holds live game state (resources, buildings, NPCs, cocktails)
3. **Save/Load**: JSON files in `Assets/Scripts/Merge/Datable/Json/` using Newtonsoft.Json
4. **UI Updates**: Managers notify UI components directly (event system)

## Key Systems

### Merge System (Island Management)
- Building construction and placement via IslandManager
- Resource production and upgrades
- Drag-and-drop building placement ([DragDropController](Assets/Scripts/Merge/Controller/DragDropController.cs))
- Isometric grid-based layout

### Raccoon System (Bar Scene)
- Customer ordering via OrderingManager
- Cocktail crafting UI (CocktailMakingManager, CocktailMakingUI)
- Guest pathfinding using IsometricPathfinder
- Dialogue system with DialogueManager and DialogueUI
- Table assignment via TableManager

### Cocktail Making System
칵테일 제작 시스템의 주요 컴포넌트:

- [CocktailMakingUI](Assets/Scripts/Raccoon/Manager/CocktailMakingUI.cs) - 메인 UI 관리
- [IngredientSelectionUI](Assets/Scripts/Merge/UI/IngredientSelectionUI.cs) - 재료 선택
- [GlassSelectionUI](Assets/Scripts/Merge/UI/GlassSelectionUI.cs) - 잔 선택
- [TechniqueSelectionUI](Assets/Scripts/Merge/UI/TechniqueSelectionUI.cs) - 기법 선택
- [ToolSelectionUI](Assets/Scripts/Merge/UI/ToolSelectionUI.cs) - 도구 선택
- [CocktailSystem](Assets/Scripts/Yoon/CocktailSystem.cs) - 점수 계산 및 검증
- [DraggableIngredient](Assets/Scripts/Yoon/DraggableIngredient.cs) - 드래그 가능 재료 UI

### Character Animation
Managed by [AnimationManager](Assets/Scripts/Hyunjae/AnimationManager.cs), with rigging helpers in [Assets/Scripts/Merge/Character/](Assets/Scripts/Merge/Character/)

## Important Conventions

### When Adding New Data Types
1. Define data class in appropriate `Datable/` folder
2. **Use `public` fields or `[SerializeField]` - NOT `{ get; private set; }`**
3. Create ScriptableObject wrapper with `[CreateAssetMenu]`
4. Add repository if complex query logic is needed
5. Create sample asset in Unity Editor to verify Inspector functionality

### When Adding UI Selection Features
1. 기존 패턴 재활용 (GlassSelectionUI 참고)
2. `event System.Action<DataType>` 이벤트 선언
3. 선택 메서드에서 이벤트 발생: `OnSelectedEvent?.Invoke(data)`
4. CocktailMakingUI에서 구독 및 처리

### When Modifying Runtime Logic
- Check if manager is singleton with `DontDestroyOnLoad` - changes persist across scenes
- DataManager is the source of truth for runtime state
- Repositories are read-only data access layers

### JSON Data Management
JSON files in [Assets/Scripts/Merge/Datable/Json/](Assets/Scripts/Merge/Datable/Json/) are loaded via JsonDataHandler in DataManager. Use Newtonsoft.Json for serialization.

## Unity-Specific Considerations

### Assets Not to Edit Directly
- `Library/`, `Temp/`, `Logs/` - generated by Unity
- `ProjectSettings/` - edit only through Unity Editor
- `.meta` files - managed by Unity, changing GUIDs breaks references

### Scene Structure
Main scenes in [Assets/Scenes/](Assets/Scenes/):
- **MainScene** - Primary entry point
- **IslandScene_Raccoon** - Island/merge gameplay
- **BarScene_Raccoon** - Bar/cocktail gameplay
- **InventoryScene** - Item management
- **SettingScene** - Game settings
- **StartSceneHJ** - Start menu

### Unity Packages in Use
Key dependencies (see [Packages/manifest.json](Packages/manifest.json)):
- TextMesh Pro 3.0.7 - All UI text
- Newtonsoft.Json 3.2.2 - JSON serialization
- Cinemachine 2.10.5 - Camera control
- Unity 2D Animation 9.2.1 - Character sprites/animation
- Unity 2D Tilemap - Grid-based level layout

## Common Development Patterns

### Finding Data
```csharp
// Access via manager singleton
var manager = DataManager.Instance;
var buildings = manager.ConstructedBuildings;

// Access via repository
var buildingData = BuildingRepository.Instance.GetBuildingById(buildingId);
```

### Creating UI Elements
Use TextMeshProUGUI for all text. UI scripts typically in `UI/` subdirectories of each namespace.

### Draggable UI Elements
드래그 가능한 UI 요소 구현 시 [DraggableIngredient.cs](Assets/Scripts/Yoon/DraggableIngredient.cs) 참고:

```csharp
public class DraggableElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData) { /* ... */ }
    public void OnDrag(PointerEventData eventData) { /* ... */ }
    public void OnEndDrag(PointerEventData eventData) { /* ... */ }
}
```

### Temporary/Work-in-Progress Code
Some folders contain `(임시파일)` (temporary files) - avoid refactoring these without team discussion.

## Reference Files

- [copilot-instructions.md](.github/copilot-instructions.md) - Korean documentation with additional context
- [DataManager.cs](Assets/Scripts/Merge/Manager/DataManager.cs) - Central data management example
- [SerializableDictionary.cs](Assets/Scripts/Yoon/SerializableDictionary.cs) - Inspector serialization pattern
- [BuildingDataSO.cs](Assets/Scripts/Merge/Datable/ScriptableObject/BuildingDataSO.cs) - ScriptableObject pattern
- [IRepository.cs](Assets/Scripts/Merge/Repository/IRepository.cs) - Repository interface
- [GlassSelectionUI.cs](Assets/Scripts/Merge/UI/GlassSelectionUI.cs) - UI event pattern example
- [DraggableIngredient.cs](Assets/Scripts/Yoon/DraggableIngredient.cs) - Draggable UI pattern

