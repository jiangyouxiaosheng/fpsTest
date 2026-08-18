/*
 * ============================================================================
 * 文件：HitscanShot.cs
 * 用途：即时命中射击工具（静态类）。
 *       对轻/重型武器使用：从枪口沿方向发射一条射线，立即判定命中，
 *       并应用伤害衰减（距离越远伤害越低）与爆头倍率。
 *       霰弹枪的"多发弹丸"也在此用多次射线模拟。
 * 所属：CharacterController.FPS.Weapon.Ballistic（Runtime 层）
 * ============================================================================
 */

using CharacterController.FPS.Weapon.Damage;
using UnityEngine;

namespace CharacterController.FPS.Weapon.Ballistic
{
    /// <summary>
    /// 即时命中工具：一次调用完成 射线检测 → 衰减计算 → 伤害施加。
    /// 返回是否命中，方便 WeaponController 播放命中特效/音效。
    /// </summary>
    public static class HitscanShot
    {
        /// <summary>
        /// 发射单发即时命中射线。
        /// </summary>
        /// <param name="origin">射线起点（枪口）</param>
        /// <param name="direction">射线方向（已含散布）</param>
        /// <param name="maxDistance">最大射程</param>
        /// <param name="baseDamage">基础伤害（未衰减）</param>
        /// <param name="ballistic">弹道数据（衰减参数）</param>
        /// <param name="headshotMultiplier">爆头倍率</param>
        /// <param name="source">伤害来源（开枪者）</param>
        /// <param name="hitMask">可命中层级</param>
        /// <returns>是否命中物体</returns>
        public static bool Fire(Vector3 origin, Vector3 direction, float maxDistance,
            float baseDamage, BallisticData ballistic, float headshotMultiplier,
            GameObject source, LayerMask hitMask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, hitMask))
            {
                // 计算距离衰减后的伤害
                float distance = hit.distance;
                float damage = baseDamage;

                if (ballistic != null)
                {
                    damage = ApplyFalloff(baseDamage, distance, ballistic);
                }

                // 爆头判定：命中碰撞体或其父级挂有 HeadshotMarker 标记
                bool isHeadshot = hit.collider.GetComponentInParent<HeadshotMarker>() != null;
                if (isHeadshot)
                {
                    damage *= headshotMultiplier;
                }

                // 施加伤害
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    var info = new DamageInfo(
                        damage,
                        isHeadshot ? DamageType.Headshot : DamageType.Bullet,
                        hit.point,
                        hit.normal,
                        source);
                    damageable.TakeDamage(info);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 霰弹枪射击：在锥形范围内随机散布发射 pellets 条射线。
        /// </summary>
        /// <param name="pellets">弹丸数</param>
        /// <param name="spreadAngle">散布锥角（度）</param>
        /// <returns>命中的弹丸数</returns>
        public static int FireSpread(Vector3 origin, Vector3 direction, float maxDistance,
            float baseDamage, BallisticData ballistic, float headshotMultiplier,
            GameObject source, LayerMask hitMask, int pellets, float spreadAngle)
        {
            int hitCount = 0;

            for (int i = 0; i < pellets; i++)
            {
                // 在锥形范围内随机偏移方向
                Vector3 spreadDir = Quaternion.Euler(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    0f) * direction;

                if (Fire(origin, spreadDir, maxDistance, baseDamage, ballistic,
                        headshotMultiplier, source, hitMask))
                {
                    hitCount++;
                }
            }

            return hitCount;
        }

        /// <summary>
        /// 伤害衰减计算：距离在 [falloffStart, falloffEnd] 间线性降低到 minMultiplier。
        /// </summary>
        public static float ApplyFalloff(float baseDamage, float distance, BallisticData ballistic)
        {
            if (distance <= ballistic.damageFalloffStart)
            {
                return baseDamage;
            }

            if (distance >= ballistic.damageFalloffEnd)
            {
                return baseDamage * ballistic.minDamageMultiplier;
            }

            // 线性插值衰减
            float t = (distance - ballistic.damageFalloffStart)
                      / (ballistic.damageFalloffEnd - ballistic.damageFalloffStart);
            float multiplier = Mathf.Lerp(1f, ballistic.minDamageMultiplier, t);
            return baseDamage * multiplier;
        }
    }
}
