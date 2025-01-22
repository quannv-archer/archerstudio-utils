using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ArcherStudio.Utils
{
    public class WeightedRandomizer<T>
    {
        private List<T> originalItems;
        private Dictionary<T, float> weights;
        private List<T> currentBag;
        private System.Random random;

        public WeightedRandomizer(IEnumerable<T> items, Dictionary<T, float> itemWeights = null, int? seed = null)
        {
            originalItems = items.ToList();
            weights = itemWeights ?? originalItems.ToDictionary(item => item, item => 1f);
            currentBag = new List<T>();
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            foreach (var item in originalItems)
            {
                if (!weights.ContainsKey(item))
                {
                    weights[item] = 1f; // Default weight = 1
                }
            }
        }

        public T GetNext()
        {
            if (currentBag.Count == 0)
            {
                RefillBag();
            }

            float totalWeight = currentBag.Sum(item => weights[item]);
            float randomPoint = (float)(random.NextDouble() * totalWeight);

            float currentWeight = 0;
            for (int i = 0; i < currentBag.Count; i++)
            {
                currentWeight += weights[currentBag[i]];
                if (randomPoint <= currentWeight)
                {
                    T selectedItem = currentBag[i];
                    currentBag.RemoveAt(i);
                    return selectedItem;
                }
            }

            return currentBag[^1];
        }

        private void RefillBag()
        {
            currentBag = new List<T>();

            foreach (var item in originalItems)
            {
                int copies = Mathf.RoundToInt(weights[item]);
                for (int i = 0; i < copies; i++)
                {
                    currentBag.Add(item);
                }
            }

            // Shuffle bag
            for (int i = currentBag.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                (currentBag[i], currentBag[randomIndex]) = (currentBag[randomIndex], currentBag[i]);
            }
        }

        public void UpdateWeight(T item, float newWeight)
        {
            if (weights.ContainsKey(item))
            {
                weights[item] = newWeight;
            }
        }

        public void UpdateWeights(Dictionary<T, float> newWeights)
        {
            foreach (var pair in newWeights)
            {
                if (weights.ContainsKey(pair.Key))
                {
                    weights[pair.Key] = pair.Value;
                }
            }
        }

        public float GetWeight(T item)
        {
            return weights.GetValueOrDefault(item, 0f);
        }

        public void Reset()
        {
            currentBag.Clear();
        }
    }
}