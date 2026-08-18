using System;

namespace FinalProjectCardList.Core.TelegramBot.Dto
{
    public class ToDoItemCallbackDto : CallbackDto
    {
        public string Action { get; set; } = string.Empty;
        public Guid ToDoItemId { get; set; }

        // Формат: "action|toDoItemId"
        public override string ToString()
        {
            return $"{Action}|{ToDoItemId}";
        }

        public static new ToDoItemCallbackDto FromString(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            var separatorIndex = input.IndexOf('|');
            if (separatorIndex < 0)
            {
                return new ToDoItemCallbackDto
                {
                    Action = input
                };
            }

            var action = input[..separatorIndex];
            var idPart = input[(separatorIndex + 1)..];
            var ok = Guid.TryParse(idPart, out var id);

            return new ToDoItemCallbackDto
            {
                Action = action,
                ToDoItemId = ok ? id : Guid.Empty
            };
        }
    }
}
