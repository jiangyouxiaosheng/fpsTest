/*
 * ============================================================================
 * 文件：BallisticData.cs
 * 用途：弹道/子弹数据资产（ScriptableObject）。
 *       定义子弹是"即时命中(Hitscan)"还是"实体弹丸(Projectile)"，
 *       以及弹丸速度、重力、伤害衰减、霰弹弹丸数等参数。
 *       由 WeaponController 在开火时按此数据决定命中判定方式。
 * 所属：CharacterController.FPS.Weapon（Data 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 弹道数据。APEX 为弹道制（子弹有飞行时间与下坠），
    /// 但为性能与手感，轻/重型武器多用即时命中，能量/狙击用实体弹丸。
    /// </summary>
    [CreateAssetMenu(menuName = "FPS/Weapon/Ballistic Data", fileName = "Ballistic_New")]
    public class BallisticData : ScriptableObject
    {
        [Header("命中方式")]
        [Tooltip("true = 即时命中（射线立刻判定）；false = 实体弹丸（有飞行时间）")]
        public bool isHitscan = true;

        [Tooltip("弹丸飞行速度（米/秒），仅 isHitscan = false 时使用")]
        public float projectileSpeed = 300f;

        [Tooltip("重力影响系数（0 = 无下坠；0.5 = 一半重力；1 = 完整重力）")]
        [Range(0f, 2f)]
        public float gravityScale = 0f;

        [Header("伤害衰减")]
        [Tooltip("衰减起始距离（米）：超过后伤害开始下降")]
        public float damageFalloffStart = 40f;

        [Tooltip("衰减结束距离（米）：到达后伤害降到最低")]
        public float damageFalloffEnd = 100f;

        [Tooltip("最远距离处的最小伤害倍率（0.6 = 只剩 60% 伤害）")]
        [Range(0f, 1f)]
        public float minDamageMultiplier = 0.6f;

        [Header("霰弹")]
        [Tooltip("单次开火发射的弹丸数（EVA-8 = 8，普通枪 = 1）")]
        [Range(1, 12)]
        public int pellets = 1;

        [Tooltip("穿甲系数（0 = 不穿甲；>0 时按比例无视护甲）")]
        [Range(0f, 1f)]
        public float armorPiercing = 0f;
    }
}
