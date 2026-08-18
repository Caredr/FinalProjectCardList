using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.TelegramBot.Dto
{
    public class CallbackDto
    {
        public string Action { get; set; } = string.Empty;
        public static CallbackDto FromString(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));
            // ищем первый |
            var separatorIndex = input.IndexOf('|');
            // если | нет — вся строка = Action
            if (separatorIndex < 0)
            {
                return new CallbackDto
                {
                    Action = input
                };
            }
            // до первого | — это action
            var action = input.Substring(0, separatorIndex);
            return new CallbackDto
            {
                Action = action
            };
        }
    }
}
