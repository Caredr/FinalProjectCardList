using System;
using System.Linq;

namespace FinalProjectCardList.Core.TelegramBot.Dto
{
    // Расширяет ToDoListCallbackDto номером страницы для постраничного вывода.
    // Формат: "action|toDoListId|page"
    public class PagedListCallbackDto : ToDoListCallbackDto
    {
        public int Page { get; set; }

        public PagedListCallbackDto() { }

        public PagedListCallbackDto(string action, Guid toDoListId, int page)
        {
            Action = action;
            ToDoListId = toDoListId;
            Page = page;
        }

        public override string ToString() => $"{Action}|{ToDoListId}|{Page}";

        public static new PagedListCallbackDto FromString(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            var parts = input.Split('|');
            var page = 0;
            var baseInput = input;

            // Если есть третья часть — это номер страницы, остальное — база "action|id"
            if (parts.Length >= 3)
            {
                int.TryParse(parts[^1], out page);
                baseInput = string.Join('|', parts[..^1]);
            }

            var baseDto = ToDoListCallbackDto.FromString(baseInput);
            return new PagedListCallbackDto(baseDto.Action, baseDto.ToDoListId, page);
        }
    }
}
