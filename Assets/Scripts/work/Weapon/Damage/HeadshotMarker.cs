/*
 * ============================================================================
 * 文件：HeadshotMarker.cs
 * 用途：爆头区域标记组件（空标记，无字段）。
 *       挂在角色/敌人模型的"头部碰撞体"上，弹道命中时通过
 *       GetComponentInParent 判断是否命中头部，从而应用爆头倍率。
 *       相比 CompareTag("Head")，组件方式无需配置 TagManager，更稳健。
 * 所属：CharacterController.FPS.Weapon.Damage（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon.Damage
{
    /// <summary>
    /// 爆头标记：作为头部碰撞体的标记组件使用。
    /// 例：在角色模型的 Head 子碰撞体上挂此组件，
    /// 射击命中该碰撞体时自动按爆头计算伤害。
    /// </summary>
    public class HeadshotMarker : MonoBehaviour
    {
        // 纯标记组件，无需任何字段或逻辑。
        // 检测方式见 HitscanShot.Fire / Projectile.OnHit。
    }
}
