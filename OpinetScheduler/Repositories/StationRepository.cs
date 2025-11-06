using System;
using System.Collections.Generic;
using OpinetScheduler.Services;

namespace OpinetScheduler.Repositories;

public class StationRepository : IStationRepository
{
    private readonly DapperService dbconnection;

    public StationRepository(DapperService _dbconnection)
    {
        dbconnection = _dbconnection;
    }

    public IEnumerable<AreaInfo> GetAllAreaInfo()
    {
        string query = "select areaid, areaname from area";
        var result = dbconnection.GetQuery<AreaInfo>(query);
        return result;
    }

    public int SetAreaInfo(int AreaID, string AreaName)
    {
        string query = $"insert into area(areaid, areaname, updatetime) values({AreaID}, '{AreaName}', 'now()')";
        var result = dbconnection.SetQuery(query);

        return result;
    }
}