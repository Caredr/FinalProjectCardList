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
        public string? CurrentStep { get; set; } // Switch/case по шагам диалога, Определяет, что спрашивать и что ожидать
        public Dictionary<string, object> Data {  get; set; } // "Временные данные МЕЖДУ шагами",
                                                              // Хранение между шагами (название задачи на шаге 1 → используется на шаге 3), Полиморфизм: разные типы данных
                                                              //  Изоляция: каждый пользователь = свой Dictionary
        public ToDoUser? Context { get; set; } // "Кто ведет диалог?"
        public DateTime CreatedAt { get; }

    }
}

/* Это DTO (Data Transfer Object) — упаковка состояния диалога между сообщениями Telegram бота. 
 * "Память FSM" для хранения:
Какой сценарий выполняется
На каком шаге диалога
Временные данные между шагами
Контекст пользователя */
