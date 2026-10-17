using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using GithubAnalyzer.Worker.Database;
namespace GithubAnalyzer.Worker.Tests.Mocks;

/// <summary>
/// Catatan eksekusi command SQL oleh Dapper.
/// </summary>
public sealed record ExecutedCommand(string Sql, Dictionary<string, object?> Parameters);

/// <summary>
/// In-memory mock database untuk menguji operasi Dapper AOT tanpa koneksi server PostgreSQL riil.
/// </summary>
public sealed class InMemoryDatabase
{
    private readonly List<ExecutedCommand> _executedCommands = [];
    private readonly Dictionary<string, DataTable> _queryResponses = [];
    private readonly Dictionary<string, int> _nonQueryResponses = [];

    public IReadOnlyList<ExecutedCommand> ExecutedCommands => _executedCommands;

    public void SetupQuery(string sqlPattern, DataTable table)
    {
        _queryResponses[sqlPattern] = table;
    }

    public void SetupNonQuery(string sqlPattern, int rowsAffected)
    {
        _nonQueryResponses[sqlPattern] = rowsAffected;
    }

    public IDbConnectionFactory CreateConnectionFactory()
    {
        return new MockDbConnectionFactory(this);
    }

    internal int HandleExecuteNonQuery(string sql, Dictionary<string, object?> parameters)
    {
        _executedCommands.Add(new ExecutedCommand(sql, parameters));

        foreach (var (pattern, rows) in _nonQueryResponses)
        {
            if (sql.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return rows;
        }

        return 1; // Default 1 row affected
    }

    internal DbDataReader HandleExecuteReader(string sql, Dictionary<string, object?> parameters)
    {
        _executedCommands.Add(new ExecutedCommand(sql, parameters));

        foreach (var (pattern, table) in _queryResponses)
        {
            if (sql.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return table.CreateDataReader();
        }

        return new DataTable().CreateDataReader();
    }
}

internal sealed class MockDbConnectionFactory(InMemoryDatabase db) : IDbConnectionFactory
{
    public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        return ValueTask.FromResult<DbConnection>(new InMemoryDbConnection(db));
    }
}

internal sealed class InMemoryDbConnection(InMemoryDatabase db) : DbConnection
{
    private ConnectionState _state = ConnectionState.Open;

    [AllowNull]
    public override string ConnectionString { get; set; } = "Data Source=:memory:";
    public override string Database => "InMemoryTestDb";
    public override string DataSource => ":memory:";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        _state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => new FakeDbTransaction(this);

    protected override DbCommand CreateDbCommand()
        => new InMemoryDbCommand(db, this);
}

internal sealed class FakeDbTransaction(DbConnection connection) : DbTransaction
{
    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    protected override DbConnection? DbConnection => connection;
    public override void Commit() { }
    public override void Rollback() { }
}

internal sealed class InMemoryDbCommand(InMemoryDatabase db, DbConnection connection) : DbCommand
{
    private readonly InMemoryDbParameterCollection _parameters = new();

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; } = false;
    public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;
    protected override DbConnection? DbConnection { get; set; } = connection;
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }
    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new InMemoryDbParameter();

    public override int ExecuteNonQuery()
    {
        return db.HandleExecuteNonQuery(CommandText, _parameters.ToDictionary());
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(db.HandleExecuteNonQuery(CommandText, _parameters.ToDictionary()));
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return db.HandleExecuteReader(CommandText, _parameters.ToDictionary());
    }

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        return Task.FromResult(db.HandleExecuteReader(CommandText, _parameters.ToDictionary()));
    }

    public override object? ExecuteScalar()
    {
        using var reader = ExecuteDbDataReader(CommandBehavior.Default);
        return reader.Read() && reader.FieldCount > 0 ? reader.GetValue(0) : null;
    }

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ExecuteScalar());
    }
}

internal sealed class InMemoryDbParameter : DbParameter
{
    public override DbType DbType { get; set; } = DbType.String;
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; } = true;
    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }

    public override void ResetDbType() { }
}

internal sealed class InMemoryDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = [];

    public override int Count => _parameters.Count;
    public override object SyncRoot => this;

    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var v in values) Add(v);
    }

    public override void Clear() => _parameters.Clear();
    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_parameters).CopyTo(array, index);
    public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _parameters.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _parameters.RemoveAt(index);
    public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) => _parameters[IndexOf(parameterName)] = value;

    public Dictionary<string, object?> ToDictionary()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in _parameters)
        {
            var key = p.ParameterName.TrimStart('@', ':');
            dict[key] = p.Value;
        }
        return dict;
    }
}
