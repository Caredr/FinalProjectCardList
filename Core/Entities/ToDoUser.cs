using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Entities
{
    public class ToDoUser // это сущность (entity) для пользователя в ToDo-приложении. 
                          // Хранит информацию о пользователе, включая его уникальный идентификатор, имя пользователя в Telegram, дату регистрации и идентификатор пользователя в Telegram. 
                          // Эта информация может использоваться для управления доступом к задачам и спискам задач, а также для персонализации опыта пользователя.
    {
        public Guid UserId { get;  set; } // Уникальный идентификатор пользователя (GUID)
        public string TelegramUserName { get; set; } // Имя пользователя в Telegram (опционально, может быть пустым)
        public DateTime RegisteredAt { get; set; }
        public long TelegramUserId { get; set; } // Идентификатор пользователя в Telegram (опционально, может быть 0 или отрицательным, если не привязан к Telegram)
    }
}
