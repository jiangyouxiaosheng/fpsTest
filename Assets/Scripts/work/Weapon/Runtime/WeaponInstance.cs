/*
 * ============================================================================
 * 文件：WeaponInstance.cs
 * 用途：单把武器的运行时可变实例（非 MonoBehaviour，纯数据容器）。
 *       它把 WeaponData（静态表）与运行状态（当前弹药、已装配件、当前模式、
 *       当前状态）组合在一起，并对外暴露"聚合属性"——
 *       即 基础值 × 所有配件修正 后的最终数值，供射击/换弹/HUD 使用。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using System.Collections.Generic;
using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 武器运行时实例。由 WeaponInventory 创建并持有，
    /// 一把武器对应一个实例；实例被装配到角色后才可开火。
    /// </summary>
    public class WeaponInstance
    {
        /// <summary>武器静态数据（只读引用）</summary>
        public WeaponData Data { get; }

        /// <summary>当前弹匣内子弹数</summary>
        public int CurrentAmmo { get; private set; }

        /// <summary>当前射击模式</summary>
        public FireModeType CurrentMode { get; private set; }

        /// <summary>武器当前状态（待机/开火/换弹/切换/开镜）</summary>
        public WeaponState State { get; set; }

        /// <summary>已装配件：槽位 → 配件数据</summary>
        private readonly Dictionary<AttachmentType, AttachmentData> _attachments = new();

        /// <summary>已装配件（只读遍历用）</summary>
        public IReadOnlyDictionary<AttachmentType, AttachmentData> Attachments => _attachments;

        /// <summary>开火累计发数（用于后坐力模式采样与散布递增）</summary>
        public int ShotIndex { get; set; }

        // ==================== 构造与初始化 ====================

        public WeaponInstance(WeaponData data, int initialAmmo)
        {
            Data = data;
            CurrentAmmo = Mathf.Clamp(initialAmmo, 0, data.magazineSize);
            CurrentMode = data.defaultMode;
            State = WeaponState.Idle;
        }

        // ==================== 弹药 ====================

        /// <summary>
        /// 消耗一发子弹。返回是否成功（弹匣为空时失败，触发空仓换弹）。
        /// </summary>
        public bool ConsumeAmmo()
        {
            if (CurrentAmmo <= 0)
            {
                return false;
            }

            CurrentAmmo--;
            ShotIndex++;
            return true;
        }

        /// <summary>
        /// 直接补充弹匣（拾取/作弊/金色弹匣等用）。
        /// </summary>
        public void AddAmmoToMagazine(int amount)
        {
            CurrentAmmo = Mathf.Min(CurrentAmmo + amount, MagazineSize);
        }

        // ==================== 配件 ====================

        /// <summary>
        /// 装配一个配件到对应槽位（会覆盖同槽旧配件）。
        /// 校验逻辑在 AttachmentSystem.Validate 中。
        /// </summary>
        public void EquipAttachment(AttachmentData attachment)
        {
            if (attachment == null)
            {
                return;
            }

            _attachments[attachment.slot] = attachment;
        }

        /// <summary>
        /// 卸下指定槽位的配件，返回被卸下的配件（无则返回 null）。
        /// </summary>
        public AttachmentData UnequipAttachment(AttachmentType slot)
        {
            if (_attachments.TryGetValue(slot, out var old))
            {
                _attachments.Remove(slot);
                return old;
            }

            return null;
        }

        /// <summary>
        /// 获取某槽位已装的配件（无则 null）。
        /// </summary>
        public AttachmentData GetAttachment(AttachmentType slot)
        {
            _attachments.TryGetValue(slot, out var attachment);
            return attachment;
        }

        // ==================== 射击模式 ====================

        /// <summary>
        /// 直接设置当前射击模式（仅在模式受支持时生效，否则忽略并保持原模式）。
        /// 供外部按 UI 选择、HopUp 赋予或武器数据默认值设置使用。
        /// </summary>
        public void SetCurrentMode(FireModeType mode)
        {
            // 校验模式是否可用（基础支持 或 配件赋予）
            bool available = Data.supportedModes.Contains(mode);
            if (!available)
            {
                foreach (var kv in _attachments)
                {
                    if (kv.Value.enablesNewFireMode && kv.Value.grantedFireMode == mode)
                    {
                        available = true;
                        break;
                    }
                }
            }

            if (available)
            {
                CurrentMode = mode;
            }
        }

        /// <summary>
        /// 在支持的射击模式间轮换（APEX 的按键切换模式）。
        /// 若金色 HopUp 赋予了新模式，也会一并参与轮换。
        /// </summary>
        public void CycleFireMode()
        {
            if (Data.supportedModes.Count == 0)
            {
                return;
            }

            // 收集所有可用模式（基础 + HopUp 赋予）
            var allModes = new List<FireModeType>(Data.supportedModes);
            foreach (var kv in _attachments)
            {
                var att = kv.Value;
                if (att.enablesNewFireMode && !allModes.Contains(att.grantedFireMode))
                {
                    allModes.Add(att.grantedFireMode);
                }
            }

            if (allModes.Count <= 1)
            {
                return;
            }

            int idx = allModes.IndexOf(CurrentMode);
            CurrentMode = allModes[(idx + 1) % allModes.Count];
        }

        /// <summary>
        /// 切换模式下重置射击发数计数（后坐力模式从第 0 发重新开始）。
        /// </summary>
        public void ResetShotIndex()
        {
            ShotIndex = 0;
        }

        // ==================== 聚合属性（基础值 × 配件修正） ====================

        /// <summary>
        /// 聚合弹匣容量 = 基础容量 + 所有弹匣配件加成。
        /// </summary>
        public int MagazineSize
        {
            get
            {
                int bonus = 0;
                if (_attachments.TryGetValue(AttachmentType.Magazine, out var mag))
                {
                    bonus += mag.magazineBonus;
                }

                return Data.magazineSize + bonus;
            }
        }

        /// <summary>
        /// 聚合伤害 = 基础伤害（爆头倍率在命中时另行计算）。
        /// </summary>
        public float Damage => Data.damage;

        /// <summary>
        /// 聚合后坐力乘数 = 各配件后坐力乘数连乘（枪口/枪托）。
        /// </summary>
        public float RecoilMultiplier
        {
            get
            {
                float m = 1f;
                TryApply(AttachmentType.Barrel, a => m *= a.recoilMultiplier);
                TryApply(AttachmentType.Stock, a => m *= a.recoilMultiplier);
                return m;
            }
        }

        /// <summary>
        /// 聚合散布乘数 = 各配件散布乘数连乘（枪口/激光）。
        /// </summary>
        public float SpreadMultiplier
        {
            get
            {
                float m = 1f;
                TryApply(AttachmentType.Barrel, a => m *= a.spreadMultiplier);
                TryApply(AttachmentType.Laser, a => m *= a.spreadMultiplier);
                return m;
            }
        }

        /// <summary>
        /// 聚合换弹时间乘数（弹匣配件加速换弹）。
        /// </summary>
        public float ReloadMultiplier
        {
            get
            {
                float m = 1f;
                TryApply(AttachmentType.Magazine, a => m *= a.reloadMultiplier);
                return m;
            }
        }

        /// <summary>
        /// 聚合 ADS 时间乘数（枪托加速开镜）。
        /// </summary>
        public float AdsTimeMultiplier
        {
            get
            {
                float m = 1f;
                TryApply(AttachmentType.Stock, a => m *= a.adsTimeMultiplier);
                return m;
            }
        }

        /// <summary>
        /// 聚合爆头倍率额外加成（HopUp 如 Skullpiercer）。
        /// </summary>
        public float HeadshotMultiplierBonus
        {
            get
            {
                float b = 0f;
                TryApply(AttachmentType.HopUp, a => b += a.headshotMultiplierBonus);
                return b;
            }
        }

        /// <summary>
        /// 按类型计算换弹时间（战术 vs 空仓）× 配件修正。
        /// </summary>
        public float GetReloadTime(ReloadType reloadType)
        {
            float baseTime = reloadType == ReloadType.Empty
                ? Data.emptyReloadTime
                : Data.tacticalReloadTime;
            return baseTime * ReloadMultiplier;
        }

        /// <summary>
        /// 当前应执行的换弹类型：弹匣空则空仓换弹，否则战术换弹。
        /// </summary>
        public ReloadType CurrentReloadType => CurrentAmmo <= 0 ? ReloadType.Empty : ReloadType.Tactical;

        /// <summary>
        /// 辅助方法：若指定槽位有配件，则对其执行委托。
        /// </summary>
        private void TryApply(AttachmentType slot, System.Action<AttachmentData> apply)
        {
            if (_attachments.TryGetValue(slot, out var attachment))
            {
                apply(attachment);
            }
        }
    }
}
