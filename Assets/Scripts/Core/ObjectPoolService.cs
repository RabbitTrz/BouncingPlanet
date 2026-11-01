using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// Stage 0 的通用 GameObject 池。后续行星视图、粒子、流星、炸弹都复用它。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectPoolService : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Stack<GameObject>> pools =
            new Dictionary<GameObject, Stack<GameObject>>();

        private Transform poolRoot;

        public void Initialize()
        {
            if (poolRoot != null)
                return;

            GameObject root = new GameObject("PooledObjects");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[ObjectPool] Prefab is null.");
                return null;
            }

            if (!pools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                pools.Add(prefab, stack);
            }

            GameObject instance;
            if (stack.Count > 0)
            {
                instance = stack.Pop();
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab);
                PooledObject marker = instance.GetComponent<PooledObject>();
                if (marker == null)
                    marker = instance.AddComponent<PooledObject>();
                marker.OriginPrefab = prefab;
            }

            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled != null)
                pooled.IsInPool = false;

            Transform t = instance.transform;
            if (parent != null)
                t.SetParent(parent, false);
            else
                t.SetParent(null, false);

            t.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (prefab == null)
                return null;
            GameObject instance = Spawn(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            PooledObject marker = instance.GetComponent<PooledObject>();
            if (marker == null || marker.OriginPrefab == null)
            {
                Destroy(instance);
                return;
            }

            if (marker.IsInPool)
                return;

            marker.IsInPool = true;
            instance.SetActive(false);

            if (poolRoot == null)
                Initialize();
            instance.transform.SetParent(poolRoot, false);

            if (!pools.TryGetValue(marker.OriginPrefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                pools.Add(marker.OriginPrefab, stack);
            }
            stack.Push(instance);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
                return;

            if (!pools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                pools.Add(prefab, stack);
            }

            for (int i = 0; i < count; i++)
            {
                GameObject instance = Instantiate(prefab);
                PooledObject marker = instance.GetComponent<PooledObject>();
                if (marker == null)
                    marker = instance.AddComponent<PooledObject>();
                marker.OriginPrefab = prefab;
                marker.IsInPool = true;
                instance.SetActive(false);
                if (poolRoot == null)
                    Initialize();
                instance.transform.SetParent(poolRoot, false);
                stack.Push(instance);
            }
        }

        public void ClearAll()
        {
            foreach (KeyValuePair<GameObject, Stack<GameObject>> pair in pools)
            {
                while (pair.Value.Count > 0)
                {
                    GameObject instance = pair.Value.Pop();
                    if (instance != null)
                        Destroy(instance);
                }
            }
            pools.Clear();
        }
    }
}
