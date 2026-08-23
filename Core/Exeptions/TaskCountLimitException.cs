using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Exeptions
{
    public class TaskCountLimitException(int count) : Exception($"Превышенно максимальное количество карт{count}");
}
