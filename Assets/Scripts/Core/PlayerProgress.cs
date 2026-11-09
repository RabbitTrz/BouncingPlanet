using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 关卡进度：1 → 2 → 3 → 无尽 顺序解锁，通关后写入 PlayerPrefs。
    /// 保存机制与换装一致：桌面端在注册表/plist，WebGL 在浏览器 IndexedDB。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerProgress : MonoBehaviour
    {
        private const string KeyPrefix = "BouncingPlanet.Progress.Level";
        private const int LevelCount = 3;

        public static PlayerProgress Instance { get; private set; }

        public event Action Changed;

        private readonly bool[] completed = new bool[LevelCount];
        private bool initialized;

        public void Initialize(GameBootstrap bootstrap)
        {
            if (initialized)
                return;
            initialized = true;
            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsCompleted(int order)
        {
            if (order < 1 || order > LevelCount)
                return false;
            return completed[order - 1];
        }

        /// <summary>第 1 关默认解锁；2/3 关要上一关通关；无尽要三关全通。</summary>
        public bool IsLevelUnlocked(LevelConfig level)
        {
            if (level == null)
                return false;
            if (level.IsEndless)
                return IsCompleted(1) && IsCompleted(2) && IsCompleted(3);
            if (level.order <= 1)
                return true;
            return IsCompleted(level.order - 1);
        }

        public void CompleteLevel(LevelConfig level)
        {
            if (level == null || level.IsEndless)
                return;
            int order = level.order;
            if (order < 1 || order > LevelCount || completed[order - 1])
                return;

            completed[order - 1] = true;
            PlayerPrefs.SetInt(Key(order), 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            Debug.Log("[Progress] 第 " + order + " 关已通关。");
        }

        [ContextMenu("Unlock All (F9)")]
        public void UnlockAll()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                completed[i] = true;
                PlayerPrefs.SetInt(Key(i + 1), 1);
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
            Debug.Log("[Progress] 已解锁全部关卡。");
        }

        [ContextMenu("Reset Progress (Shift+R)")]
        public void ResetProgress()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                completed[i] = false;
                PlayerPrefs.DeleteKey(Key(i + 1));
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
            Debug.Log("[Progress] 关卡进度已重置。");
        }

        private void Update()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) ||
                         Input.GetKey(KeyCode.RightShift);
            if (shift && Input.GetKeyDown(KeyCode.R))
                ResetProgress();
            if (Input.GetKeyDown(KeyCode.F9))
                UnlockAll();
        }

        private void Load()
        {
            for (int i = 0; i < LevelCount; i++)
                completed[i] = PlayerPrefs.GetInt(Key(i + 1), 0) != 0;
        }

        private static string Key(int order)
        {
            return KeyPrefix + order + "Complete";
        }
    }
}
