using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>对象池实例标记，记录它来自哪个预制体。</summary>
    public sealed class PooledObject : MonoBehaviour
    {
        internal GameObject OriginPrefab { get; set; }
        internal bool IsInPool { get; set; }
    }
}
