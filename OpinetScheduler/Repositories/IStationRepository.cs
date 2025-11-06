namespace OpinetScheduler.Repositories;

public interface IStationRepository
{
    IEnumerable<AreaInfo> GetAllAreaInfo();

    int SetAreaInfo(int AreaID, string AreaName);
}

public class AreaInfo
{
    public int areaid { get; set; }
    public string areaname { get; set; }
    public string areaidstring => areaid.ToString("D2");
}