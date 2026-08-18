/*
 * ============================================================================
 * 文件：BurstFireMode.cs
 * 用途：连发（三连发/五连发）射击模式实现。
 *       按下扳机后立即打出第一发，然后按 burstInterval 节奏由协程打完
 *       burstCount 发——整组连发必定打满，松开扳机不会中断（保证
 *       "三连发就是三发"，不会因点击太快只打出 1 发）；
 *       按住期间不会重复触发新的连发序列（一次扳机 = 一组连发）。
 * 所属：CharacterController.FPS.Weapon.FireMode（Runtime 层）
 * ============================================================================
 */

using System.Collections;
using UnityEngine;

namespace CharacterController.FPS.Weapon.FireMode
{
    /// <summary>
    /// 连发模式：一次扣扳机必定打满 N 发（Hemlok 3 连发、Prowler 5 连发）。
    /// 由协程驱动节奏，不依赖按住时长；连发节奏由 burstInterval 控制，
    /// 绕过武器射速冷却（bypassCooldown），保证三发紧凑打出。
    /// </summary>
    public class BurstFireMode : IFireMode
    {
        /// <summary>连发序列中剩余待发射的子弹数</summary>
        private int _remainingInBurst;

        /// <summary>驱动连发的协程（切换模式/武器时用于停止）</summary>
        private Coroutine _burstRoutine;

        /// <summary>当前所属控制器（启动/停止协程用）</summary>
        private WeaponController _controller;

        public FireModeType Type => FireModeType.Burst;

        public void OnSwitchTo(WeaponController controller)
        {
            // 切换武器/模式时，停止进行中的连发协程并复位状态
            if (_controller != null && _burstRoutine != null)
            {
                _controller.StopCoroutine(_burstRoutine);
            }

            _remainingInBurst = 0;
            _burstRoutine = null;
            _controller = controller;
        }

        public void OnTriggerDown(WeaponController controller)
        {
            // 已有连发进行中则忽略本次按下（一次扳机 = 一组连发）
            if (_remainingInBurst > 0 || _burstRoutine != null)
            {
                return;
            }

            _remainingInBurst = controller.Weapon.Data.burstCount;
            _burstRoutine = controller.StartCoroutine(BurstRoutine(controller));
        }

        public void OnTriggerHold(WeaponController controller, float deltaTime)
        {
            // 连发节奏由协程驱动，按住无额外效果
        }

        public void OnTriggerUp(WeaponController controller)
        {
            // 不中断进行中的连发：保证一次扣扳机必定打满整组
        }

        /// <summary>
        /// 连发协程：立即打出第一发，之后按 burstInterval 节奏打完剩余子弹。
        /// </summary>
        private IEnumerator BurstRoutine(WeaponController controller)
        {
            while (_remainingInBurst > 0)
            {
                TryFireBurst(controller);

                if (_remainingInBurst <= 0)
                {
                    break;
                }

                yield return new WaitForSeconds(controller.Weapon.Data.burstInterval);
            }

            _burstRoutine = null;
        }

        /// <summary>
        /// 发射连发序列中的一发。
        /// 弹匣空了则中断整组连发，交由控制器触发空仓换弹。
        /// </summary>
        private void TryFireBurst(WeaponController controller)
        {
            if (_remainingInBurst <= 0)
            {
                return;
            }

            if (!controller.FireOneShot(bypassCooldown: true))
            {
                _remainingInBurst = 0;
                return;
            }

            _remainingInBurst--;
        }
    }
}
