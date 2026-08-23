using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    public class ScryfallCard
    {
        public string Name { get; set; } = string.Empty;
        public string Set { get; set; } = string.Empty;
        public string CollectorNumber { get; set; } = string.Empty;
        public decimal? Usd { get; set; }
        public decimal? UsdFoil { get; set; }
        public decimal? Eur { get; set; }
        public decimal? Tix { get; set; }
    }
}
