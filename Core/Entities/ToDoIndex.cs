using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    internal class ToDoIndex 
    {
        public Dictionary<Guid, Guid> ItemToUserMap { get; set; } = new();
    }
}
