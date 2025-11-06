namespace OpinetScheduler.Models;

// 공통 응답 래퍼 (RESULT.OIL)
public class OpinetEnvelope<T>
{
    public OpinetResult<T>? RESULT { get; set; }
}

public class OpinetResult<T>
{
    public List<T>? OIL { get; set; }
}

// 샘플 DTO들 (필요 시 추가)
public class AvgAllPriceItem
{
    public string? TRADE_DT { get; set; }
    public string? PRODCD { get; set; }
    public string? PRODNM { get; set; }
    public string? PRICE { get; set; }
    public string? DIFF { get; set; }
}

public class SidoPriceItem
{
    public string? SIDOCD { get; set; }
    public string? SIDONM { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
    public string? DIFF { get; set; }
}

public class SigunPriceItem
{
    public string? SIGUNCD { get; set; }
    public string? SIGUNNM { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
    public string? DIFF { get; set; }
}

public class AvgRecentPriceItem
{
    public string? DATE { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
}

public class PollAvgRecentPriceItem
{
    public string? DATE { get; set; }
    public string? PRODCD { get; set; }
    public string? POLL_DIV_CD { get; set; }
    public string? PRICE { get; set; }
}

public class AreaAvgRecentPriceItem
{
    public string? DATE { get; set; }
    public string? AREA_CD { get; set; }
    public string? AREA_NM { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
}

public class AvgLastWeekItem
{
    public string? WEEK { get; set; }
    public string? STA_DT { get; set; }
    public string? END_DT { get; set; }
    public string? AREA_CD { get; set; }
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
}

public class LowTopItem
{
    public string? UNI_ID { get; set; }
    public string? PRICE { get; set; }
    public string? POLL_DIV_CD { get; set; }
    public string? OS_NM { get; set; }
    public string? VAN_ADR { get; set; }
    public string? NEW_ADR { get; set; }
    public string? GIS_X_COOR { get; set; }
    public string? GIS_Y_COOR { get; set; }
}

public class AroundAllItem
{
    public string? UNI_ID { get; set; }
    public string? POLL_DIV_CD { get; set; }
    public string? OS_NM { get; set; }
    public string? PRICE { get; set; }
    public string? DISTANCE { get; set; }
    public string? GIS_X_COOR { get; set; }
    public string? GIS_Y_COOR { get; set; }
}

public class DetailByIdItem
{
    public string? UNI_ID { get; set; }
    public string? POLL_DIV_CD { get; set; }
    public string? OS_NM { get; set; }
    public string? VAN_ADR { get; set; }
    public string? NEW_ADR { get; set; }
    public string? TEL { get; set; }
    public string? SIGUNCD { get; set; }
    public string? LPG_YN { get; set; }
    public string? MAINT_YN { get; set; }
    public string? CAR_WASH_YN { get; set; }
    public string? KPETRO_YN { get; set; }
    public string? CVS_YN { get; set; }
    public string? GIS_X_COOR { get; set; }
    public string? GIS_Y_COOR { get; set; }
    public List<OilPriceItem>? OIL_PRICE { get; set; }
}

public class OilPriceItem
{
    public string? PRODCD { get; set; }
    public string? PRICE { get; set; }
    public string? TRADE_DT { get; set; }
    public string? TRADE_TM { get; set; }
}

public class SearchByNameItem
{
    public string? UNI_ID { get; set; }
    public string? POLL_DIV_CD { get; set; }
    public string? OS_NM { get; set; }
    public string? NEW_ADR { get; set; }
    public string? SIGUNCD { get; set; }
    public string? GIS_X_COOR { get; set; }
    public string? GIS_Y_COOR { get; set; }
}

public class UreaPriceItem
{
    public string? UNI_ID { get; set; }
    public string? OS_NM { get; set; }
    public string? ADRESS { get; set; }
    public string? TEL { get; set; }
    public string? GIS_X_COOR { get; set; }
    public string? GIS_Y_COOR { get; set; }
    public string? STOCK_YN { get; set; }
    public string? PRICE { get; set; }
    public string? TRADE_DT { get; set; }
    public string? TRADE_TM { get; set; }
}

public class AreaCodeItem
{
    public string? AREA_CD { get; set; }
    public string? AREA_NM { get; set; }
}