using FinalProjectCardList.Core.Infrastructure.DataAccess;

namespace FinalProjectCardList.Core.DataAccess;

public class DataContextFactory : IDataContextFactory<ToDoDataContext>
{
    private readonly string _connectionString;

    public DataContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    ToDoDataContext IDataContextFactory<ToDoDataContext>.CreateDataContext() => new ToDoDataContext(_connectionString);
}
