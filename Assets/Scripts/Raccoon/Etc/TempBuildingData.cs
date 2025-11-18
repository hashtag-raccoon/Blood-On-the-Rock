using UnityEngine;

namespace Merge
{
    /// <summary>
    /// 건물 배치 프리뷰 오브젝트에 BuildingData를 임시 저장하는 스크립트
    ///  배치 확정 시 프리뷰 오브젝트는 삭제되고, BuildingFactory가 생성하는 실제 건물을 생성됨
    /// </summary>
    public class TempBuildingData : MonoBehaviour
    {
        public BuildingData buildingData;
    }
}
