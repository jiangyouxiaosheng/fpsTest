/*
 * ============================================================================
 * 文件：WeaponInventory.cs
 * 用途：角色武器栏（MonoBehaviour）：主武器 + 副武器两个槽位，
 *       管理拾取/丢弃/切换武器，并持有弹药背包引用。
 *       武器切换后由 WeaponController 感知当前武器变化并重设射击模式。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 武器栏：两个槽位（主/副，对齐 APEX 双持）。
    /// 当前激活的武器由 CurrentWeapon 暴露，供 WeaponController 使用。
    /// </summary>
    public class WeaponInventory : MonoBehaviour
    {
        [Header("武器槽位")]
        [Tooltip("主武器实例（可为空）")]
        [SerializeField] private WeaponInstance primaryWeapon;

        [Tooltip("副武器实例（可为空）")]
        [SerializeField] private WeaponInstance secondaryWeapon;

        [Header("依赖")]
        [Tooltip("弹药背包：换弹时从此处提取备弹")]
        [SerializeField] private AmmoInventory ammoInventory;

        /// <summary>当前激活的槽位</summary>
        public WeaponSlotType CurrentSlot { get; private set; } = WeaponSlotType.Primary;

        /// <summary>弹药背包引用</summary>
        public AmmoInventory AmmoInventory => ammoInventory;

        /// <summary>当前激活的武器实例（可能为空）</summary>
        public WeaponInstance CurrentWeapon
            => CurrentSlot == WeaponSlotType.Primary ? primaryWeapon : secondaryWeapon;

        // ==================== 拾取 / 放置 ====================

        /// <summary>
        /// 拾取一把武器放入指定槽位（旧武器被替换并返回，供地面掉落）。
        /// </summary>
        public WeaponInstance EquipWeapon(WeaponSlotType slot, WeaponData data, int ammoInMagazine)
        {
            var instance = new WeaponInstance(data, ammoInMagazine);
            return ReplaceWeapon(slot, instance);
        }

        /// <summary>
        /// 直接用实例放入槽位（用于从地面捡回已有配件的武器）。
        /// </summary>
        public WeaponInstance ReplaceWeapon(WeaponSlotType slot, WeaponInstance instance)
        {
            WeaponInstance old;
            if (slot == WeaponSlotType.Primary)
            {
                old = primaryWeapon;
                primaryWeapon = instance;
            }
            else
            {
                old = secondaryWeapon;
                secondaryWeapon = instance;
            }

            return old;
        }

        /// <summary>
        /// 丢弃当前武器，槽位置空。返回被丢弃的实例。
        /// </summary>
        public WeaponInstance DropCurrentWeapon()
        {
            return ReplaceWeapon(CurrentSlot, null);
        }

        /// <summary>
        /// 清空两个槽位（重生等场景）。
        /// </summary>
        public void Clear()
        {
            primaryWeapon = null;
            secondaryWeapon = null;
        }

        // ==================== 切换 ====================

        /// <summary>
        /// 切换到指定槽位。目标槽为空则拒绝切换。
        /// </summary>
        public bool SwitchTo(WeaponSlotType slot)
        {
            WeaponInstance target = slot == WeaponSlotType.Primary ? primaryWeapon : secondaryWeapon;
            if (target == null)
            {
                return false;
            }

            CurrentSlot = slot;
            return true;
        }

        /// <summary>
        /// 在两个槽位间轮换切换（按键 1/2 或滚轮）。
        /// </summary>
        public bool CycleSlot()
        {
            WeaponSlotType next = CurrentSlot == WeaponSlotType.Primary
                ? WeaponSlotType.Secondary
                : WeaponSlotType.Primary;
            return SwitchTo(next);
        }
    }
}
