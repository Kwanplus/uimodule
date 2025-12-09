namespace UIModule
{
    /// <summary>
    /// UI 레이어 타입 정의
    /// 출력 순서: Background(가장 아래) → Screen → Popup → Overlay → System(최상단)
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 가장 아래 레이어 - 전역 배경 UI (고정 배경 이미지, 전역 패턴, 전환용 페이드 배경 등)
        /// </summary>
        Background = 0,
        
        /// <summary>
        /// 메인 화면 레이어 - 로비 화면, 전투 HUD, 설정 전체 화면 등 (한 번에 1개만 활성)
        /// </summary>
        Screen = 1,
        
        /// <summary>
        /// 팝업 레이어 - 다이얼로그, 확인/취소 창, 인벤토리 등 (여러 개 중첩 가능)
        /// </summary>
        Popup = 2,
        
        /// <summary>
        /// 오버레이 레이어 - 토스트 메시지, 툴팁, 튜토리얼 마커 등 (보조 정보)
        /// </summary>
        Overlay = 3,
        
        /// <summary>
        /// 최상단 시스템 레이어 - 점검 공지, 치명적 에러, 강제 업데이트 안내 등
        /// </summary>
        System = 4
    }
}

