using FinalProjectCardList.Core.Entities;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.DataAccess.Models
{
    [Table("Notification")]
    internal class NotificationModel
    {
        [PrimaryKey]
        [Column("Id")]
        public Guid Id { get; set; }
        [Column("UserId"), NotNull]
        public Guid UserId { get; set; }
        [Column("Type"), NotNull]
        public string Type { get; set; }
        [Column("Text"), NotNull]
        public string Text { get; set; }
        [Column("ScheduledAt"), NotNull]
        public DateTime ScheduledAt { get; set; }
        [Column("IsNotified"), NotNull]
        public bool IsNotified { get; set; }
        [Column("NotifiedAt"), Nullable]
        public DateTime? NotifiedAt { get; set; }
    }
}
