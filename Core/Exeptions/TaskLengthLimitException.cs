using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Exeptions
{
    public class TaskLengthLimitException(int taskLength, int taskLengthLimit) : Exception($"Превышенно максимальная длина карты {taskLength}, лимит {taskLengthLimit}");
}
