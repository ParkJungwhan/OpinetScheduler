using OpinetScheduler.Models;

namespace OpinetScheduler.Services;

public class OpinetMDB
{
    // dapper로 sqlite에 연결하는 코드 작성

    private OpinetClient opinetcli;

    public OpinetMDB(OpinetClient _opinetcli)
    {
        // "OpinetMDB.db" sqlite3 파일 확인
        opinetcli = _opinetcli;
    }

    public async Task GetMasterData()
    {
        // sqlite 파일 연결해서 데이터 가져오기
        // Area 정보

        Dictionary<string, List<AreaCodeItem>> allArea = new();

        var allDiv = opinetcli.GetAreaCodeAsync();
        var div = allDiv.Result.RESULT;
        foreach (var item in div.OIL!)
        {
            Console.WriteLine($"{item.AREA_CD} {item.AREA_NM}");

            var cd_area = opinetcli.GetAreaCodeAsync(item.AREA_CD);
            var area = cd_area.Result.RESULT.OIL;

            allArea.Add(item.AREA_CD, area);
        }
    }
}