using FinalProjectCardList.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.TelegramBot.Scenaries
{
    public class ScenarioContext
    {
        public ScenarioContext(ScenarioType scenario)
        {
            CurrentScenario = scenario;
            Data = new Dictionary<string, object>();
            CurrentStep = null;
            Context = null;
            CreatedAt = DateTime.UtcNow;
        }
        public ScenarioType CurrentScenario {  get; set; }
        public string? CurrentStep { get; set; } 
        public Dictionary<string, object> Data {  get; set; } 
        public ToDoUser? Context { get; set; }
        public DateTime CreatedAt { get; }

    }
}
