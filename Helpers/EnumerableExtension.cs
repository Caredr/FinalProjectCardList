using System.Collections.Generic;
using System.Linq;

namespace FinalProjectCardList.Helpers
{
    public static class EnumerableExtension
    {
        // Возвращает подмножество элементов по размеру пакета и его (нулевому) номеру.
        // Пример: [1..10], batchSize=3, batchNumber=1 → [4,5,6]
        public static IEnumerable<T> GetBatchByNumber<T>(this IEnumerable<T> source, int batchSize, int batchNumber)
        {
            return source.Skip(batchSize * batchNumber).Take(batchSize);
        }
    }
}
