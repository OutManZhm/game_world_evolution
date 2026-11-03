using SqlSugar;

namespace GWE.Repository;

/// <summary>数据库会话：SqlSugarScope 单例 + 事务边界。业务层只依赖 IDbSession。</summary>
public interface IDbSession
{
    ISqlSugarClient Client { get; }
    void ExecuteInTransaction(Action work);
}

public sealed class SqlSugarDbSession : IDbSession
{
    public SqlSugarDbSession(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Client = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = $"DataSource={databasePath}",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    public ISqlSugarClient Client { get; }

    public void ExecuteInTransaction(Action work)
    {
        Client.Ado.BeginTran();
        try
        {
            work();
            Client.Ado.CommitTran();
        }
        catch
        {
            Client.Ado.RollbackTran();
            throw;
        }
    }
}
