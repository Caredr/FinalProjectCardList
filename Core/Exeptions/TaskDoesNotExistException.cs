using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Exeptions
{
    //Ошибка на проверку существования тасок
    internal class TaskDoesNotExistException(string description) : Exception(description);
}
