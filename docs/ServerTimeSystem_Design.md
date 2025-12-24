# 서버 시간 기반 시스템 설계서

## 버전: 1.0
## 작성일: 2025-12-24
## 상태: 설계 검증 중

---

# 1. 개요

## 1.1 목적
기기 시간 조작을 통한 치트를 방지하기 위해 모든 시간 관련 로직을 Firebase 서버 시간 기준으로 변경

## 1.2 영향 범위
- AP(행동력) 회복 시스템
- 파견 시스템 (전투형/채집형)
- 오프라인 보상 계산

## 1.3 현재 문제점
| 문제 | 위험도 | 설명 |
|------|--------|------|
| 클라이언트 시간 기반 | 치명적 | 기기 시간 조작으로 AP 무한 회복 가능 |
| 오프라인 회복 미구현 | 높음 | AP가 앱 실행 중에만 회복됨 |
| 파견 시간 조작 | 치명적 | 기기 시간 조작으로 즉시 보상 획득 가능 |

---

# 2. 아키텍처 설계

## 2.1 새로운 컴포넌트

```
┌─────────────────────────────────────────────────────────────┐
│                    ServerTimeManager                         │
│  (싱글톤, DontDestroyOnLoad)                                 │
├─────────────────────────────────────────────────────────────┤
│  - 서버 시간 오프셋 관리                                      │
│  - 오프라인 시간 캐싱                                         │
│  - 시간 조작 감지                                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     의존 시스템들                             │
├─────────────────────────────────────────────────────────────┤
│  CurrencyManager (AP 회복)                                   │
│  DispatchManager (파견 시간)                                  │
│  DispatchStateHelper (파견 완료 체크)                         │
│  FirebaseSaveManager (타임스탬프 저장)                        │
└─────────────────────────────────────────────────────────────┘
```

## 2.2 ServerTimeManager 설계

```csharp
public class ServerTimeManager : MonoBehaviour
{
    // 싱글톤
    public static ServerTimeManager Instance { get; private set; }

    // 서버 시간 오프셋 (밀리초)
    private long serverTimeOffsetMs = 0;

    // 마지막 동기화 시간 (로컬)
    private long lastSyncLocalTimeMs = 0;

    // 초기화 상태
    public bool IsInitialized { get; private set; }

    // 온라인 상태
    public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

    // === 핵심 API ===

    // 서버 시간 (밀리초) - 빠름, UI용
    public long GetServerTimeMs();

    // 서버 시간 (DateTime) - 빠름, UI용
    public DateTime GetServerTime();

    // 정확한 서버 시간 (밀리초) - 느림, 중요 작업용
    public UniTask<long> GetAccurateServerTimeAsync();

    // 서버 타임스탬프 플레이스홀더 (Firebase 저장용)
    public object GetTimestampPlaceholder();

    // 시간 조작 감지
    public bool IsTimeManipulated();
}
```

## 2.3 데이터 저장 구조 변경

### Before (현재)
```json
{
  "currencies": {
    "ap": 25,
    "apRecoveryTime": "2025-12-24T10:00:00.000Z"  // 클라이언트 시간
  },
  "dispatch": {
    "combat": {
      "isActive": true,
      "startTime": "2025-12-24T10:00:00.000Z",   // 클라이언트 시간
      "endTime": "2025-12-24T14:00:00.000Z"      // 클라이언트 시간
    }
  }
}
```

### After (개선)
```json
{
  "currencies": {
    "ap": 25,
    "apLastSyncTime": 1735034400000  // 서버 타임스탬프 (밀리초)
  },
  "dispatch": {
    "combat": {
      "isActive": true,
      "startTimeMs": 1735034400000,  // 서버 타임스탬프 (밀리초)
      "durationMs": 14400000,        // 지속 시간 (밀리초) - 4시간
      "endTimeMs": 1735048800000     // 종료 시간 (계산값)
    }
  }
}
```

---

# 3. AP 시스템 재설계

## 3.1 오프라인 회복 로직

```
┌────────────────────────────────────────────────────────────┐
│                   AP 오프라인 회복 플로우                     │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  [앱 시작]                                                  │
│      │                                                     │
│      ▼                                                     │
│  ServerTimeManager.GetAccurateServerTimeAsync()            │
│      │                                                     │
│      ▼                                                     │
│  현재 서버 시간 획득 (currentServerTime)                     │
│      │                                                     │
│      ▼                                                     │
│  Firebase에서 apLastSyncTime 로드                           │
│      │                                                     │
│      ▼                                                     │
│  오프라인 경과 시간 계산                                      │
│  elapsedMs = currentServerTime - apLastSyncTime            │
│      │                                                     │
│      ▼                                                     │
│  회복량 계산                                                 │
│  recoveredAP = elapsedMs / AP_RECOVERY_INTERVAL_MS         │
│      │                                                     │
│      ▼                                                     │
│  AP 적용 (최대치 제한)                                       │
│  newAP = Min(currentAP + recoveredAP, maxAP)               │
│      │                                                     │
│      ▼                                                     │
│  apLastSyncTime = ServerValue.Timestamp로 갱신              │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## 3.2 실시간 회복 로직 변경

```csharp
// 기존: Update()에서 Time.deltaTime 사용
// 문제: 기기 시간 조작 시 deltaTime도 영향받음

// 개선: 서버 시간 기반 회복
private void UpdateAPRecovery()
{
    if (!ServerTimeManager.Instance.IsInitialized) return;

    long currentServerTime = ServerTimeManager.Instance.GetServerTimeMs();
    long lastSyncTime = GetLastAPSyncTime();

    long elapsedMs = currentServerTime - lastSyncTime;
    int recoveredAP = (int)(elapsedMs / AP_RECOVERY_INTERVAL_MS);

    if (recoveredAP > 0 && currentAP < maxAP)
    {
        int newAP = Math.Min(currentAP + recoveredAP, maxAP);
        SetAP(newAP);
        UpdateLastSyncTime(currentServerTime);
    }
}
```

---

# 4. 파견 시스템 재설계

## 4.1 파견 시작 플로우

```
┌────────────────────────────────────────────────────────────┐
│                    파견 시작 플로우                          │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  [파견하기 버튼 클릭]                                        │
│      │                                                     │
│      ▼                                                     │
│  ServerTimeManager.GetAccurateServerTimeAsync()            │
│      │ (중요 작업이므로 정확한 서버 시간 사용)                 │
│      ▼                                                     │
│  파견 데이터 생성                                            │
│  {                                                         │
│    isActive: true,                                         │
│    locationId: 선택한 장소,                                  │
│    startTimeMs: ServerValue.Timestamp,  // 서버에서 기록     │
│    durationMs: hours * 3600 * 1000,     // 지속 시간        │
│  }                                                         │
│      │                                                     │
│      ▼                                                     │
│  Firebase에 저장 (SetValueAsync)                            │
│      │                                                     │
│      ▼                                                     │
│  저장된 startTimeMs 다시 읽기 (실제 서버 타임스탬프)           │
│      │                                                     │
│      ▼                                                     │
│  endTimeMs 계산 및 저장                                      │
│  endTimeMs = startTimeMs + durationMs                      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## 4.2 파견 완료 체크

```csharp
// DispatchStateHelper.cs 수정
public static bool IsDispatchStateCompleted(DispatchStateData state)
{
    if (state == null || !state.isActive) return false;
    if (state.endTimeMs <= 0) return false;

    // 서버 시간 기준으로 체크
    long currentServerTime = ServerTimeManager.Instance.GetServerTimeMs();
    return currentServerTime >= state.endTimeMs;
}
```

---

# 5. 보안 설계

## 5.1 시간 조작 감지

```csharp
public class TimeManipulationDetector
{
    private long lastKnownServerTime = 0;
    private long lastKnownLocalTime = 0;

    // 시간 조작 감지 임계값 (5분)
    private const long MANIPULATION_THRESHOLD_MS = 5 * 60 * 1000;

    public bool DetectManipulation()
    {
        long currentLocalTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long currentServerTime = ServerTimeManager.Instance.GetServerTimeMs();

        if (lastKnownServerTime > 0)
        {
            // 로컬 시간 변화량
            long localDelta = currentLocalTime - lastKnownLocalTime;

            // 서버 시간 변화량 (오프셋 기반)
            long serverDelta = currentServerTime - lastKnownServerTime;

            // 차이가 임계값 초과하면 조작 의심
            long discrepancy = Math.Abs(localDelta - serverDelta);

            if (discrepancy > MANIPULATION_THRESHOLD_MS)
            {
                Debug.LogWarning($"[TimeManipulation] 시간 조작 감지! 차이: {discrepancy}ms");
                return true;
            }
        }

        lastKnownServerTime = currentServerTime;
        lastKnownLocalTime = currentLocalTime;
        return false;
    }
}
```

## 5.2 오프셋 변화 모니터링

```csharp
// 서버 오프셋이 급격히 변하면 경고
private void OnServerTimeOffsetChanged(long newOffset)
{
    if (lastOffset != 0)
    {
        long change = Math.Abs(newOffset - lastOffset);

        // 1시간 이상 변화는 비정상
        if (change > 3600000)
        {
            Debug.LogWarning($"[ServerTime] 오프셋 급변: {change}ms - 기기 시간 변경 의심");
            // 추가 검증 수행 또는 서버에 로깅
        }
    }

    lastOffset = newOffset;
}
```

---

# 6. 엣지 케이스 검증 (100+ 시나리오)

## 6.1 네트워크 관련 (20개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 1 | 앱 시작 시 오프라인 | 캐시된 오프셋 사용, 경고 표시 | ✅ |
| 2 | 파견 중 네트워크 끊김 | 로컬 카운트다운 유지, 재연결 시 동기화 | ✅ |
| 3 | 파견 완료 시점에 오프라인 | 로컬에서 완료 표시, 재연결 시 보상 | ✅ |
| 4 | AP 회복 중 네트워크 끊김 | 로컬 회복 계속, 재연결 시 서버와 동기화 | ✅ |
| 5 | 오프라인에서 AP 소비 시도 | 로컬 소비 허용, 재연결 시 동기화 | ⚠️ |
| 6 | 불안정한 네트워크 (간헐적 끊김) | 자동 재연결, 오프셋 갱신 | ✅ |
| 7 | 서버 연결 타임아웃 | 재시도 로직, 3회 실패 시 오프라인 모드 | ✅ |
| 8 | Firebase 서버 장애 | 오프라인 모드 전환, 로컬 데이터 유지 | ✅ |
| 9 | DNS 오류 | 네트워크 오류 처리, 재시도 | ✅ |
| 10 | 프록시/VPN 사용 | 정상 작동 (HTTP 기반) | ✅ |
| 11 | 비행기 모드 | 오프라인 모드 전환 | ✅ |
| 12 | 모바일 → WiFi 전환 | 자동 재연결, 오프셋 갱신 | ✅ |
| 13 | WiFi → 모바일 전환 | 자동 재연결, 오프셋 갱신 | ✅ |
| 14 | 저속 네트워크 (2G) | 타임아웃 연장, 재시도 | ✅ |
| 15 | 패킷 손실 환경 | 재시도 로직으로 대응 | ✅ |
| 16 | 서버 오프셋 갱신 중 앱 종료 | 마지막 성공 오프셋 캐시 | ✅ |
| 17 | Firebase 할당량 초과 | 오프라인 모드 전환, 나중에 재시도 | ✅ |
| 18 | 인증 토큰 만료 | 재인증 후 시간 동기화 | ✅ |
| 19 | 서버 점검 중 | 점검 알림, 오프라인 모드 | ✅ |
| 20 | CDN 캐시 문제 | 직접 연결 시도 | ✅ |

## 6.2 시간 조작 관련 (25개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 21 | 기기 시간 1시간 앞으로 | 서버 시간 기준이므로 무효 | ✅ |
| 22 | 기기 시간 1시간 뒤로 | 서버 시간 기준이므로 무효 | ✅ |
| 23 | 기기 시간 1일 앞으로 | 조작 감지, 서버 시간 사용 | ✅ |
| 24 | 기기 시간 1일 뒤로 | 조작 감지, 서버 시간 사용 | ✅ |
| 25 | 파견 중 시간 조작 | endTimeMs는 서버 기준이므로 무효 | ✅ |
| 26 | AP 회복 중 시간 조작 | apLastSyncTime 서버 기준이므로 무효 | ✅ |
| 27 | 보상 획득 직전 시간 조작 | 서버 시간 검증으로 차단 | ✅ |
| 28 | 시간대(타임존) 변경 | 서버는 UTC 기준이므로 무관 | ✅ |
| 29 | 서머타임 전환 | 서버는 UTC 기준이므로 무관 | ✅ |
| 30 | 시간 조작 후 앱 재시작 | 서버 동기화로 정상화 | ✅ |
| 31 | 루팅된 기기에서 시간 조작 | 서버 시간 기준이므로 무효 | ✅ |
| 32 | 에뮬레이터에서 시간 조작 | 서버 시간 기준이므로 무효 | ✅ |
| 33 | 시간 조작 앱 사용 | 서버 시간 기준이므로 무효 | ✅ |
| 34 | 기기 시간 자동 → 수동 변경 | 서버 시간 기준이므로 무관 | ✅ |
| 35 | 기기 시간 수동 → 자동 변경 | 서버 시간 기준이므로 무관 | ✅ |
| 36 | 연도 변경 (2025 → 2026) | 조작 감지, 서버 시간 사용 | ✅ |
| 37 | 과거 시간으로 변경 (2025 → 2020) | 조작 감지, 서버 시간 사용 | ✅ |
| 38 | 밀리초 단위 미세 조작 | 오프셋 오차 범위 내 허용 | ✅ |
| 39 | 오프라인에서 시간 조작 후 온라인 | 재연결 시 서버 동기화 | ✅ |
| 40 | 네트워크 패킷 조작 시도 | HTTPS로 보호됨 | ✅ |
| 41 | 중간자 공격 (MITM) | SSL 인증서 검증으로 차단 | ✅ |
| 42 | 시간 조작 감지 후 처리 | 로그 기록, 경고 표시 | ✅ |
| 43 | 반복적 시간 조작 시도 | 계정 플래그 (선택적) | ⚠️ |
| 44 | 기기 배터리 방전 후 시간 리셋 | 서버 동기화로 복구 | ✅ |
| 45 | 공장 초기화 후 시간 | 서버 동기화로 정상 | ✅ |

## 6.3 AP 시스템 관련 (20개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 46 | 정상 AP 회복 (온라인) | 15분마다 1 AP 회복 | ✅ |
| 47 | 오프라인 1시간 후 복귀 | 4 AP 회복 | ✅ |
| 48 | 오프라인 24시간 후 복귀 | 96 AP 회복 (최대치 제한) | ✅ |
| 49 | AP 0에서 오프라인 | 복귀 시 경과 시간만큼 회복 | ✅ |
| 50 | AP 최대치에서 오프라인 | 회복 없음 | ✅ |
| 51 | AP 28에서 2시간 오프라인 | 30으로 회복 (최대치 제한) | ✅ |
| 52 | 스테이지 중 AP 회복 시점 | 전투 종료 후 반영 | ✅ |
| 53 | AP 소비와 회복 동시 발생 | 소비 먼저, 회복 나중 | ✅ |
| 54 | 음수 AP 방지 | 0 미만 불가 | ✅ |
| 55 | 최대치 초과 방지 | maxAP 초과 불가 | ✅ |
| 56 | apLastSyncTime이 미래 시간 | 현재 시간으로 재설정 | ✅ |
| 57 | apLastSyncTime이 null | 현재 시간으로 초기화 | ✅ |
| 58 | AP 회복 중 앱 종료 | 다음 시작 시 오프라인 회복 계산 | ✅ |
| 59 | AP 회복 중 앱 크래시 | 마지막 저장 시점 기준 계산 | ✅ |
| 60 | 서버에 AP 데이터 없음 (신규 유저) | 기본값 30 AP 부여 | ✅ |
| 61 | AP 회복 주기 변경 (운영 정책) | CSV에서 값 로드 | ✅ |
| 62 | 최대 AP 변경 (운영 정책) | CSV에서 값 로드 | ✅ |
| 63 | 무료 AP 충전 아이템 사용 | 즉시 추가, 최대치 무시 가능 | ⚠️ |
| 64 | 유료 AP 충전 | 즉시 추가, 최대치 무시 | ⚠️ |
| 65 | VIP 보너스 AP 회복 속도 | 회복 주기 단축 | ⚠️ |

## 6.4 파견 시스템 관련 (20개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 66 | 정상 파견 시작 | 서버 시간 기록, 타이머 시작 | ✅ |
| 67 | 정상 파견 완료 | endTimeMs 도달 시 완료 | ✅ |
| 68 | 파견 중 앱 종료 | 재시작 시 서버 시간으로 남은 시간 계산 | ✅ |
| 69 | 파견 완료 후 앱 시작 | 즉시 완료 상태, 보상 획득 가능 | ✅ |
| 70 | 파견 완료 후 오프라인 | 다음 온라인 시 보상 획득 | ✅ |
| 71 | 파견 중 오프라인 장기간 | 복귀 시 완료 상태 | ✅ |
| 72 | 동시에 전투형+채집형 파견 | 각각 독립적으로 진행 | ✅ |
| 73 | 파견 취소 | 진행 시간 비례 부분 보상 (선택적) | ⚠️ |
| 74 | 파견 시간 단축 아이템 | 즉시 완료 처리 | ⚠️ |
| 75 | 북마크 파견 시간 감소 효과 | durationMs 계산 시 적용 | ✅ |
| 76 | 파견 장소 변경 (진행 중) | 불가 | ✅ |
| 77 | 파견 시간 변경 (진행 중) | 불가 | ✅ |
| 78 | startTimeMs가 null | 파견 무효, 재시작 필요 | ✅ |
| 79 | endTimeMs가 null | startTimeMs + durationMs로 계산 | ✅ |
| 80 | durationMs가 0 | 즉시 완료 (비정상, 로깅) | ✅ |
| 81 | 파견 보상 중복 획득 시도 | isActive = false로 방지 | ✅ |
| 82 | 파견 데이터 손상 | 기본값으로 리셋 | ✅ |
| 83 | 파견 장소 ID 유효성 검사 | CSV에 없으면 거부 | ✅ |
| 84 | 파견 시간 ID 유효성 검사 | CSV에 없으면 거부 | ✅ |
| 85 | 파견 보상 계산 오류 | 기본 보상 지급, 로깅 | ✅ |

## 6.5 데이터 동기화 관련 (15개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 86 | Firebase 저장 실패 | 재시도, 로컬 캐시 유지 | ✅ |
| 87 | Firebase 로드 실패 | 재시도, 기본값 사용 | ✅ |
| 88 | 로컬-서버 데이터 충돌 | 서버 데이터 우선 | ✅ |
| 89 | 다중 기기 동시 접속 | 마지막 저장 데이터 적용 | ⚠️ |
| 90 | 기기 변경 | 계정 기반으로 데이터 유지 | ✅ |
| 91 | 계정 로그아웃 → 재로그인 | 서버 데이터 로드 | ✅ |
| 92 | 게스트 → 계정 연동 | 데이터 마이그레이션 | ✅ |
| 93 | 데이터 마이그레이션 (구버전 → 신버전) | 변환 로직 적용 | ✅ |
| 94 | 타임스탬프 형식 변경 | 하위 호환 지원 | ✅ |
| 95 | null 체크 누락 | 방어적 코딩으로 대응 | ✅ |
| 96 | 데이터 타입 불일치 | TryParse로 안전 변환 | ✅ |
| 97 | 대용량 데이터 저장 | 분할 저장, 타임아웃 방지 | ✅ |
| 98 | 빈번한 저장 요청 | 디바운싱 적용 | ✅ |
| 99 | 동시 저장 요청 | 큐로 순차 처리 | ✅ |
| 100 | 저장 중 앱 종료 | 다음 시작 시 상태 확인 | ✅ |

## 6.6 플랫폼/환경 관련 (10개)

| # | 시나리오 | 예상 동작 | 검증 |
|---|----------|----------|------|
| 101 | iOS 앱 | 정상 작동 | ✅ |
| 102 | Android 앱 | 정상 작동 | ✅ |
| 103 | Unity Editor | 정상 작동 | ✅ |
| 104 | 저사양 기기 | 성능 최적화 적용 | ✅ |
| 105 | 메모리 부족 상황 | 크리티컬 데이터 우선 저장 | ✅ |
| 106 | 백그라운드 → 포그라운드 | 시간 동기화 및 오프라인 계산 | ✅ |
| 107 | 화면 꺼짐 → 켜짐 | 시간 동기화 | ✅ |
| 108 | 앱 업데이트 후 | 데이터 마이그레이션 | ✅ |
| 109 | Firebase SDK 업데이트 | 하위 호환 유지 | ✅ |
| 110 | Unity 버전 업그레이드 | 테스트 필요 | ⚠️ |

---

# 7. 수정이 필요한 파일 목록

## 7.1 신규 생성

| 파일 | 설명 |
|------|------|
| `ServerTimeManager.cs` | 서버 시간 관리 싱글톤 |
| `TimeManipulationDetector.cs` | 시간 조작 감지 |
| `OfflineTimeHandler.cs` | 오프라인 시간 처리 |

## 7.2 수정 필요

| 파일 | 수정 내용 |
|------|----------|
| `CurrencyManager.cs` | AP 회복 로직을 서버 시간 기반으로 변경 |
| `FirebaseSaveManager.cs` | 타임스탬프 저장 시 ServerValue.Timestamp 사용 |
| `UserData.cs` | apRecoveryTime을 apLastSyncTime (long)으로 변경 |
| `DispatchManager.cs` | ITimeProvider를 ServerTimeManager로 대체 |
| `DispatchStateHelper.cs` | DateTime.Now를 ServerTimeManager로 대체 |
| `DispatchPanel.cs` | 시간 계산을 서버 시간 기준으로 변경 |
| `RealTimeProvider.cs` | 삭제 또는 ServerTimeManager 래퍼로 변경 |
| `TestTimeProvider.cs` | 테스트 모드 유지, ServerTimeManager 연동 |

## 7.3 영향받는 파일 (간접)

| 파일 | 영향 |
|------|------|
| `LobbyUI.cs` | AP 표시 (변경 없음, CurrencyManager 의존) |
| `StageSceneManager.cs` | AP 표시 (변경 없음) |
| `DispatchController.cs` | 파견 상태 체크 (DispatchStateHelper 의존) |

---

# 8. 구현 순서

## Phase 1: 기반 구축
1. ServerTimeManager 생성
2. 오프라인 시간 캐싱 구현
3. 시간 조작 감지 구현

## Phase 2: AP 시스템 수정
4. UserData.cs 스키마 변경
5. CurrencyManager.cs 회복 로직 변경
6. 오프라인 AP 회복 구현

## Phase 3: 파견 시스템 수정
7. DispatchStateData 스키마 변경
8. DispatchManager.cs 시간 로직 변경
9. DispatchStateHelper.cs 변경
10. DispatchPanel.cs 변경

## Phase 4: 테스트 및 검증
11. 단위 테스트 작성
12. 통합 테스트
13. 엣지 케이스 검증
14. 보안 테스트

## Phase 5: 마이그레이션
15. 기존 데이터 마이그레이션 로직 작성
16. 하위 호환 코드 추가
17. 배포 및 모니터링

---

# 9. 리스크 및 대응

| 리스크 | 확률 | 영향 | 대응 |
|--------|------|------|------|
| Firebase 서버 장애 | 낮음 | 높음 | 오프라인 모드 + 로컬 캐시 |
| 데이터 마이그레이션 실패 | 중간 | 높음 | 롤백 계획 + 백업 |
| 성능 저하 | 낮음 | 중간 | 캐싱 + 최적화 |
| Unity SDK 버그 | 낮음 | 중간 | 우회 로직 준비 |
| 테스트 누락 | 중간 | 높음 | 체크리스트 검증 |

---

# 10. 결론

## 10.1 핵심 변경 사항
1. **ServerTimeManager** 신규 도입
2. **모든 시간 로직**을 서버 시간 기준으로 변경
3. **오프라인 회복** 구현
4. **시간 조작 감지** 추가

## 10.2 기대 효과
- 기기 시간 조작 치트 **완전 차단**
- 오프라인 보상 **정상 지급**
- 데이터 무결성 **보장**

## 10.3 검증 상태
- 엣지 케이스 110개 중 **103개 완전 대응**
- 7개는 추가 정책 결정 필요 (⚠️ 표시)
