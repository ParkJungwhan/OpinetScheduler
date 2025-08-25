# 구현 대상 API 목록 (Opinet API Free 2024)

> 면세유(면세유 전용 API) 관련 엔드포인트는 제외합니다.

## 제품 코드
- B034: 고급휘발유, B027: 보통휘발유, D047: 자동차경유, C004: 실내등유, K015: 자동차부탄

## 평균가격 계열
- 전국 평균가: `/api/avgAllPrice.do`
  - GET, 필수: `code`, `out`
  - 예: `http://www.opinet.co.kr/api/avgAllPrice.do?out=json&code=XXXXXX`
- 시도별 평균가: `/api/avgSidoPrice.do`
  - GET, 필수: `code`, `out`; 선택: `sido`, `prodcd`
  - 예: `.../avgSidoPrice.do?out=json&code=XXXXXX&sido=01&prodcd=B027`
- 시군구별 평균가: `/api/avgSigunPrice.do`
  - GET, 필수: `code`, `out`, `sido`; 선택: `sigun`, `prodcd`
  - 예: `.../avgSigunPrice.do?out=json&code=XXXXXX&sido=01&sigun=0101&prodcd=D047`
- 최근 7일 전국 일일 평균가: `/api/avgRecentPrice.do`
  - GET, 필수: `code`, `out`; 선택: `date`, `prodcd`
- 최근 7일 상표별 평균가: `/api/pollAvgRecentPrice.do`
  - GET, 필수: `code`, `out`; 선택: `prodcd`, `pollcd`
- 최근 7일 지역별 평균가: `/api/areaAvgRecentPrice.do`
  - GET, 필수: `code`, `out`, `area`; 선택: `date`, `prodcd`
- 최근 1주 주간 평균가(전국/시도): `/api/avgLastWeek.do`
  - GET, 필수: `code`, `out`; 선택: `prodcd`, `sido`

## 주유소 검색/상세
- 전국/지역별 최저가 Top N: `/api/lowTop10.do`
  - GET, 필수: `code`, `out`, `prodcd`; 선택: `area`, `cnt(1~20)`
  - 예: `.../lowTop10.do?out=json&code=XXXXXX&prodcd=B027&area=0101&cnt=20`
- 반경 내 검색: `/api/aroundAll.do`
  - GET, 필수: `code`, `out`, `x`, `y`, `radius(<=5000)`, `prodcd`, `sort(1|2)`
- 주유소 상세(코드): `/api/detailById.do`
  - GET, 필수: `code`, `out`, `id`
- 상호로 검색: `/api/searchByName.do`
  - GET, 필수: `code`, `out`, `osnm(2자 이상)`; 선택: `area`

## 보조 데이터
- 요소수 판매가격(지역별): `/api/ureaPrice.do`
  - GET, 필수: `code`, `out`, `area`
- 지역코드 조회: `/api/areaCode.do`
  - GET, 필수: `code`, `out`; 선택: `area(시도코드 2자리)`

## 비범위(구현 제외)
- 면세유 관련 평균가/최저가/상표별 API 일체 (문서 내 “면세유” 표기 엔드포인트)

