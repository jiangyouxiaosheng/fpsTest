/*
 * ============================================================================
 * 文件：WeaponFireTest.cs
 * 用途：枪械开火测试脚本（Runtime 层，场景测试代码）。
 *       挂在枪械（Test_Pistol_Wingman）上：
 *       1. 运行时把测试武器数据（武器_测试手枪）装备进 WeaponInventory
 *          —— WeaponInstance 是纯 C# 运行时对象、无法序列化进场景，
 *             因此必须由本脚本在 Start 时重新装备；
 *       2. 鼠标左键开火：按住左键 = 全自动连发，单发模式 = 每按一下打一发；
 *       3. 鼠标右键点击：切换射击模式（单发 ↔ 全自动）；
 *       4. 每打掉一发子弹就在 Console 打印弹匣残弹数；
 *       5. 开火后触发同物体上的 WeaponRecoil 后坐力动画。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 枪械开火测试：左键开火（支持按住连发）、右键切换射击模式、
    /// 打印残弹、触发枪身后坐力。弹匣打空后由 WeaponController 自动换弹。
    /// </summary>
    public class WeaponFireTest : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("武器控制器（留空则自动查找本物体上的组件）")]
        [SerializeField] private WeaponController weaponController;

        [Tooltip("测试武器数据（运行时装备进武器栏）")]
        [SerializeField] private WeaponData weaponData;

        [Header("初始弹药")]
        [Tooltip("初始弹匣内子弹数")]
        [SerializeField] private int magazineAmmo = 12;

        [Tooltip("初始备弹数（补充进弹药背包，供弹匣打空后自动换弹）")]
        [SerializeField] private int reserveAmmo = 60;

        /// <summary>后坐力动画组件（本物体上，可选）</summary>
        private WeaponRecoil _recoil;

        /// <summary>上一帧的残弹数（用于检测"打掉一发"）</summary>
        private int _lastAmmo = -1;

        private void Awake()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }

            _recoil = GetComponent<WeaponRecoil>();
        }

        private void Start()
        {
            EquipTestWeapon();
        }

        private void Update()
        {
            if (weaponController == null || weaponController.Weapon == null)
            {
                return;
            }

            var weapon = weaponController.Weapon;

            // —— 鼠标输入（新输入系统）——
            var mouse = Mouse.current;
            if (mouse != null)
            {
                // 左键按下：开火（全自动模式按住会连发，单发模式只打一发）
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (weaponController.IsReloading)
                    {
                        Debug.Log($"[WeaponFireTest] 正在换弹中，弹匣残弹: {weapon.CurrentAmmo}/{weapon.MagazineSize}");
                    }
                    else
                    {
                        weaponController.TryFire();
                    }
                }

                // 左键松开：停止开火（全自动连发随之停止）
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    weaponController.StopFire();
                }

                // 右键点击：切换射击模式（单发 → 全自动 → 三连发 → 单发）
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    CycleFireMode();
                }
            }

            // —— 残弹检测：每打掉一发就打印残弹 + 触发后坐力 ——
            if (weapon.CurrentAmmo < _lastAmmo)
            {
                Debug.Log($"[WeaponFireTest] 开火！弹匣残弹: {weapon.CurrentAmmo}/{weapon.MagazineSize}");

                if (_recoil != null)
                {
                    _recoil.TriggerRecoil();
                }

                if (weapon.CurrentAmmo <= 0)
                {
                    Debug.Log($"[WeaponFireTest] 弹匣已空（0/{weapon.MagazineSize}），自动换弹中…");
                }
            }

            if (weapon.CurrentAmmo != _lastAmmo)
            {
                _lastAmmo = weapon.CurrentAmmo;
            }
        }

        /// <summary>
        /// 运行时装备测试武器并补充备弹。
        /// </summary>
        private void EquipTestWeapon()
        {
            if (weaponController == null || weaponData == null)
            {
                Debug.LogWarning("[WeaponFireTest] 缺少 WeaponController 或 WeaponData 引用，无法装备测试武器");
                return;
            }

            // 运行时兜底：确保模式列表为 全自动/三连发/单发。
            // 资产里 supportedModes 的序列化若被 Unity 读错，会导致右键切换异常；
            // 这里在装备前修正内存中的列表（退出 Play 后自动还原，不影响资产文件）。
            weaponData.supportedModes.Clear();
            weaponData.supportedModes.Add(FireModeType.FullAuto);
            weaponData.supportedModes.Add(FireModeType.Burst);
            weaponData.supportedModes.Add(FireModeType.SemiAuto);

            // 装备为主武器（内部会重建射击模式并重置后坐力状态）
            weaponController.PickupWeapon(WeaponSlotType.Primary, weaponData, magazineAmmo);

            // 补充备弹，供弹匣打空后自动换弹
            var ammoInventory = GetComponent<AmmoInventory>();
            if (ammoInventory != null && weaponData.ammoType != null)
            {
                ammoInventory.AddAmmo(weaponData.ammoType, reserveAmmo);
            }

            _lastAmmo = weaponController.Weapon.CurrentAmmo;

            Debug.Log($"[WeaponFireTest] 测试武器已装备：{weaponData.weaponName}，" +
                      $"弹匣 {_lastAmmo}/{weaponController.Weapon.MagazineSize}，备弹 {reserveAmmo}。" +
                      $"左键开火（按住连发），右键切换射击模式（当前：{GetFireModeName(weaponController.Weapon.CurrentMode)}）");
        }

        /// <summary>
        /// 切换当前武器的射击模式（单发 ↔ 全自动）。
        /// </summary>
        private void CycleFireMode()
        {
            if (weaponController == null || weaponController.Weapon == null)
            {
                return;
            }

            weaponController.CycleFireMode();
            Debug.Log($"[WeaponFireTest] 已切换射击模式：{GetFireModeName(weaponController.Weapon.CurrentMode)}");
        }

        /// <summary>
        /// 射击模式的中文名（仅用于日志）。
        /// </summary>
        private static string GetFireModeName(FireModeType mode)
        {
            return mode switch
            {
                FireModeType.SemiAuto => "单发",
                FireModeType.Burst => "三连发",
                FireModeType.FullAuto => "全自动",
                FireModeType.Charge => "充能",
                _ => mode.ToString()
            };
        }
    }
}
