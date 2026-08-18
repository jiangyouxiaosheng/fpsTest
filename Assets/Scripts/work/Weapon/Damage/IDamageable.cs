/*
 * ============================================================================
 * 文件：IDamageable.cs
 * 用途：伤害系统的核心接口。
 *       任何可被子弹命中的物体（玩家、敌人、靶子、可破坏物）实现此接口，
 *       弹道命中后统一调用 TakeDamage，实现"伤害来源无关"的解耦。
 * 所属：CharacterController.FPS.Weapon.Damage（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon.Damage
{
    /// <summary>
    /// 伤害类型（扩展点：后续可加 Explosive / Energy / Melee 等）。
    /// </summary>
    public enum DamageType
    {
        Bullet,     // 普通子弹
        Headshot,   // 爆头（可拆分为独立类型，便于 UI 与音效区分）
        Special     // 特殊弹药（Kraber 等）
    }

    /// <summary>
    /// 一次伤害的完整信息：数值、类型、命中点、命中法线、伤害来源。
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>伤害数值（已含爆头倍率与衰减）</summary>
        public readonly float Amount;

        /// <summary>伤害类型</summary>
        public readonly DamageType Type;

        /// <summary>命中世界坐标</summary>
        public readonly Vector3 HitPoint;

        /// <summary>命中表面法线</summary>
        public readonly Vector3 HitNormal;

        /// <summary>伤害来源（开枪者，可为 null）</summary>
        public readonly GameObject Source;

        public DamageInfo(float amount, DamageType type, Vector3 hitPoint, Vector3 hitNormal, GameObject source)
        {
            Amount = amount;
            Type = type;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Source = source;
        }
    }

    /// <summary>
    /// 可受伤接口：实现 TakeDamage 即可被武器系统伤害。
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(in DamageInfo info);
    }
}
