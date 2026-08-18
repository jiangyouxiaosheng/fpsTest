/*
 * ============================================================================
 * 文件：Health.cs
 * 用途：通用血量组件示例实现（MonoBehaviour）。
 *       实现 IDamageable 接口，管理血量与可选护甲（对齐 APEX 护甲概念），
 *       提供死亡回调事件，供玩家/敌人/靶子直接挂载使用。
 * 所属：CharacterController.FPS.Weapon.Damage（Runtime 层）
 * ============================================================================
 */

using System;
using UnityEngine;

namespace CharacterController.FPS.Weapon.Damage
{
    /// <summary>
    /// 通用血量组件：血量 + 可选护甲 + 死亡事件。
    /// 护甲先于血量承受伤害（APEX 的护盾机制简化版）。
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("血量")]
        [Tooltip("最大生命值")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("最大护甲值（0 = 无护甲）")]
        [SerializeField] private float maxShield = 0f;

        /// <summary>当前生命值</summary>
        public float CurrentHealth { get; private set; }

        /// <summary>当前护甲值</summary>
        public float CurrentShield { get; private set; }

        /// <summary>是否已死亡</summary>
        public bool IsDead { get; private set; }

        /// <summary>受伤回调（参数为伤害信息）</summary>
        public event Action<DamageInfo> OnDamaged;

        /// <summary>死亡回调</summary>
        public event Action<GameObject> OnDied;

        /// <summary>最大生命值（只读）</summary>
        public float MaxHealth => maxHealth;

        /// <summary>最大护甲值（只读）</summary>
        public float MaxShield => maxShield;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentShield = maxShield;
        }

        /// <summary>
        /// 收到伤害：先扣护甲，再扣血量；血量归零触发死亡。
        /// </summary>
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead)
            {
                return;
            }

            float remaining = info.Amount;

            // 1. 扣护甲
            if (CurrentShield > 0f)
            {
                float shieldAbsorb = Mathf.Min(CurrentShield, remaining);
                CurrentShield -= shieldAbsorb;
                remaining -= shieldAbsorb;
            }

            // 2. 扣血量
            if (remaining > 0f)
            {
                CurrentHealth -= remaining;
            }

            OnDamaged?.Invoke(info);

            // 3. 死亡判定
            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                IsDead = true;
                OnDied?.Invoke(info.Source);
                Debug.Log($"[Health] {name} 已死亡，击杀者：{(info.Source != null ? info.Source.name : "未知")}");
            }
        }

        /// <summary>
        /// 治疗（超出上限部分丢弃）。
        /// </summary>
        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            IsDead = false; // 允许复活场景
        }

        /// <summary>
        /// 补充护甲。
        /// </summary>
        public void RechargeShield(float amount)
        {
            CurrentShield = Mathf.Min(CurrentShield + amount, maxShield);
        }

        /// <summary>
        /// 立即死亡（用于坠落/秒杀等非子弹伤害）。
        /// </summary>
        public void Kill(GameObject source)
        {
            TakeDamage(new DamageInfo(CurrentHealth + CurrentShield, DamageType.Special,
                transform.position, Vector3.up, source));
        }
    }
}
