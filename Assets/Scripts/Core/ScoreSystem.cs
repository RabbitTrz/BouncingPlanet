using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 分数接口。v1.1 只累计/显示；未来无双会复用同一套分数作为入口。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreSystem : MonoBehaviour
    {
        public int Score { get; private set; }
        public event Action<int, int> Changed;

        public void Add(int amount)
        {
            if (amount == 0)
                return;
            Score += amount;
            Changed?.Invoke(Score, amount);
        }

        public void ResetScore()
        {
            if (Score == 0)
                return;
            int old = Score;
            Score = 0;
            Changed?.Invoke(Score, -old);
        }
    }
}
