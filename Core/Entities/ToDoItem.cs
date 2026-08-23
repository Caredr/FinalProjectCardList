using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    public class ToDoItem 
    {
        public Guid Id { get; set; } 
        public ToDoUser UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } 
        public ToDoItemState State { get; set; }
        public DateTime? StateChangedAt { get; set; } 
        public DateTime? Deadline { get; set; } 
        public ToDoList? ListId { get; set; } 
        public int Quantity { get; set; } = 1;
        public string? ScryfallSet { get; set; }          
        public string? ScryfallCollectorNumber { get; set; } 
        public decimal? LastPriceUsd { get; set; }
        public DateTime? LastPriceCheckedAt { get; set; }
    }
}
