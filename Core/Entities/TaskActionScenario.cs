using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    internal enum TaskActionScenario
    {
        Show,
        Export,
        ShowCompleted,
        ShowTask,
        DeleteTask,
        CompleteTask,
        AddTaskList,
        DeleteList,
        DeleteTaskList,
        DeleteTaskItem,
        PostponeTask,
        PostponeList,
    }
}
