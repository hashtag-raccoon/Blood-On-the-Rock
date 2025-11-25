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

        // 인벤토리에서 꺼낸 건물인지 여부
        // ConstructedBuilding에도 있지만, TempBuilding을 새 건물을 생성 중인지 인벤토리에서 꺼낸 건물인지 구분하기 위한 임시 Bool 값이 필요함
        public bool isFromInventory = false;

        // 인벤토리에서 꺼낸 경우, 기존 건물 인스턴스 ID
        // 이 ID를 통해 기존 건물과 같은 건물인지 구분할 수 있음
        public long constructedBuildingId = -1;
    }
}
