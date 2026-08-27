using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    internal static class TaskActionScenarioExtensions
    {
        public static string ToAction(this TaskActionScenario scenario) => scenario switch
        {
            TaskActionScenario.Show => "show",
            TaskActionScenario.Export => "export",
            TaskActionScenario.ShowCompleted => "showcompleted",
            TaskActionScenario.ShowTask => "showtask",
            TaskActionScenario.DeleteTask => "deletetask",
            TaskActionScenario.CompleteTask => "completetask",
            TaskActionScenario.AddTaskList => "addtask_list",
            TaskActionScenario.DeleteList => "deletelist",
            TaskActionScenario.DeleteTaskList=> "deletetask_list",
            TaskActionScenario.DeleteTaskItem => "deletetask_item",
            TaskActionScenario.PostponeTask => "postpone_task",
            TaskActionScenario.PostponeList => "postpone_list"
        };
    }
}
