# OpinetScheduler

Opinet 공공 API를 주기적으로 호출해 원본 응답과 정규화된 스냅샷을 PostgreSQL에 적재하는 .NET 8 콘솔 스케줄러입니다.

- 실행 형태: .NET 8 콘솔 애플리케이션
- 주요 역할: API 호출, 호출 이력 기록, 스냅샷 적재, 정기 동기화 실행
- DB: PostgreSQL
- 설정 파일: `appsettings.json`

## NuGet Packages

현재 `OpinetScheduler.csproj` 기준 주요 NuGet 패키지는 아래와 같습니다.

| Package | Version | Usage |
| --- | --- | --- |
| Dapper | 2.1.66 | PostgreSQL 대상 경량 ORM 및 SQL 실행 |
| Microsoft.Extensions.Configuration | 8.0.0 | 앱 설정 로드 |
| Microsoft.Extensions.Configuration.Binder | 8.0.2 | `SchedulerSettings` 바인딩 |
| Microsoft.Extensions.Configuration.Json | 8.0.1 | `appsettings.json` 읽기 |
| Microsoft.Extensions.Logging.Console | 8.0.0 | 콘솔 로그 출력 |
| Npgsql | 8.0.3 | PostgreSQL 연결 드라이버 |

## 실행 모드

- 기본 실행: 주기적으로 스케줄을 확인하고 due 작업만 수행
- `--once`: 모든 스케줄 작업을 1회 강제 실행
- `--once --smoke`: 엔드포인트별 최소 호출로 스모크 테스트 수행
- `--sync-area-codes`: 지역코드 전체 동기화
- `--sync-api-1`: `avgAllPrice` 1회 동기화
- `--sync-api-2`: `avgSidoPrice` 시도 전체 동기화
- `--sync-required-apis`: 초기 적재용 필수 API 묶음 실행
- `--sync-detail-by-id`: 저장된 대상 ID 기준 상세정보 동기화

## API 호출 일정

기본 루프는 `Runtime:TickSeconds` 값에 따라 30초마다 실행되며, 각 시각의 `00분~04분` 사이에만 해당 시간대 작업을 수행합니다.

| 구분 | 기준 시각 | 호출 API | 갱신 기준 |
| --- | --- | --- | --- |
| 현재 평균가격 계열 | 매일 `01, 02, 09, 12, 16, 19시` | `avgAllPrice`, `avgSidoPrice`, `avgSigunPrice` | 당일 시점 기준 평균 가격 갱신 |
| 최근 7일 평균가 계열 | 매일 `00시` | `avgRecentPrice`, `pollAvgRecentPrice`, `areaAvgRecentPrice` | 일별 집계가 확정되는 시점 기준 갱신 |
| 주간 평균가 | 매주 `금요일 10시` | `avgLastWeek` | 주간 평균가 공개 시점 기준 갱신 |
| 최저가 Top20 | 매일 `01, 02, 09, 12, 16, 19시` | `lowTop10` | 주요 유종별 저가 주유소 목록 갱신 |
| 반경 검색 | 매일 `01, 02, 09, 12, 16, 19시` | `aroundAll` | 고정 좌표 기준 주변 주유소 정보 갱신 |
| 상세/이름 검색 | 매일 `01, 02, 09, 12, 16, 19시` | `detailById`, `searchByName` | 시드 ID 및 상호명 기준 상세 정보 갱신 |
| 지역코드 동기화 | 매일 `03시` | `areaCode` | 전국 및 시도별 지역코드 기준정보 갱신 |

## 호출 정책과 갱신 기준

- 일일 호출 한도는 `CallPolicy:DailyLimit = 1500`입니다.
- 안전 여유분 `SafetyReserve = 100`을 제외하고 실행하므로 실제 스케줄러가 사용하는 최대 호출량은 하루 1400건입니다.
- 호출 이력 누적 사용량이 가용량을 초과하면 남은 작업은 실행하지 않습니다.
- `detailById` 대상은 최근 `lowTop10`, `searchByName` 결과에서 수집한 주유소 ID를 우선 사용하고, 비어 있으면 `StationIdsForDetail` 설정값으로 대체합니다.
- `searchByName`, `aroundAll`, `lowTop10`, `areaAvgRecentPrice`는 `appsettings.json`의 지역/상품 설정값을 기준으로 호출 대상을 결정합니다.

## 설정 예시

주요 스케줄 설정은 `appsettings.json`의 `SchedulerSettings` 아래에서 관리합니다.

```json
"Refresh": {
  "CurrentPriceHours": [1, 2, 9, 12, 16, 19],
  "DailyFinalizeHour": 0,
  "WeeklyPriceHour": 10,
  "AreaCodeSyncHour": 3
},
"Runtime": {
  "TickSeconds": 30,
  "DetailByIdMaxTargets": 50
}
```
