using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 静态 Sprite 状态集合。美术和策划可在 Inspector 中增加/调整状态、帧序列和运动曲线。
    /// </summary>
    [CreateAssetMenu(menuName = "BouncingPlanet/Animation/Sprite State Profile", fileName = "SpriteStateProfile")]
    public sealed class SpriteStateProfile : ScriptableObject
    {
        public string defaultState = "Idle";
        public List<SpriteStateDefinition> states = new List<SpriteStateDefinition>();

        [System.NonSerialized] private Dictionary<string, SpriteStateDefinition> lookup;

        public SpriteStateDefinition Find(string stateName)
        {
            BuildLookup();
            if (string.IsNullOrEmpty(stateName))
                return DefaultState;
            lookup.TryGetValue(stateName, out SpriteStateDefinition state);
            return state;
        }

        public SpriteStateDefinition DefaultState
        {
            get
            {
                BuildLookup();
                if (lookup.TryGetValue(defaultState, out SpriteStateDefinition state))
                    return state;
                return states.Count > 0 ? states[0] : null;
            }
        }

        public void AddState(SpriteStateDefinition state)
        {
            if (state == null)
                return;
            states.Add(state);
            lookup = null;
        }

        public void RebuildLookup()
        {
            lookup = null;
            BuildLookup();
        }

        private void BuildLookup()
        {
            if (lookup != null)
                return;
            lookup = new Dictionary<string, SpriteStateDefinition>();
            for (int i = 0; i < states.Count; i++)
            {
                SpriteStateDefinition state = states[i];
                if (state == null || string.IsNullOrEmpty(state.stateName))
                    continue;
                lookup[state.stateName] = state;
            }
        }

        private void OnValidate()
        {
            lookup = null;
        }
    }
}
