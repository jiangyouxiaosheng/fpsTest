/*
 * ============================================================================
 * 文件：AttachmentSystem.cs
 * 用途：配件装配/卸下的校验与执行（静态工具类）。
 *       负责检查：槽位是否匹配、稀有度是否超过武器槽上限、同槽唯一性，
 *       并调用 WeaponInstance.EquipAttachment 真正写入。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 配件系统：静态校验 + 装配入口。
    /// 设计为静态类，不持有状态，方便在拾取 UI / 背包 / 地面交互中复用。
    /// </summary>
    public static class AttachmentSystem
    {
        /// <summary>
        /// 校验配件是否能装配到这把武器上。
        /// </summary>
        /// <param name="weapon">目标武器实例</param>
        /// <param name="attachment">待装配配件</param>
        /// <param name="error">失败原因（成功时为 null）</param>
        /// <returns>true = 可以装配</returns>
        public static bool Validate(WeaponInstance weapon, AttachmentData attachment, out string error)
        {
            error = null;

            if (weapon == null || weapon.Data == null)
            {
                error = "武器为空";
                return false;
            }

            if (attachment == null)
            {
                error = "配件为空";
                return false;
            }

            // 1. 查找武器是否声明了该槽位
            var slotDef = weapon.Data.attachmentSlots.Find(s => s.type == attachment.slot);
            if (slotDef == null)
            {
                error = $"武器 [{weapon.Data.weaponName}] 没有 [{attachment.slot}] 槽位";
                return false;
            }

            // 2. 稀有度是否超过槽位上限
            if (attachment.rarity > slotDef.maxRarity)
            {
                error = $"配件稀有度 [{attachment.rarity}] 超过槽位上限 [{slotDef.maxRarity}]";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试装配配件。校验通过则替换同槽旧配件并返回 true。
        /// </summary>
        public static bool TryEquip(WeaponInstance weapon, AttachmentData attachment)
        {
            if (!Validate(weapon, attachment, out _))
            {
                return false;
            }

            weapon.EquipAttachment(attachment);
            Debug.Log($"[AttachmentSystem] 装配 [{attachment.displayName}] → {weapon.Data.weaponName}");
            return true;
        }

        /// <summary>
        /// 卸下指定槽位配件，返回被卸下的配件（无则 null）。
        /// </summary>
        public static AttachmentData Unequip(WeaponInstance weapon, AttachmentType slot)
        {
            if (weapon == null)
            {
                return null;
            }

            var removed = weapon.UnequipAttachment(slot);
            if (removed != null)
            {
                Debug.Log($"[AttachmentSystem] 卸下 [{removed.displayName}]");
            }

            return removed;
        }
    }
}
