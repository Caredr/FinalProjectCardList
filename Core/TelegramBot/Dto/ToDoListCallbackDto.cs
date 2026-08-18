using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.TelegramBot.Dto
{
    public class ToDoListCallbackDto : CallbackDto // Класс наследует CallbackDto. Используется для передачи данных через кнопки inline-клавиатуры Telegram.
    {
        public string Action { get; set; } = string.Empty; //Название действия — например "show", "addtask_list", "deletetask_item". По нему UpdateHandler понимает что делать при нажатии кнопки.
        public Guid ToDoListId { get; set; } //Id списка или задачи. Переиспользуется для хранения как listId, так и taskId — в зависимости от Action.
        public static string ToString(ToDoListCallbackDto dto) //Статический метод — преобразует объект в строку для записи в callbackData кнопки. Telegram принимает максимум 64 символа.
        {
            // Формат: "action|todoListId"
            ArgumentNullException.ThrowIfNull(dto);
            return $"{dto.Action}|{dto.ToDoListId}";
        }
        public static ToDoListCallbackDto FromString(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));
            var separatorIndex = input.IndexOf('|');
            if (separatorIndex < 0)
            {
                return new ToDoListCallbackDto
                {
                    Action = input
                };
            }
            var action = input[..separatorIndex];
            var idPart = input[(separatorIndex + 1)..];
            var ok = Guid.TryParse(idPart, out var id);
            return new ToDoListCallbackDto
            {
                Action = action,
                ToDoListId = ok ? id : Guid.Empty
            };
        }
    }
}
