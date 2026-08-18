/*
 * ============================================================================
 * 文件：AutoFireDemo.cs
 * 用途：枪械功能自动演示脚本（Runtime 层）。
 *       挂在与 WeaponController 同一物体上，按固定节奏自动执行：
 *       开火 → 打空弹匣 → 换弹 → 继续开火。
 *       用于在【不接入玩家输入】的情况下直观查看枪械系统的工作流程
 *       （开火、弹药消耗、空仓换弹、后坐力、散布、伤害）。
 *       查看完挂载效果后可随时删除本组件。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 自动开火演示：每 fireInterval 秒开一枪，
    /// 弹匣打空后自动触发换弹（空仓换弹），从而演示完整枪械循环。
    /// </summary>
    public class AutoFireDemo : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("要驱动的武器控制器（自动查找本物体上的组件）")]
        [SerializeField] private WeaponController weaponController;

        [Header("演示节奏")]
        [Tooltip("每次开火的间隔（秒）")]
        [SerializeField] private float fireInterval = 0.8f;

        [Tooltip("是否在弹匣打空后自动换弹")]
        [SerializeField] private bool autoReload = true;

        /// <summary>累计计时器</summary>
        private float _timer;

        private void Awake()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }
        }

        private void Update()
        {
            if (weaponController == null)
            {
                return;
            }

            // 换弹期间等待，不推进开火计时
            if (weaponController.IsReloading)
            {
                return;
            }

            _timer += Time.deltaTime;

            // 达到间隔 → 开一枪
            if (_timer >= fireInterval)
            {
                _timer = 0f;

                // 按下→发射→松开（模拟一次完整的扳机操作）
                weaponController.TryFire();
                weaponController.StopFire();

                // 弹匣打空后自动换弹（演示空仓换弹）
                if (autoReload && weaponController.Weapon != null &&
                    weaponController.Weapon.CurrentAmmo <= 0)
                {
                    weaponController.StartReload();
                }
            }
        }
    }
}
