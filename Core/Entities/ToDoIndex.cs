using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    internal class ToDoIndex // Класс ToDoIndex представляет индекс, который связывает задачи (ToDoItem) с пользователями (ToDoUser).
                             // Он содержит словарь, который отображает идентификаторы задач на идентификаторы пользователей. Этот индекс может использоваться для быстрого поиска задач, принадлежащих определенному пользователю, без необходимости проходить через все задачи.
    {
        // Словарь, который отображает идентификаторы задач (Guid) на идентификаторы пользователей (Guid).
        // Ключом является идентификатор задачи, а значением - идентификатор пользователя, которому принадлежит эта задача.
        public Dictionary<Guid, Guid> ItemToUserMap { get; set; } = new();
    }
}
