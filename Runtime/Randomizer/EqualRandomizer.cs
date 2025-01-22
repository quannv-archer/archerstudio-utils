using System.Collections.Generic;
using System.Linq;

namespace ArcherStudio.Utils
{
    public class EqualRandomizer<T>
    {
        private List<T> originalItems;
        private List<T> currentBag;
        private System.Random random;

        public EqualRandomizer(IEnumerable<T> items, int? seed = null)
        {
            originalItems = items.ToList();
            currentBag = new List<T>();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public T GetNext()
        {
            if (currentBag.Count == 0)
            {
                RefillBag();
            }

            int index = random.Next(currentBag.Count);
            T item = currentBag[index];
            currentBag.RemoveAt(index);

            return item;
        }

        private void RefillBag()
        {
            currentBag = new List<T>(originalItems);

            int n = currentBag.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (currentBag[k], currentBag[n]) = (currentBag[n], currentBag[k]);
            }
        }

        public int RemainingCount()
        {
            return currentBag.Count;
        }

        public List<T> GetRemainingItems()
        {
            return new List<T>(currentBag);
        }

        public void Reset()
        {
            currentBag.Clear();
        }
    }
}