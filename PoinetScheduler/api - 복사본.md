# Opinet API 사용 가이드 (2024 Free) — 면세유 제외

공통
- Base URL: `https://www.opinet.co.kr`
- 포맷: `out=json|xml` (예시는 `json` 기준)
- 인증: `code=발급키`
- 제품코드: `B034(고급휘발유)`, `B027(보통휘발유)`, `D047(자동차경유)`, `C004(실내등유)`, `K015(자동차부탄)`
- API Limit: 하루 1,500회
- 갱신주기: 현재 판매가격(1,2,9,12,16,19시), 일일 평균가격(24시), 주간 평균가격(금 10시), 요소수(7,13,18,24시)

## 1) 전국 주유소 평균가격(현재) — avgAllPrice
- Endpoint: `GET /api/avgAllPrice.do`
- Query: `code`(필수), `out`(필수)
- 응답 필드: `TRADE_DT`, `PRODCD`, `PRODNM`, `PRICE`, `DIFF`
- 예시: `/api/avgAllPrice.do?out=json&code=XXXXXX`
- 샘플 응답
  {"RESULT":{"OIL":[{"TRADE_DT":"20240825","PRODCD":"B027","PRODNM":"보통휘발유","PRICE":"1725.43","DIFF":"-2.10"}]}}

## 2) 시도별 평균가격(현재) — avgSidoPrice
- Endpoint: `GET /api/avgSidoPrice.do`
- Query: `code`(필수), `out`(필수), `sido`(선택, 2자리), `prodcd`(선택)
- 응답 필드: `SIDOCD`, `SIDONM`, `PRODCD`, `PRICE`, `DIFF`
- 예시: `/api/avgSidoPrice.do?out=json&code=XXXXXX&sido=01&prodcd=B027`
- 샘플 응답
  {"RESULT":{"OIL":[{"SIDOCD":"01","SIDONM":"서울","PRODCD":"B027","PRICE":"1810.12","DIFF":"-1.00"}]}}

## 3) 시군구별 평균가격(현재) — avgSigunPrice
- Endpoint: `GET /api/avgSigunPrice.do`
- Query: `code`(필수), `out`(필수), `sido`(필수, 2자리), `sigun`(선택, 4자리), `prodcd`(선택)
- 응답 필드: `SIGUNCD`, `SIGUNNM`, `PRODCD`, `PRICE`, `DIFF`
- 예시: `/api/avgSigunPrice.do?out=json&code=XXXXXX&sido=01&sigun=0101&prodcd=D047`
- 샘플 응답
  {"RESULT":{"OIL":[{"SIGUNCD":"0101","SIGUNNM":"강남구","PRODCD":"D047","PRICE":"1670.50","DIFF":"-0.80"}]}}

## 4) 최근 7일 전국 일일 평균가격 — avgRecentPrice
- Endpoint: `GET /api/avgRecentPrice.do`
- Query: `code`(필수), `out`(필수), `date`(선택, YYYYMMDD), `prodcd`(선택)
- 응답 필드: `DATE`, `PRODCD`, `PRICE`
- 예시: `/api/avgRecentPrice.do?out=json&code=XXXXXX&prodcd=B027`
- 샘플 응답
  {"RESULT":{"OIL":[{"DATE":"20240819","PRODCD":"B027","PRICE":"1728.10"}]}}

## 5) 최근 7일 상표별 평균가격 — pollAvgRecentPrice
- Endpoint: `GET /api/pollAvgRecentPrice.do`
- Query: `code`(필수), `out`(필수), `prodcd`(선택), `pollcd`(선택)
- 응답 필드: `DATE`, `PRODCD`, `POLL_DIV_CD`, `PRICE`
- 예시: `/api/pollAvgRecentPrice.do?out=json&code=XXXXXX&prodcd=B027&pollcd=SKE`
- 샘플 응답
  {"RESULT":{"OIL":[{"DATE":"20240824","PRODCD":"B027","POLL_DIV_CD":"SKE","PRICE":"1750.30"}]}}

## 6) 최근 7일 지역별 평균가격 — areaAvgRecentPrice
- Endpoint: `GET /api/areaAvgRecentPrice.do`
- Query: `code`(필수), `out`(필수), `area`(필수, 시도2/시군4), `date`(선택), `prodcd`(선택)
- 응답 필드: `DATE`, `AREA_CD`, `AREA_NM`, `PRODCD`, `PRICE`
- 예시: `/api/areaAvgRecentPrice.do?out=json&code=XXXXXX&area=0101&prodcd=B027`
- 샘플 응답
  {"RESULT":{"OIL":[{"DATE":"20240824","AREA_CD":"0101","AREA_NM":"서울 강남구","PRODCD":"B027","PRICE":"1815.20"}]}}

## 7) 최근 1주 주간 평균가격 — avgLastWeek
- Endpoint: `GET /api/avgLastWeek.do`
- Query: `code`(필수), `out`(필수), `prodcd`(선택), `sido`(선택)
- 응답 필드: `WEEK`, `STA_DT`, `END_DT`, `AREA_CD(00=전국)`, `PRODCD`, `PRICE`
- 예시: `/api/avgLastWeek.do?out=json&code=XXXXXX&prodcd=B027&sido=01`
- 샘플 응답
  {"RESULT":{"OIL":[{"WEEK":"202434","STA_DT":"20240819","END_DT":"20240825","AREA_CD":"01","PRODCD":"B027","PRICE":"1751.00"}]}}

## 8) 전국/지역별 최저가 Top N — lowTop10
- Endpoint: `GET /api/lowTop10.do`
- Query: `code`(필수), `out`(필수), `prodcd`(필수), `area`(선택), `cnt(1~20)`(선택)
- 응답 필드: `UNI_ID`, `PRICE`, `POLL_DIV_CD`, `OS_NM`, `VAN_ADR`, `NEW_ADR`, `GIS_X_COOR`, `GIS_Y_COOR`
- 예시: `/api/lowTop10.do?out=json&code=XXXXXX&prodcd=B027&area=0101&cnt=10`
- 샘플 응답
  {"RESULT":{"OIL":[{"UNI_ID":"A0002517","OS_NM":"OO주유소","PRICE":"1650","POLL_DIV_CD":"SOL","NEW_ADR":"서울 강남구 ...","GIS_X_COOR":"198123","GIS_Y_COOR":"445678"}]}}

## 9) 반경 내 주유소 검색 — aroundAll
- Endpoint: `GET /api/aroundAll.do`
- Query: `code`(필수), `out`(필수), `x`(필수, KATEC), `y`(필수), `radius<=5000`(필수, m), `prodcd`(필수), `sort=1|2`(필수)
- 응답 필드: `UNI_ID`, `POLL_DIV_CD`, `OS_NM`, `PRICE`, `DISTANCE`, `GIS_X_COOR`, `GIS_Y_COOR`
- 예시: `/api/aroundAll.do?out=json&code=XXXXXX&x=314681.8&y=544837&radius=5000&sort=1&prodcd=B027`
- 샘플 응답
  {"RESULT":{"OIL":[{"UNI_ID":"A0001001","OS_NM":"OO셀프","POLL_DIV_CD":"GSC","PRICE":"1715","DISTANCE":"820","GIS_X_COOR":"314900","GIS_Y_COOR":"544990"}]}}

## 10) 주유소 상세정보(ID) — detailById
- Endpoint: `GET /api/detailById.do`
- Query: `code`(필수), `out`(필수), `id`(필수)
- 응답 필드(기본): `UNI_ID`,`POLL_DIV_CD`,`OS_NM`,`NEW_ADR`,`TEL`,`SIGUNCD`,`LPG_YN`,`MAINT_YN`,`CAR_WASH_YN`,`KPETRO_YN`,`CVS_YN`,`GIS_X_COOR`,`GIS_Y_COOR`
- 응답 필드(가격): `OIL_PRICE[]:{PRODCD,PRICE,TRADE_DT,TRADE_TM}`
- 예시: `/api/detailById.do?out=json&code=XXXXXX&id=A0002517`
- 샘플 응답
  {"RESULT":{"OIL":[{"UNI_ID":"A0002517","OS_NM":"OO주유소","POLL_DIV_CD":"SKE","NEW_ADR":"서울 ...","TEL":"02-000-0000","SIGUNCD":"0101","LPG_YN":"N","MAINT_YN":"Y","CAR_WASH_YN":"Y","KPETRO_YN":"N","CVS_YN":"N","GIS_X_COOR":"198123","GIS_Y_COOR":"445678","OIL_PRICE":[{"PRODCD":"B027","PRICE":"1729","TRADE_DT":"20240825","TRADE_TM":"1020"}]}]}}

<<<<<<< HEAD:api.md
## 11) 상호로 주유소 검색 — searchByName
- Endpoint: `GET /api/searchByName.do`
- Query: `code`(필수), `out`(필수), `osnm`(필수, 2자 이상), `area`(선택, 시도)
- 응답 필드: `UNI_ID`, `POLL_DIV_CD`, `OS_NM`, `NEW_ADR`, `SIGUNCD`, `GIS_X_COOR`, `GIS_Y_COOR`
- 예시: `/api/searchByName.do?out=json&code=XXXXXX&osnm=보라매&area=01`
- 샘플 응답
  {"RESULT":{"OIL":[{"UNI_ID":"A0003001","OS_NM":"보라매OO주유소","POLL_DIV_CD":"RTO","NEW_ADR":"서울 동작구 ...","SIGUNCD":"0115"}]}}

## 12) 요소수 판매가격(지역별) — ureaPrice
- Endpoint: `GET /api/ureaPrice.do`
- Query: `code`(필수), `out`(필수), `area`(필수, 시도 2자리)
- 응답 필드: `UNI_ID`, `OS_NM`, `ADRESS`, `TEL`, `GIS_X_COOR`, `GIS_Y_COOR`, `STOCK_YN`, `PRICE`, `TRADE_DT`, `TRADE_TM`
- 예시: `/api/ureaPrice.do?out=json&code=XXXXXX&area=01`
- 샘플 응답
  {"RESULT":{"OIL":[{"UNI_ID":"A0009001","OS_NM":"OO주유소","ADRESS":"서울 ...","TEL":"02-...","STOCK_YN":"Y","PRICE":"1200","TRADE_DT":"20240825","TRADE_TM":"1300"}]}}
=======
--------------------------------------------------------------------------------
- 전국 면세유 주유소 평균가격(현재)  // 제외
--------------------------------------------------------------------------------
- 시도별 면세유 주유소 평균가격(현재) // 제외
--------------------------------------------------------------------------------
- 시군구별 면세유 주유소 평균가격(현재)  // 제외
--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 면세유 평균가격  // 제외
--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 상표별 면세유 평균가격  // 제외
--------------------------------------------------------------------------------
- 전국/지역별 최저가 면세유 주유소(Top20)  // 제외
--------------------------------------------------------------------------------
- 주유소의 요소수 판매가격(지역별) : 요소수 판매 주유소의 재고유무 및 판매단가
rest : https://www.opinet.co.kr/api/ureaPrice.do?code=XXXXXX&out=xml&area=01
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
area 필수 시도코드(2자리) 입력 시 해당 지역의 데이터 조회
--Result
UNI_ID 주유소코드
OS_NM 상호
ADRESS 주소
TEL 전화번호
GIS_X_COOR GIS X좌표(KATEC)
GIS_Y_COOR GIS Y좌표(KATEC)
STOCK_YN 재고유무 (Y/N)
PRICE 요소수 판매단가(원/리터) *업데이트 시각 : 7시, 13시, 18시, 24시
TRADE_DT 보고일자(카드결제일자)
TRADE_TM 보고시각(카드결제시각)
>>>>>>> origin/develop:PoinetScheduler/api - 복사본.md

## 13) 지역코드 조회 — areaCode
- Endpoint: `GET /api/areaCode.do`
- Query: `code`(필수), `out`(필수), `area`(선택, 시도코드 2자리 입력시 해당 시군구 목록)
- 응답 필드: `AREA_CD`, `AREA_NM`
- 예시: `/api/areaCode.do?out=json&code=XXXXXX` 또는 `/api/areaCode.do?out=json&code=XXXXXX&area=01`
- 샘플 응답
  {"RESULT":{"OIL":[{"AREA_CD":"01","AREA_NM":"서울"},{"AREA_CD":"0101","AREA_NM":"강남구"}]}}

<<<<<<< HEAD:api.md
### 제외(비범위)
- 면세유 관련 평균가/상표별/최저가 등 면세유 전용 엔드포인트 전부 제외
=======
----------------------------------------
----------------------------------------
- API Limit(하루 call 제한 수) : 1500
- 갱신시간
  - 현재 판매가격 : 1,2,9,12,16,19시
  - 일일 평균가격 : 24시
  - 주간 평균가격 : 금요일 10시
  - 요소수 판매가격 : 7,13,18,24시
>>>>>>> origin/develop:PoinetScheduler/api - 복사본.md
