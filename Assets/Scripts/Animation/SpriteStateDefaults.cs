using System.Collections.Generic;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 运行时占位动画配置。美术可以创建自己的 SpriteStateProfile 资产并覆盖，
    /// 这里的曲线只是让程序占位图也有基本状态运动。
    /// </summary>
    public static class SpriteStateDefaults
    {
        private static SpriteStateProfile playerProfile;
        private static readonly Dictionary<PlanetTypeDef, SpriteStateProfile> planetProfiles =
            new Dictionary<PlanetTypeDef, SpriteStateProfile>();
        private static readonly Dictionary<PlanetTypeDef, SpriteStateProfile> fallbackProfiles =
            new Dictionary<PlanetTypeDef, SpriteStateProfile>();

        public static SpriteStateProfile GetPlayerProfile()
        {
            if (playerProfile == null)
            {
                playerProfile = Resources.Load<SpriteStateProfile>(
                    "Animation/PlayerAnimationProfile");
                if (playerProfile == null)
                {
                    Debug.LogWarning("[SpriteStateDefaults] PlayerAnimationProfile 缺失，使用程序化 fallback。");
                    playerProfile = CreatePlayerProfile();
                }
            }
            return playerProfile;
        }

        public static SpriteStateProfile GetPlanetProfile(PlanetTypeDef def)
        {
            if (def == null)
                return null;

            bool hasExplicitStates = def.animationStates != null &&
                                     def.animationStates.HasIdleFrames;

            // 显式状态可能在运行时被填/清空；缓存只在类型匹配时复用。
            if (planetProfiles.TryGetValue(def, out SpriteStateProfile cached) &&
                cached != null && cached.name.StartsWith("Runtime_PlanetStates_"))
            {
                if (hasExplicitStates)
                    return cached;
                planetProfiles.Remove(def);
            }

            if (hasExplicitStates)
            {
                SpriteStateProfile fromStates = CreateProfileFromAnimationStates(def);
                planetProfiles[def] = fromStates;
                return fromStates;
            }

            // 直接引用的 Profile 不缓存，便于在 Play Mode 里即时更换；
            // 但如果美术 Profile 缺少可用的 Shattered 状态，套一层运行时副本补上程序碎裂动画。
            if (def.visualProfile != null)
            {
                if (HasUsableShatter(def.visualProfile))
                    return def.visualProfile;
                return GetFallbackProfile(def, def.visualProfile);
            }

            if (planetProfiles.TryGetValue(def, out cached) && cached != null)
                return cached;

            SpriteStateProfile profile = CreatePlanetProfile(def);
            planetProfiles[def] = profile;
            return profile;
        }

        private static SpriteStateProfile CreateProfileFromAnimationStates(PlanetTypeDef def)
        {
            PlanetAnimationSet set = def.animationStates;
            set.EnsureDefaults();

            SpriteStateProfile profile = ScriptableObject.CreateInstance<SpriteStateProfile>();
            profile.name = "Runtime_PlanetStates_" + def.name;
            profile.hideFlags = HideFlags.DontSave;
            profile.defaultState = "Idle";
            profile.AddState(CreateIdleState(def, set.idle));
            if (set.hit != null)
                profile.AddState(set.hit);
            profile.AddState(IsShatterUsable(set.shattered)
                ? set.shattered
                : CreateShatterFallback(def));
            return profile;
        }

        /// <summary>
        /// 开启待机摆动时，复制一份 Idle 状态并清掉呼吸缩放曲线；
        /// 摆动本身由 SpriteStateMachine 叠加，不修改美术资产里的原始曲线。
        /// </summary>
        private static SpriteStateDefinition CreateIdleState(
            PlanetTypeDef def, SpriteStateDefinition source)
        {
            if (source == null || def == null || !def.idleSwayEnabled)
                return source;

            return new SpriteStateDefinition
            {
                stateName = source.stateName,
                frames = source.frames,
                fps = source.fps,
                loop = source.loop,
                autoReturn = source.autoReturn,
                durationOverride = source.durationOverride,
                baseOffset = source.baseOffset,
                baseScale = source.baseScale,
                baseRotation = source.baseRotation,
                offsetX = source.offsetX,
                offsetY = source.offsetY,
                scaleX = AnimationCurve.Constant(0f, 1f, 1f),
                scaleY = AnimationCurve.Constant(0f, 1f, 1f),
                rotation = AnimationCurve.Constant(0f, 1f, 0f)
            };
        }

        /// <summary>
        /// 美术 Profile 里没有 Shattered（或只有空状态）时，创建一个运行时副本，
        /// 保留 Idle/Hit 的美术配置，并补上原有的缩小+旋转消失动画。
        /// </summary>
        private static SpriteStateProfile GetFallbackProfile(PlanetTypeDef def,
            SpriteStateProfile source)
        {
            string runtimeName = "Runtime_PlanetFallback_" + source.name;
            if (fallbackProfiles.TryGetValue(def, out SpriteStateProfile cached) &&
                cached != null && cached.name == runtimeName)
            {
                return cached;
            }

            SpriteStateProfile profile = ScriptableObject.CreateInstance<SpriteStateProfile>();
            profile.name = runtimeName;
            profile.hideFlags = HideFlags.DontSave;
            profile.defaultState = source.defaultState;

            if (source.states != null)
            {
                for (int i = 0; i < source.states.Count; i++)
                {
                    SpriteStateDefinition state = source.states[i];
                    if (state == null || state.stateName == "Shattered")
                        continue;
                    profile.AddState(state);
                }
            }

            profile.AddState(CreateShatterFallback(def));
            fallbackProfiles[def] = profile;
            return profile;
        }

        private static SpriteStateDefinition CreateShatterFallback(PlanetTypeDef def)
        {
            Sprite crash = def != null ? def.crashSprite : null;
            return new SpriteStateDefinition
            {
                stateName = "Shattered",
                loop = false,
                autoReturn = false,
                durationOverride = 0.28f,
                frames = crash != null ? new[] { crash } : null,
                baseScale = Vector2.one,
                scaleX = Curve(0f, 1f, 1f, 0.1f),
                scaleY = Curve(0f, 1f, 1f, 0.1f),
                rotation = Curve(0f, 0f, 1f, 30f)
            };
        }

        private static bool HasUsableShatter(SpriteStateProfile profile)
        {
            if (profile == null)
                return false;
            return IsShatterUsable(profile.Find("Shattered"));
        }

        private static bool IsShatterUsable(SpriteStateDefinition state)
        {
            if (state == null)
                return false;
            if (state.HasFrames)
                return true;

            // 没有帧时，只有配置了非恒定运动曲线才算美术真的做了碎裂表现；
            // 否则用程序 Fallback 补缩小/旋转。
            return CurveMoves(state.scaleX) || CurveMoves(state.scaleY) ||
                   CurveMoves(state.rotation) ||
                   CurveMoves(state.offsetX) || CurveMoves(state.offsetY);
        }

        private static bool CurveMoves(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return false;

            float first = curve.Evaluate(0f);
            for (int i = 0; i < curve.length; i++)
            {
                if (!Mathf.Approximately(curve.keys[i].value, first))
                    return true;
            }
            return false;
        }

        private static SpriteStateProfile CreatePlayerProfile()
        {
            SpriteStateProfile profile = ScriptableObject.CreateInstance<SpriteStateProfile>();
            profile.name = "Runtime_PlayerSpriteStates";
            profile.hideFlags = HideFlags.DontSave;
            profile.defaultState = "happy";

            profile.AddState(new SpriteStateDefinition
            {
                stateName = "happy",
                loop = true,
                autoReturn = false,
                durationOverride = 1.2f,
                scaleX = Curve(0f, 1f, 0.5f, 1.04f, 1f, 1f),
                scaleY = Curve(0f, 1f, 0.5f, 0.97f, 1f, 1f),
                offsetY = Curve(0f, 0f, 0.5f, 0.006f, 1f, 0f)
            });
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "up",
                loop = false,
                autoReturn = true,
                durationOverride = 0.35f,
                baseScale = new Vector2(0.96f, 1.08f),
                scaleX = Curve(0f, 1f, 0.5f, 0.98f, 1f, 1f),
                scaleY = Curve(0f, 1f, 0.5f, 1.03f, 1f, 1f)
            });
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "down",
                loop = false,
                autoReturn = true,
                durationOverride = 0.35f,
                baseScale = new Vector2(1.06f, 0.94f),
                scaleX = Curve(0f, 1f, 0.5f, 1.02f, 1f, 1f),
                scaleY = Curve(0f, 1f, 0.5f, 0.98f, 1f, 1f)
            });
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "overdrive",
                loop = true,
                autoReturn = false,
                durationOverride = 0.24f,
                baseScale = new Vector2(1.10f, 1.10f),
                scaleX = Curve(0f, 1f, 0.5f, 1.10f, 1f, 1f),
                scaleY = Curve(0f, 1f, 0.5f, 0.94f, 1f, 1f),
                rotation = Curve(0f, -4f, 0.5f, 4f, 1f, -4f)
            });
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "dead",
                loop = false,
                autoReturn = false,
                durationOverride = 0.5f,
                scaleX = Curve(0f, 1f, 1f, 0.05f),
                scaleY = Curve(0f, 1f, 1f, 0.05f),
                rotation = Curve(0f, 0f, 1f, 45f)
            });
            return profile;
        }

        private static SpriteStateProfile CreatePlanetProfile(PlanetTypeDef def)
        {
            SpriteStateProfile profile = ScriptableObject.CreateInstance<SpriteStateProfile>();
            profile.name = "Runtime_PlanetSpriteStates";
            profile.hideFlags = HideFlags.DontSave;
            profile.defaultState = "Idle";

            bool useSway = def != null && def.idleSwayEnabled;
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "Idle",
                loop = true,
                durationOverride = 1.5f,
                scaleX = useSway
                    ? AnimationCurve.Constant(0f, 1f, 1f)
                    : Curve(0f, 1f, 0.5f, 1.025f, 1f, 1f),
                scaleY = useSway
                    ? AnimationCurve.Constant(0f, 1f, 1f)
                    : Curve(0f, 1f, 0.5f, 0.985f, 1f, 1f)
            });
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "Hit",
                loop = false,
                autoReturn = true,
                durationOverride = 0.18f,
                scaleX = Curve(0f, 1f, 0.35f, 1.16f, 0.7f, 0.92f, 1f, 1f),
                scaleY = Curve(0f, 1f, 0.35f, 0.78f, 0.7f, 1.07f, 1f, 1f)
            });

            Sprite crash = def != null ? def.crashSprite : null;
            profile.AddState(new SpriteStateDefinition
            {
                stateName = "Shattered",
                loop = false,
                autoReturn = false,
                durationOverride = 0.28f,
                frames = crash != null ? new[] { crash } : null,
                scaleX = Curve(0f, 1f, 1f, 0.1f),
                scaleY = Curve(0f, 1f, 1f, 0.1f),
                rotation = Curve(0f, 0f, 1f, 30f)
            });

            return profile;
        }

        private static AnimationCurve Curve(params float[] keys)
        {
            Keyframe[] frames = new Keyframe[keys.Length / 2];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = new Keyframe(keys[i * 2], keys[i * 2 + 1]);
            return new AnimationCurve(frames);
        }
    }
}
