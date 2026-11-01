using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 撞击反馈参数。v1.0 只先把字段和调参入口建好，Stage 2 再接入实际表现。
    /// </summary>
    [CreateAssetMenu(menuName = "BouncingPlanet/Feedback Config", fileName = "FeedbackConfig")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Header("落地 / 弹跳 Landing / Bounce")]
        [Tunable("Landing", 0f, 0.2f, 0.005f, "顿帧基础时长")]
        public float landingHitstopBase = 0.04f;

        [Tunable("Landing", 0f, 0.05f, 0.001f, "顿帧速度加成")]
        public float landingHitstopPerSpeed = 0.008f;

        [Tunable("Landing", 0f, 0.2f, 0.005f, "顿帧最大时长")]
        public float landingHitstopMax = 0.06f;

        [Tunable("Landing", 0f, 2f, 0.01f, "震屏基础强度")]
        public float landingShakeBase = 0.18f;

        [Tunable("Landing", 0f, 1f, 0.01f, "震屏速度加成")]
        public float landingShakePerSpeed = 0.09f;

        [Tunable("Landing", 0f, 2f, 0.01f, "镜头前推基础值")]
        public float landingPunchBase = 0.40f;

        [Tunable("Landing", 0f, 1f, 0.01f, "镜头前推速度加成")]
        public float landingPunchPerSpeed = 0.22f;

        [Tunable("Landing", 0f, 1f, 0.01f, "压扁基础值")]
        public float landingSquashBase = 0.55f;

        [Tunable("Landing", 0f, 1f, 0.01f, "压扁速度加成")]
        public float landingSquashPerSpeed = 0.09f;

        [Tunable("Landing", 0f, 1f, 0.01f, "压扁最大值")]
        public float landingSquashMax = 1f;

        [Header("炸弹星 Bomb Star")]
        [Tunable("Bomb", 0f, 0.3f, 0.005f, "炸弹顿帧")]
        public float bombHitstop = 0.10f;

        [Tunable("Bomb", 0f, 5f, 0.05f, "炸弹震屏")]
        public float bombShake = 2.60f;

        [Tunable("Bomb", 0f, 3f, 0.05f, "炸弹镜头前推")]
        public float bombPunch = 1.40f;

        [Tunable("Bomb", 0f, 1f, 0.01f, "炸弹压扁")]
        public float bombSquash = 1.00f;

        [Tunable("Bomb", 0f, 1f, 0.01f, "炸弹屏闪强度")]
        public float bombScreenFlash = 0.40f;

        [Header("冰 / 云 Ice / Cloud")]
        [Tunable("Ice", 0f, 0.2f, 0.005f, "冰星顿帧")]
        public float iceHitstop = 0.04f;

        [Tunable("Ice", 0f, 2f, 0.01f, "冰星震屏")]
        public float iceShake = 0.45f;

        [Tunable("Ice", 0f, 1f, 0.01f, "冰星屏闪")]
        public float iceScreenFlash = 0.10f;

        [Tunable("Cloud", 0f, 2f, 0.01f, "云朵震屏")]
        public float cloudShake = 0.40f;

        [Tunable("Cloud", 0f, 1f, 0.01f, "云朵屏闪")]
        public float cloudScreenFlash = 0.10f;

        [Header("其他 Other")]
        [Tunable("Other", 0f, 2f, 0.01f, "上升挡回震屏")]
        public float ascendBlockShake = 0.35f;

        [Tunable("Other", 0f, 1f, 0.01f, "屏闪衰减速度")]
        public float screenFlashDecay = 2.20f;

        [Header("衰减 Decay")]
        [Tunable("Decay", 0.1f, 20f, 0.1f, "压扁衰减速度")]
        public float squashDecay = 6f;

        [Tunable("Decay", 0.1f, 10f, 0.1f, "震屏衰减速度")]
        public float shakeDecay = 2.2f;

        [Tunable("Decay", 0.1f, 20f, 0.1f, "镜头前推衰减速度")]
        public float punchDecay = 5f;

        public float ComputeLandingHitstop(float impactSpeed)
        {
            return Mathf.Min(landingHitstopMax,
                landingHitstopBase + Mathf.Max(0f, impactSpeed) * landingHitstopPerSpeed);
        }

        public float ComputeLandingShake(float impactSpeed)
        {
            return Mathf.Max(0f, landingShakeBase + Mathf.Max(0f, impactSpeed) * landingShakePerSpeed);
        }

        public float ComputeLandingPunch(float impactSpeed)
        {
            return Mathf.Max(0f, landingPunchBase + Mathf.Max(0f, impactSpeed) * landingPunchPerSpeed);
        }

        public float ComputeLandingSquash(float impactSpeed)
        {
            return Mathf.Clamp(
                landingSquashBase + Mathf.Max(0f, impactSpeed) * landingSquashPerSpeed,
                0f, landingSquashMax);
        }

        [ContextMenu("Reset To Initial Demo Defaults")]
        public void ResetToDefaults()
        {
            landingHitstopBase = 0.04f;
            landingHitstopPerSpeed = 0.008f;
            landingHitstopMax = 0.06f;
            landingShakeBase = 0.18f;
            landingShakePerSpeed = 0.09f;
            landingPunchBase = 0.40f;
            landingPunchPerSpeed = 0.22f;
            landingSquashBase = 0.55f;
            landingSquashPerSpeed = 0.09f;
            landingSquashMax = 1f;

            bombHitstop = 0.10f;
            bombShake = 2.60f;
            bombPunch = 1.40f;
            bombSquash = 1.00f;
            bombScreenFlash = 0.40f;

            iceHitstop = 0.04f;
            iceShake = 0.45f;
            iceScreenFlash = 0.10f;
            cloudShake = 0.40f;
            cloudScreenFlash = 0.10f;

            ascendBlockShake = 0.35f;
            screenFlashDecay = 2.20f;
            squashDecay = 6f;
            shakeDecay = 2.2f;
            punchDecay = 5f;
        }

        private void OnValidate()
        {
            landingHitstopBase = Mathf.Max(0f, landingHitstopBase);
            landingHitstopPerSpeed = Mathf.Max(0f, landingHitstopPerSpeed);
            landingHitstopMax = Mathf.Max(landingHitstopBase, landingHitstopMax);
            landingShakeBase = Mathf.Max(0f, landingShakeBase);
            landingShakePerSpeed = Mathf.Max(0f, landingShakePerSpeed);
            landingPunchBase = Mathf.Max(0f, landingPunchBase);
            landingPunchPerSpeed = Mathf.Max(0f, landingPunchPerSpeed);
            landingSquashBase = Mathf.Clamp01(landingSquashBase);
            landingSquashPerSpeed = Mathf.Max(0f, landingSquashPerSpeed);
            landingSquashMax = Mathf.Clamp01(landingSquashMax);

            bombHitstop = Mathf.Max(0f, bombHitstop);
            bombShake = Mathf.Max(0f, bombShake);
            bombPunch = Mathf.Max(0f, bombPunch);
            bombSquash = Mathf.Clamp01(bombSquash);
            bombScreenFlash = Mathf.Clamp01(bombScreenFlash);

            iceHitstop = Mathf.Max(0f, iceHitstop);
            iceShake = Mathf.Max(0f, iceShake);
            iceScreenFlash = Mathf.Clamp01(iceScreenFlash);
            cloudShake = Mathf.Max(0f, cloudShake);
            cloudScreenFlash = Mathf.Clamp01(cloudScreenFlash);

            ascendBlockShake = Mathf.Max(0f, ascendBlockShake);
            screenFlashDecay = Mathf.Max(0f, screenFlashDecay);
            squashDecay = Mathf.Max(0f, squashDecay);
            shakeDecay = Mathf.Max(0f, shakeDecay);
            punchDecay = Mathf.Max(0f, punchDecay);
        }
    }
}
