using LinqToDB;
using LinqToDB.Data;
using FinalProjectCardList.Core.DataAccess.Models;

namespace FinalProjectCardList.Core.Infrastructure.DataAccess;

internal class ToDoDataContext : LinqToDB.Data.DataConnection
{
    public ToDoDataContext(string connectionString)
        : base(ProviderName.PostgreSQL, connectionString)
    {
    }
    public ITable<ToDoUserModel> ToDoUser => this.GetTable<ToDoUserModel>();
    public ITable<ToDoListModel> ToDoList => this.GetTable<ToDoListModel>();
    public ITable<ToDoItemModel> ToDoItem => this.GetTable<ToDoItemModel>();
    public ITable<NotificationModel> Notification => this.GetTable<NotificationModel>();
}
