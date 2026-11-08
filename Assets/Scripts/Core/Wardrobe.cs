using System;
using UnityEngine;

namespace BouncingPlanet
{
    public enum ClothingCategory
    {
        Hair,
        Cloth,
        Decoration
    }

    /// <summary>
    /// 换装数据：三类部件（头发/衣服/装饰）各 3 件，
    /// 负责从 Image 下加载规范化后的分层素材、持久化选择并广播变化。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Wardrobe : MonoBehaviour
    {
        public const int OptionCount = 3;
        public const int LayerCount = 7;

        private const string PartRoot = "Image/Player/Parts/";
        private const string StickerRoot = "Image/Clothes/";

        private const string HairPref = "BouncingPlanet.Wardrobe.Hair";
        private const string ClothPref = "BouncingPlanet.Wardrobe.Cloth";
        private const string DecorationPref =
            "BouncingPlanet.Wardrobe.Decoration";

        public static Wardrobe Instance { get; private set; }

        public event Action Changed;

        public int HairIndex { get; private set; }
        public int ClothIndex { get; private set; }
        public int DecorationIndex { get; private set; }

        private Sprite paper;
        private Sprite bodyNormal;
        private Sprite bodyDown;
        private Sprite bodyOverdrive;
        private Sprite faceHappy;
        private Sprite faceDown;
        private Sprite faceUp;
        private Sprite faceOverdrive;
        private Sprite handHappy;
        private Sprite handDown;
        private Sprite handUp;
        private Sprite handOverdrive;
        private Sprite[] hair;
        private Sprite[] cloth;
        private Sprite[] decoration;
        private Sprite[] hairStickers;
        private Sprite[] clothStickers;
        private Sprite[] decorationStickers;
        private bool initialized;

        public void Initialize(GameBootstrap bootstrap)
        {
            if (initialized)
                return;
            initialized = true;
            Instance = this;

            paper = Load("Paper");
            bodyNormal = Load("Body_Normal");
            bodyDown = Load("Body_Down");
            bodyOverdrive = Load("Body_Overdrive");
            faceHappy = Load("Face_Happy");
            faceDown = Load("Face_Down");
            faceUp = Load("Face_Up");
            faceOverdrive = Load("Face_Overdrive");
            handHappy = Load("Hand_Happy");
            handDown = Load("Hand_Down");
            handUp = Load("Hand_Up");
            handOverdrive = Load("Hand_Overdrive");

            hair = LoadSet("Hair_");
            cloth = LoadSet("Cloth_");
            decoration = LoadSet("Decoration_");
            hairStickers = LoadStickers("Hair_");
            clothStickers = LoadStickers("Cloth_");
            decorationStickers = LoadStickers("Decoration_");

            HairIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(HairPref, 0), 0, OptionCount - 1);
            ClothIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(ClothPref, 0), 0, OptionCount - 1);
            DecorationIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(DecorationPref, 0), 0, OptionCount - 1);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int GetIndex(ClothingCategory category)
        {
            switch (category)
            {
                case ClothingCategory.Hair:
                    return HairIndex;
                case ClothingCategory.Cloth:
                    return ClothIndex;
                default:
                    return DecorationIndex;
            }
        }

        public void Equip(ClothingCategory category, int index)
        {
            index = Mathf.Clamp(index, 0, OptionCount - 1);
            switch (category)
            {
                case ClothingCategory.Hair:
                    if (HairIndex == index)
                        return;
                    HairIndex = index;
                    PlayerPrefs.SetInt(HairPref, index);
                    break;
                case ClothingCategory.Cloth:
                    if (ClothIndex == index)
                        return;
                    ClothIndex = index;
                    PlayerPrefs.SetInt(ClothPref, index);
                    break;
                default:
                    if (DecorationIndex == index)
                        return;
                    DecorationIndex = index;
                    PlayerPrefs.SetInt(DecorationPref, index);
                    break;
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public Sprite GetSticker(ClothingCategory category, int index)
        {
            index = Mathf.Clamp(index, 0, OptionCount - 1);
            Sprite[] set = category == ClothingCategory.Hair
                ? hairStickers
                : category == ClothingCategory.Cloth
                    ? clothStickers
                    : decorationStickers;
            return set != null && index < set.Length ? set[index] : null;
        }

        /// <summary>返回从底到顶的 7 层：paper/body/face/hair/cloth/decoration/hand。</summary>
        public Sprite[] GetLayers(string stateName)
        {
            string state = NormalizeState(stateName);
            Sprite body = state == "down"
                ? bodyDown
                : state == "overdrive"
                    ? bodyOverdrive
                    : bodyNormal;
            Sprite face = state == "down"
                ? faceDown
                : state == "up"
                    ? faceUp
                    : state == "overdrive"
                        ? faceOverdrive
                        : faceHappy;
            Sprite hand = state == "down"
                ? handDown
                : state == "up"
                    ? handUp
                    : state == "overdrive"
                        ? handOverdrive
                        : handHappy;

            return new[]
            {
                paper,
                body,
                face,
                Get(hair, HairIndex),
                Get(cloth, ClothIndex),
                Get(decoration, DecorationIndex),
                hand
            };
        }

        public static string NormalizeState(string stateName)
        {
            switch (stateName)
            {
                case "override":
                    return "overdrive";
                case "dead":
                    return "happy";
                case "happy":
                case "down":
                case "up":
                case "overdrive":
                    return stateName;
                default:
                    return "happy";
            }
        }

        private Sprite[] LoadSet(string prefix)
        {
            Sprite[] set = new Sprite[OptionCount];
            for (int i = 0; i < OptionCount; i++)
                set[i] = Load(prefix + (i + 1));
            return set;
        }

        private Sprite[] LoadStickers(string prefix)
        {
            Sprite[] set = new Sprite[OptionCount];
            for (int i = 0; i < OptionCount; i++)
                set[i] = LoadSticker(prefix + (i + 1));
            return set;
        }

        private Sprite Load(string name)
        {
            return Resources.Load<Sprite>(PartRoot + name);
        }

        private Sprite LoadSticker(string name)
        {
            return Resources.Load<Sprite>(StickerRoot + name);
        }

        private static Sprite Get(Sprite[] set, int index)
        {
            if (set == null || set.Length == 0)
                return null;
            return set[Mathf.Clamp(index, 0, set.Length - 1)];
        }
    }
}
