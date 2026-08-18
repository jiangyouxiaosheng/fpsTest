/*
 * ============================================================================
 * 文件：SemiAutoFireMode.cs
 * 用途：单发射击模式实现。
 *       每次按下扳机发射 1 发，受武器射速（fireRate）冷却限制，
 *       防止"点射过快"超出武器理论射速。
 * 所属：CharacterController.FPS.Weapon.FireMode（Runtime 层）
 * ============================================================================
 */

namespace CharacterController.FPS.Weapon.FireMode
{
    /// <summary>
    /// 单发模式：按下扳机 → 发射一发 → 等待射速冷却。
    /// 适用于 P2020 / Wingman / G7 等半自动武器。
    /// </summary>
    public class SemiAutoFireMode : IFireMode
    {
        public FireModeType Type => FireModeType.SemiAuto;

        public void OnSwitchTo(WeaponController controller)
        {
            // 无需特殊处理：冷却由 WeaponController 统一管理
        }

        public void OnTriggerDown(WeaponController controller)
        {
            // 单发：按下即尝试发射一发
            controller.FireOneShot();
        }

        public void OnTriggerHold(WeaponController controller, float deltaTime)
        {
            // 单发模式按住无效果（如需"按住连点"可在此按冷却自动补发）
        }

        public void OnTriggerUp(WeaponController controller)
        {
            // 松开无特殊处理
        }
    }
}
