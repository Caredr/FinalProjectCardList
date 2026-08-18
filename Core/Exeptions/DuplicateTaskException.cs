using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalProjectCardList.Core.Exeptions
{
    //Ошибка на проверку сущетсования
    public class DuplicateTaskException(string task) : Exception($"Такая {task} уже существует");
}
