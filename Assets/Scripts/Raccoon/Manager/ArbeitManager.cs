using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Raccoon.Manager
{
    /// <summary>
    /// 아르바이트 지원자 관리 매니저 (비즈니스 로직 전담)
    /// UI와 분리된 후보자 생성, 필터링, 고용 로직 담당
    /// </summary>
    public class ArbeitManager : MonoBehaviour
    {
        public static ArbeitManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ArbeitManager] 싱글톤 설정됨");
            }
            else
            {
                Debug.LogWarning("[ArbeitManager] 중복된 인스턴스 죽어잇");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Job Center 초기 후보자 3명 생성
        /// </summary>
        public void InitializeJobCenter()
        {
            GenerateInitialCandidates();
        }

        /// <summary>
        /// 초기 후보자 3명 생성
        /// </summary>
        private void GenerateInitialCandidates()
        {
            if (ArbeitRepository.Instance.tempCandidateList.Count == 0)
            {
                List<TempNpcData> candidates = ArbeitRepository.Instance.CreateRandomTempCandidates(3);
                ArbeitRepository.Instance.tempCandidateList.AddRange(candidates);
            }
        }

        /// <summary>
        /// 고용되지 않은 후보자 목록 반환 (UI에서 호출)
        /// </summary>
        public List<TempNpcData> GetAvailableCandidates()
        {
            var available = ArbeitRepository.Instance.tempCandidateList
                .FindAll(candidate => !candidate.is_hired);
            return available;
        }

        /// <summary>
        /// 후보자를 고용 처리 (is_hired => 참으로 변경)
        /// </summary>
        public void HireCandidate(TempNpcData candidate)
        {
            if (candidate == null)
            {
                Debug.LogWarning("[ArbeitManager] HireCandidate() - candidate is null.");
                return;
            }

            candidate.is_hired = true;
        }

        /// <summary>
        /// 후보자 목록 갱신 (고용된 인원 제거 후 재생성)
        /// </summary>
        public void RefreshCandidates()
        {
            ArbeitRepository.Instance.tempCandidateList.Clear();
            GenerateInitialCandidates();
        }
    }
}
