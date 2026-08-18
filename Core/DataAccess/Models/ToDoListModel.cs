using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace FinalProjectCardList.Core.DataAccess.Models
{
    [Table("ToDoLists")]
    internal class ToDoListModel
    {
        [Column("Id")]
        [PrimaryKey]
        public Guid Id { get; set; }

        [Column("Name")]
        public string? Name { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("UserId")]
        public Guid UserId { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId))]
        public ToDoUserModel? User { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ToDoItemModel.List.Id))]
        public IList<ToDoItemModel> Items { get; set; } = new List<ToDoItemModel>();
    }
}
