/*
 * ============================================================================
 * 文件：WeaponTestInput.cs
 * 用途：武器系统测试输入脚本（Runtime 层）。
 *       挂在任意物体上，通过鼠标/键盘驱动 WeaponController 进行测试：
 *       - 左键按住：开火（跟随武器当前射击模式）
 *       - 右键按住：开镜 ADS
 *       - R 键：换弹
 *       - B 键：切换射击模式（如 R-301 单发↔全自动）
 *       - Q 键：切换主/副武器
 *       使用新输入系统（Input System）的 Mouse / Keyboard API，
 *       与项目输入架构一致。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel; // MouseButton 枚举位于 LowLevel 命名空间

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 测试输入脚本：为场景中的武器系统提供简易操控，
    /// 便于快速验证开火/换弹/ADS/切模式/切枪等功能。
    /// 注意：鼠标按键不使用 Key 枚举（Key 仅含键盘键），
    /// 而是通过 Mouse.current.leftButton / rightButton 读取。
    /// </summary>
    public class WeaponTestInput : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("要操控的武器控制器")]
        [SerializeField] private WeaponController weaponController;

        [Header("键位")]
        [Tooltip("开火键（默认左键，按住连射）")]
        [SerializeField] private MouseButton fireButton = MouseButton.Left;

        [Tooltip("开镜键（默认右键，按住开镜）")]
        [SerializeField] private MouseButton adsButton = MouseButton.Right;

        [Tooltip("换弹键（R）")]
        [SerializeField] private Key reloadKey = Key.R;

        [Tooltip("切换射击模式键（B）")]
        [SerializeField] private Key cycleModeKey = Key.B;

        [Tooltip("切换主/副武器键（Q）")]
        [SerializeField] private Key cycleSlotKey = Key.Q;

        /// <summary>武器控制器（供 Inspector 或代码设置）</summary>
        public WeaponController WeaponController
        {
            get => weaponController;
            set => weaponController = value;
        }

        private void Update()
        {
            if (weaponController == null)
            {
                return;
            }

            // —— 鼠标按键读取：Mouse.current 可能为 null（如无鼠标环境），需判空 ——
            var mouse = Mouse.current;
            if (mouse != null)
            {
                bool fireDown = IsMouseButtonPressed(mouse, fireButton);
                bool fireUp = IsMouseButtonReleased(mouse, fireButton);

                // 开火：按住触发，松开停止（单发模式只在按下瞬间发一发）
                if (fireDown)
                {
                    weaponController.TryFire();
                }

                if (fireUp)
                {
                    weaponController.StopFire();
                }

                // 开镜：按住开镜，松开关镜
                if (IsMouseButtonPressed(mouse, adsButton))
                {
                    weaponController.ToggleAds(true);
                }

                if (IsMouseButtonReleased(mouse, adsButton))
                {
                    weaponController.ToggleAds(false);
                }
            }

            // —— 键盘读取 ——
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // 换弹
                if (keyboard[reloadKey].wasPressedThisFrame)
                {
                    weaponController.StartReload();
                }

                // 切换射击模式
                if (keyboard[cycleModeKey].wasPressedThisFrame)
                {
                    weaponController.CycleFireMode();
                }

                // 切换主/副武器
                if (keyboard[cycleSlotKey].wasPressedThisFrame)
                {
                    weaponController.CycleSlot();
                }
            }
        }

        /// <summary>
        /// 判断指定鼠标按键是否在本帧按下。
        /// </summary>
        private static bool IsMouseButtonPressed(Mouse mouse, MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => mouse.leftButton.wasPressedThisFrame,
                MouseButton.Right => mouse.rightButton.wasPressedThisFrame,
                MouseButton.Middle => mouse.middleButton.wasPressedThisFrame,
                MouseButton.Forward => mouse.forwardButton.wasPressedThisFrame,
                MouseButton.Back => mouse.backButton.wasPressedThisFrame,
                _ => false
            };
        }

        /// <summary>
        /// 判断指定鼠标按键是否在本帧松开。
        /// </summary>
        private static bool IsMouseButtonReleased(Mouse mouse, MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => mouse.leftButton.wasReleasedThisFrame,
                MouseButton.Right => mouse.rightButton.wasReleasedThisFrame,
                MouseButton.Middle => mouse.middleButton.wasReleasedThisFrame,
                MouseButton.Forward => mouse.forwardButton.wasReleasedThisFrame,
                MouseButton.Back => mouse.backButton.wasReleasedThisFrame,
                _ => false
            };
        }
    }
}
