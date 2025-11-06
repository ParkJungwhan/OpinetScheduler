using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OpinetScheduler.Services;

public class DapperService
{
    /// dapper로 mongoDB에 연결하는 코드 작성
    /// 각 메서드는 비동기 방식으로 작성
    /// OpinetClient에서 데이터를 받아와서 MongoDB에 저장하는 기능 포함
    private string connectionString;

    private readonly DbConnection _dbConnection;
    private Stopwatch sw = new Stopwatch();

    public DapperService(IConfiguration config)
    {
        try
        {
            connectionString = config["ConnectionStrings:PostgressConnection"];
            Debug.Assert(!string.IsNullOrEmpty(connectionString));

            _dbConnection = new NpgsqlConnection(connectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error connecting to the database: " + ex.Message);
            throw;
        }
    }

    public IEnumerable<T> GetQuery<T>(string query, int timeout = 100)
    {
        Debug.Assert(null != _dbConnection);

        // 연결을 엽니다.
        _dbConnection.Open();

        sw.Restart();
        var result = _dbConnection.Query<T>(query, commandTimeout: timeout);
        Debug.WriteLine($"{DateTime.Now}\tGetQuery\t{sw.ElapsedMilliseconds}ms\t{query}");
        sw.Stop();

        _dbConnection.Close();

        return result;
    }

    public int SetQuery(string query, int timeout = 100)
    {
        Debug.Assert(null != _dbConnection);

        // 연결을 엽니다.
        _dbConnection.Open();

        sw.Restart();
        var affectedRows = _dbConnection.Execute(query, commandTimeout: timeout);
        Debug.WriteLine($"{DateTime.Now}\tSetQuery\t{sw.ElapsedMilliseconds}ms\t{query}");
        sw.Stop();

        _dbConnection.Close();

        Debug.WriteLine($"{DateTime.Now}\tSetQuery\tResult : {affectedRows}");
        return affectedRows;
    }
}