using UnityEngine;

namespace Merge
{
    /// <summary>
    /// 인테리어 배치 프리뷰 오브젝트에 InteriorData를 임시 저장하는 스크립트
    /// 배치 확정 시 프리뷰 오브젝트는 삭제되고, InteriorFactory가 생성하는 실제 인테리어를 생성됨
    /// </summary>
    public class TempInteriorData : MonoBehaviour
    {
        public InteriorData interiorData;
    }
}

