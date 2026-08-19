using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace FinalProjectCardList.Core.DataAccess.Models
{
    [Table("ToDoUser")]
    internal class ToDoUserModel
    {
        [PrimaryKey, Identity, Column("UserId")]
        public Guid UserId { get; set; }
        [Column("TelegramUserName"), NotNull]
        public string TelegramUserName { get; set; }
        [Column("RegisteredAt")]
        public DateTime RegisteredAt { get; set; }
        [Column("TelegramUserId")]
        public long TelegramUserId { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoListModel.User.UserId))]
        public IList<ToDoListModel> ToDoList { get; set; } = [];


    }
}
