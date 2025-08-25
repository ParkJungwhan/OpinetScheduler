public class OIL
{
    public string TRADE_DT { get; set; }
    public string PRODCD { get; set; }
    public string PRODNM { get; set; }
    public string PRICE { get; set; }
    public string DIFF { get; set; }
}

public class RESULT
{
    public List<OIL> OIL { get; set; }
}

public class Root
{
    public RESULT RESULT { get; set; }
}