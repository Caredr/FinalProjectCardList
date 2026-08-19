using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    internal class ToDoItem // это сущность (entity) для отдельной задачи в ToDo-приложении.
                            // Хранит полную информацию об одной задаче пользователя.
    {
        // Уникальный идентификатор задачи (PRIMARY KEY в таблице ToDoItem)
        public Guid Id { get; set; }

        // Внешний ключ на пользователя (колонка UserId в таблице ToDoItem)
        public Guid UserId { get; set; }

        // Внешний ключ на список (колонка ListId в таблице ToDoItem, может быть null)
        public Guid? ListId { get; set; }

        // Навигационные свойства (для удобства в коде, не для хранения в FK)
        public ToDoUser User { get; set; }       // владелец задачи
        public ToDoList? List { get; set; }      // список, к которому задача привязана

        public string Name { get; set; }         // название/описание задачи
        public DateTime CreatedAt { get; set; }  // время создания (UTC)
        public ToDoItemState State { get; set; } // состояние задачи
        public DateTime? StateChangedAt { get; set; }
        public DateTime? Deadline { get; set; }  // дедлайн (опционально)
        public int Quantity { get; set; } = 1;   // количество (копий карты и т.п.)
    }
}
