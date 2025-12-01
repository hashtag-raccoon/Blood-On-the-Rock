/// <summary>
/// 모든 Repository가 구현해야 하는 공통 인터페이스입니다.
/// DataManager가 초기화 과정을 일괄적으로 관리하는 데 사용됩니다.
/// </summary>
public interface IRepository
{
    bool IsInitialized { get; }
    void Initialize();
}