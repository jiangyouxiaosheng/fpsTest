/*
 * ============================================================================
 * 文件：FullAutoFireMode.cs
 * 用途：全自动射击模式实现。
 *       按住扳机期间按武器射速（fireRate）持续自动发射，
 *       松开即停。最常用的模式（R-301 / Volt / Flatline 等）。
 * 所属：CharacterController.FPS.Weapon.FireMode（Runtime 层）
 * ============================================================================
 */

namespace CharacterController.FPS.Weapon.FireMode
{
    /// <summary>
    /// 全自动模式：按住扳机持续发射，节奏由 WeaponController 的射速冷却控制
    /// （FireOneShot 内部检查冷却，未冷却完成则本帧不发射）。
    /// </summary>
    public class FullAutoFireMode : IFireMode
    {
        public FireModeType Type => FireModeType.FullAuto;

        public void OnSwitchTo(WeaponController controller)
        {
            // 无需特殊处理
        }

        public void OnTriggerDown(WeaponController controller)
        {
            // 按下瞬间先发射一发（避免等待冷却的帧延迟）
            controller.FireOneShot();
        }

        public void OnTriggerHold(WeaponController controller, float deltaTime)
        {
            // 按住期间持续请求发射，由射速冷却控制实际节奏
            controller.FireOneShot();
        }

        public void OnTriggerUp(WeaponController controller)
        {
            // 松开停止请求，FireOneShot 不再被调用
        }
    }
}
