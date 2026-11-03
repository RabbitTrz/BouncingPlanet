using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    [CreateAssetMenu(menuName = "BouncingPlanet/Level Database", fileName = "LevelDatabase")]
    public sealed class LevelDatabase : ScriptableObject
    {
        public List<LevelConfig> levels = new List<LevelConfig>();
    }
}
