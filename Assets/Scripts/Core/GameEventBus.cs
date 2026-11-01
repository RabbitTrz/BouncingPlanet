using System;
using System.Collections.Generic;

namespace BouncingPlanet
{
    /// <summary>
    /// 轻量事件队列：仿真步进中只入队，帧末统一派发给表现层。
    /// </summary>
    public sealed class GameEventBus
    {
        private readonly List<GameEvent> pending = new List<GameEvent>(64);

        public int PendingCount => pending.Count;

        public void Emit(GameEvent gameEvent)
        {
            pending.Add(gameEvent);
        }

        public void Flush(Action<GameEvent> handler)
        {
            if (handler == null)
            {
                pending.Clear();
                return;
            }

            int count = pending.Count;
            for (int i = 0; i < count; i++)
                handler(pending[i]);

            // 处理期间如果又有事件入队，保留到下一次 Flush，避免修改正在遍历的区间。
            if (pending.Count == count)
                pending.Clear();
            else
                pending.RemoveRange(0, count);
        }

        public void Clear()
        {
            pending.Clear();
        }
    }
}
