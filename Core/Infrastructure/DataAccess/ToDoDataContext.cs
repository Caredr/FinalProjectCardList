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
    public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
    public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
    public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
    public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();
}
