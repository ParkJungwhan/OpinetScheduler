-- api 종류

--------------------------------------------------------------------------------
- 전국 주유소 평균가격(현재) : 현재 오피넷에 게시되고 있는 전국 주유소 평균 가격
rest : http://www.opinet.co.kr/api/avgAllPrice.do?out=xml&code=XXXXXX
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
-- Result
TRADE_DT 해당일자
PRODCD 제품구분코드
PRODNM 제품명
PRICE 평균가격
DIFF 전일대비 등락값

--------------------------------------------------------------------------------
- 시도별 주유소 평균가격(현재) : 현재 오피넷에 게시되고 있는 시도별 주유소 평균 가격
rest :  http://www.opinet.co.kr/api/avgSidoPrice.do?out=xml&code=XXXXXX
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
sido 선택 시도코드 2자리 ( 미입력시 모든 시도 조회 )
prodcd 선택 B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유, K015:자동차부탄
(미입력시 모든 제품 조회)
-- Result
SIDOCD 시도 구분코드
SIDONM 시도명
PRODCD 제품구분
PRICE 평균가격
DIFF 전일대비 등락값

--------------------------------------------------------------------------------
- 시군구별 주유소 평균가격(현재) : 현재 오피넷에 게시되고 있는 시/군/구별 주유소 평균 가격
rest : http://www.opinet.co.kr/api/avgSigunPrice.do?out=xml&sido=01&code=XXXXXX
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
sido 필수 시도코드 2자리
sigun 선택 시군코드 4자리 (미입력시 전체 시군 조회 )
prodcd 선택 B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유, K015:자동차부탄(미입력시 모든 제품 조회)
-- Result
SIGUNCD 시군구 구분코드
SIGUNNM 시군구명
PRODCD 제품구분
PRICE 평균가격
DIFF 전일대비 등락값

--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 평균가격 : 일 평균가격 확정 수치이며, 전일부터 이전 7일간의 전국 일일 평균가격
rest :  http://www.opinet.co.kr/api/avgRecentPrice.do?out=xml&code=XXXXXX
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
date 선택 전일부터 이전 7일 중 특정일자 (미입력시 최근 7일 모두 조회)
prodcd 선택 B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유, K015:자동차부탄
(미입력시 모든 제품 조회)
-- Result
DATE 기준일자
PRODCD 제품구분
(B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유, K015:자동차부탄)
PRICE 평균가격

--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 상표별 평균가격 : 일 평균가격 확정 수치이며, 전일부터 7일간의 전국 일일 상표별 평균가격
rest : http://www.opinet.co.kr/api/pollAvgRecentPrice.do?out=xml&code=XXXXXX&prodcd=B027
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
prodcd 선택 B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유(미입력시 모든 제품 조회)
pollcd 선택 SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표, (미입력시 모든 상표조회)
-- Result
DATE 기준일자
PRODCD 제품구분(B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유)
POLL_DIV_CD 상표 (SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표)
PRICE 평균가격

--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 지역별 평균가격 : 일 평균가격 확정 수치이며, 전일부터 7일간의 전국 일일 지역별 평균가격
rest : http://www.opinet.co.kr/api/areaAvgRecentPrice.do?out=xml&code=XXXXXX&area=0101&date=20220811
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
area 필수 시도코드(2자리): 해당시도 기준, 시군코드(4자리) ： 해당시군 기준
date 선택 전일부터 이전 7일 중 특정일자(미입력시 최근 7일 모두 조회)
prodcd 선택 B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:등유 (미입력시 모든 제품 조회)
-- Result
DATE 기준일자
AREA_CD 지역코드
AREA_NM 지역명
PRODCD 제품구분 (B034:고급휘발유, B027:보통휘발유, D047:자동차경유, C004:실내등유)
PRICE 평균가격

--------------------------------------------------------------------------------
- 최근 1주의 주간 평균가격 : (전국/시도별)
rest : http://www.opinet.co.kr/api/avgLastWeek.do?prodcd=B027&code=XXXXXX&out=xml
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
prodcd 선택 휘발유:B027, 경유:D047, 고급휘발유: B034, C004:실내등유, 자동차부탄: K015 (미입력시 모든 제품)
sido 선택 시도코드 2자리 (미입력시 전국 평균 조회)
-- Result
WEEK 기준 주 ( EX. 6월 3주 : 2016063)
STA_DT 기준 주의 시작 일자
END_DT 기준 주의 종료 일자
AREA_CD 시도 구분 (00: 전국, 그 외는 해당 시도)
PRODCD 제품구분(휘발유:B027, 경유:D047, 고급휘발유: B034, 자동차부탄: K015, C004:실내등유)
PRICE 주간 평균 유가

--------------------------------------------------------------------------------
- 전국/지역별 최저가 주유소(Top20) : 전국 또는 지역별 최저가 주유소 TOP20
rest : http://www.opinet.co.kr/api/lowTop10.do?out=xml&code=XXXXXX&prodcd=B027&area=0101&cnt=2
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
prodcd 필수 제품구분(휘발유:B027, 경유:D047, 고급휘발유: B034, 실내등유:C004, 자동차부탄: K015)
area 선택 지역구분(미입력시 전국, 시도코드(2자리):해당시도 기준, 시군코드(4자리):해당시군 기준)
cnt 선택 최저가순 결과 건수 (1 ~ 20 사이 숫자 입력, 미입력시 기본값 10건)
-- Result
UNI_ID 주유소코드
PRICE 판매가격
POLL_DIV_CD 상표(SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표, E1G: E1, SKG:SK가스 )
OS_NM 상호
VAN_ADR 지번주소
NEW_ADR 도로명주소
GIS_X_COOR GIS X좌표(KATEC)
GIS_Y_COOR GIS Y좌표(KATEC)

--------------------------------------------------------------------------------
- 반경 내 주유소 검색 : 특정 위치 중심으로 반경 내 주유소
rest : http://www.opinet.co.kr/api/aroundAll.do?code=XXXXXX&x=314681.8&y=544837&radius=5000&sort=1&prodcd=B027&out=xml
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
x 필수 기준 위치 X좌표 (KATEC)
y 필수 기준 위치 Y좌표 (KATEC)
radius 필수 반경 선택 ( 최대 5000, 단위 :m)
prodcd 필수 제품 (휘발유:B027, 경유:D047, 고급휘발유: B034, 실내등유: C004, 자동차부탄: K015)
sort 필수 1: 가격순, 2: 거리순
-- Result
UNI_ID 주유소코드
POLL_DIV_CD 상표(SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표, E1G: E1, SKG:SK가스
OS_NM 상호
PRICE 판매가격
DISTANCE 기준 위치로부터의 거리 (단위 : m)
GIS_X_COOR GIS X좌표(KATEC)
GIS_Y_COOR GIS Y좌표(KATEC)

--------------------------------------------------------------------------------
- 주유소 상세정보(ID) : 특정 주유소(ID)에 대한 기본정보 및 가격정보 등 상세정보
rest : https://www.opinet.co.kr/api/detailById.do?code=XXXXXX&id=A0002517&out=xml
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
id 필수 
-- Result
UNI_ID 주유소코드
POLL_DIV_CD 상표(SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, GPOLL_DIV_CD RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표, E1G: E1, SKG:SK가스
OS_NM 상호
VAN_ADR 지번주소
NEW_ADR 도로명주소
TEL 전화번호
SIGUNCD 소재지역 시군코드
LPG_YN 업종구분 (N:주유소, Y:자동차충전소, C:주유소/충전소 겸업)
MAINT_YN 경정비 시설 존재 여부
CAR_WASH_YN 세차장 존재 여부
KPETRO_YN 품질인증주유소 여부 (한국석유관리원의 품질인증프로그램 협약 업체)
CVS_YN 편의점 존재 여부
GIS_X_COOR GIS X좌표(KATEC)
GIS_Y_COOR GIS Y좌표(KATEC)
OIL_PRICE.PRODCD 휘발유:B027, 경유:D047, 고급휘발유: B034, 실내등유: C004, 자동차부탄: K015
OIL_PRICE.PRICE 판매가격
OIL_PRICE.TRADE_DT 기준일자
OIL_PRICE.TRADE_TM 기준시간

--------------------------------------------------------------------------------
- 상호로 주유소 검색 : 상호 검색을 통한 주유소 조회
rest : http://www.opinet.co.kr/api/searchByName.do?code=XXXXXX&out=xml&osnm=보라매&area=01
-- Request
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
osnm 필수 상호 검색명 (검색어 두글자 이상)
area 선택 검색지역(시도) 코드. 미입력시 전국 기준 검색
-- Result
UNI_ID 주유소코드
POLL_DIV_CD 주유소상표 (SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, RTO:자영알뜰, RTX:고속도로알뜰, NHO:농협알뜰, ETC:자가상표
GPOLL_DIV_CD 충전소상표 (SKE:SK에너지, GSC:GS칼텍스, HDO:현대오일뱅크, SOL:S-OIL, ETC:자가상표, E1G: E1, SKG:SK가스
OS_NM 상호
VAN_ADR 지번주소
NEW_ADR 도로명주소
SIGUNCD 소재지 시군코드
LPG_YN 업종구분 (N:주유소, Y: 자동차충전소, C:주유소/충전소 겸업)
GIS_X_COOR GIS X좌표(KATEC)
GIS_Y_COOR GIS Y좌표(KATEC)

--------------------------------------------------------------------------------
- 전국 면세유 주유소 평균가격(현재) : 

--------------------------------------------------------------------------------
- 시도별 면세유 주유소 평균가격(현재)
--------------------------------------------------------------------------------
- 시군구별 면세유 주유소 평균가격(현재)
--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 면세유 평균가격
--------------------------------------------------------------------------------
- 최근 7일간 전국 일일 상표별 면세유 평균가격
--------------------------------------------------------------------------------
- 전국/지역별 최저가 면세유 주유소(Top20)
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

--------------------------------------------------------------------------------
- 지역코드 조회 : 오피넷 데이터 관련 지역코드
--------------------------------------------------------------------------------
rest : https://www.opinet.co.kr/api/areaCode.do?out=xml&code=XXXXXX&area=01
-- Request 
code 필수 공사에서 부여한 키(key) 정보
out 필수 정보 노출 형식을 정의 한다.(xml/json)
area 선택 미입력시 시도별 코드 조회, 특정 시도코드 입력시 지역내 시/군/구 코드 조회
-- Result
AREA_CD 지역코드
AREA_NM 지역명

----------------------------------------
----------------------------------------
- 하루 call 제한 수 : 1500

-- 갱신시간
- 현재 판매가격 : 1,2,9,12,16,19시
- 일일 평균가격 : 24시
- 주간 평균가격 : 금요일 10시
- 요소수 판매가격 : 7,13,18,24시