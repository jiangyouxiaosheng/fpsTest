/*
 * ============================================================================
 * 文件：IFireMode.cs
 * 用途：射击模式策略接口。
 *       所有射击模式（单发/连发/全自动/充能）实现此接口，
 *       WeaponController 通过它统一驱动开火，从而支持"模式可切换"。
 * 所属：CharacterController.FPS.Weapon.FireMode（Runtime 层）
 * ============================================================================
 */

namespace CharacterController.FPS.Weapon.FireMode
{
    /// <summary>
    /// 射击模式策略接口（策略模式）。
    /// WeaponController 持有当前模式实例，把扳机输入转发给模式处理。
    /// </summary>
    public interface IFireMode
    {
        /// <summary>本模式对应的类型（单发/连发/全自动/充能）</summary>
        FireModeType Type { get; }

        /// <summary>
        /// 切换到本模式时调用（重置内部状态，如连发计数、冷却）。
        /// </summary>
        void OnSwitchTo(WeaponController controller);

        /// <summary>
        /// 按下扳机时调用（单发在此发射；连发在此启动连发序列）。
        /// </summary>
        void OnTriggerDown(WeaponController controller);

        /// <summary>
        /// 按住扳机期间每帧调用（全自动在此持续发射；连发在此推进连发序列）。
        /// </summary>
        void OnTriggerHold(WeaponController controller, float deltaTime);

        /// <summary>
        /// 松开扳机时调用（中断连发/充能）。
        /// </summary>
        void OnTriggerUp(WeaponController controller);
    }
}
