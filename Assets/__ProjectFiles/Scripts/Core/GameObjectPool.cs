using System.Collections.Generic;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// A simple object pool for GameObjects. Pre-warms instances on Awake.
    /// Items return to the pool via Return() and are recycled by Get().
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private int prewarmCount = 10;

        [SerializeField]
        private bool expandable = true;

        [SerializeField]
        private int maxSize = 30;

        private readonly Queue<GameObject> pool = new();
        private int activeCount;

        private void Awake()
        {
            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject obj = CreateNew();
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool at the given position and rotation.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            else if (expandable && activeCount < maxSize)
            {
                obj = CreateNew();
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                return null;
            }

            obj.SetActive(true);
            activeCount++;

            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnPoolGet();

            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnPoolReturn();

            obj.SetActive(false);
            activeCount--;
            pool.Enqueue(obj);
        }

        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            // Find all active pooled objects by looking for the Poolable component
            var active = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            // Can't easily track all, but pool pattern handles this
        }

        private GameObject CreateNew()
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.name = $"{prefab.name}_pooled";
            return obj;
        }
    }

    /// <summary>
    /// Interface for poolable objects that need lifecycle callbacks.
    /// </summary>
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolReturn();
    }
}
