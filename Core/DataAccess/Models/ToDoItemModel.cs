using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FinalProjectCardList.Core.DataAccess.Models
{
    [Table("ToDoItem")]
    internal class ToDoItemModel
    {
        [PrimaryKey, Column("Id")]
   
        public Guid Id { get; set; }
        // Внешние ключи
        [Column("UserId")]
        public Guid UserId { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("State")]
        public ToDoItemState State { get; set; }

        [Column("StateChangedAt")]
        public DateTime? StateChangedAt { get; set; }

        [Column("Deadline")]
        public DateTime? Deadline { get; set; }

        [Column("ListId")]
        public Guid? ListId { get; set; }
        [Column("Quantity")]
        [Column] public int Quantity { get; set; } = 1;

        // Связи
        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId))]
        public ToDoUserModel? User { get; set; }

        [Association(ThisKey = nameof(ListId), OtherKey = nameof(ToDoListModel.Id))]
        public ToDoListModel? List { get; set; }
    }
}
