/*
 * ============================================================================
 * 文件：BurstFireMode.cs
 * 用途：连发（三连发/五连发）射击模式实现。
 *       按下扳机后按 burstInterval 节奏连续发射 burstCount 发，
 *       期间按住的时长不会增加发射数量，松开可中断后续连发。
 * 所属：CharacterController.FPS.Weapon.FireMode（Runtime 层）
 * ============================================================================
 */

namespace CharacterController.FPS.Weapon.FireMode
{
    /// <summary>
    /// 连发模式：一次扣扳机连续发射 N 发（Hemlok 3 连发、Prowler 5 连发）。
    /// 内部用定时器按 burstInterval 推进，不受武器射速冷却限制（连发节奏更快）。
    /// </summary>
    public class BurstFireMode : IFireMode
    {
        /// <summary>连发序列中剩余待发射的子弹数</summary>
        private int _remainingInBurst;

        /// <summary>连发内部定时器（秒）</summary>
        private float _burstTimer;

        public FireModeType Type => FireModeType.Burst;

        public void OnSwitchTo(WeaponController controller)
        {
            _remainingInBurst = 0;
            _burstTimer = 0f;
        }

        public void OnTriggerDown(WeaponController controller)
        {
            // 只有不在连发中才启动新序列（防止按住时反复触发）
            if (_remainingInBurst <= 0)
            {
                _remainingInBurst = controller.Weapon.Data.burstCount;
                _burstTimer = 0f;
                TryFireBurst(controller);
            }
        }

        public void OnTriggerHold(WeaponController controller, float deltaTime)
        {
            // 推进连发序列：按 burstInterval 节奏逐发发射
            if (_remainingInBurst <= 0)
            {
                return;
            }

            _burstTimer += deltaTime;
            while (_remainingInBurst > 0 && _burstTimer >= controller.Weapon.Data.burstInterval)
            {
                _burstTimer -= controller.Weapon.Data.burstInterval;
                TryFireBurst(controller);
            }
        }

        public void OnTriggerUp(WeaponController controller)
        {
            // 松开扳机中断剩余连发（APEX 中可提前打断）
            _remainingInBurst = 0;
            _burstTimer = 0f;
        }

        /// <summary>
        /// 发射连发序列中的一发。
        /// 连发节奏由 burstInterval 控制，绕过武器射速冷却（bypassCooldown）。
        /// </summary>
        private void TryFireBurst(WeaponController controller)
        {
            if (_remainingInBurst <= 0)
            {
                return;
            }

            // 弹匣空了则中断连发，交由控制器触发空仓换弹
            if (!controller.FireOneShot(bypassCooldown: true))
            {
                _remainingInBurst = 0;
                return;
            }

            _remainingInBurst--;
        }
    }
}
