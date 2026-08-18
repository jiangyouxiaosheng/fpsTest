/*
 * ============================================================================
 * 文件：AttachmentData.cs
 * 用途：配件数据资产（ScriptableObject）。
 *       定义一种配件属于哪个槽、什么稀有度，以及它的数值修正
 *       （弹匣加成、换弹/后坐力/散布/ADS 乘数）和特殊效果（HopUp）。
 *       运行时由 AttachmentSystem 装配到武器，WeaponInstance 聚合生效。
 * 所属：CharacterController.FPS.Weapon（Data 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 配件数据。一个资产 = 一种配件（如"标准弹匣"、"紫色枪托"）。
    /// 数值修正采用"乘数/加成"方式：武器基础值 × 所有已装配件修正。
    /// </summary>
    [CreateAssetMenu(menuName = "FPS/Weapon/Attachment Data", fileName = "Attachment_New")]
    public class AttachmentData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("所属配件槽（瞄具/枪口/弹匣/枪托/激光/HopUp）")]
        public AttachmentType slot;

        [Tooltip("稀有度等级（白/蓝/紫/金）")]
        public AttachmentRarity rarity;

        [Tooltip("配件显示名，如'标准弹匣'")]
        public string displayName = "新配件";

        [Tooltip("背包/装配 UI 图标")]
        public Sprite icon;

        [Header("数值修正（基础值 × 乘数 / + 加成）")]
        [Tooltip("弹匣容量 +N 发")]
        public int magazineBonus;

        [Tooltip("换弹时间乘数（0.85 = 快 15%）")]
        [Range(0.1f, 2f)]
        public float reloadMultiplier = 1f;

        [Tooltip("后坐力乘数（0.8 = 减少 20%）")]
        [Range(0.1f, 2f)]
        public float recoilMultiplier = 1f;

        [Tooltip("散布乘数（0.8 = 更精准 20%）")]
        [Range(0.1f, 2f)]
        public float spreadMultiplier = 1f;

        [Tooltip("ADS 时间乘数（0.85 = 开镜快 15%）")]
        [Range(0.1f, 2f)]
        public float adsTimeMultiplier = 1f;

        [Tooltip("镜头稳定加成（越大越稳，抵消后坐力抖动）")]
        [Range(0f, 1f)]
        public float stabilityBonus = 0f;

        [Tooltip("爆头倍率额外加成（如骷髅穿膛者 Skullpiercer）")]
        public float headshotMultiplierBonus = 0f;

        [Header("特殊效果（HopUp 专用）")]
        [Tooltip("是否赋予新射击模式（如 Select Fire 转换扳机组）")]
        public bool enablesNewFireMode;

        [Tooltip("赋予的新射击模式（enablesNewFireMode = true 时生效）")]
        public FireModeType grantedFireMode = FireModeType.SemiAuto;

        [Tooltip("通用特殊数值（如 Turbocharger 的预热倍率、伤害加成等）")]
        public float specialValue = 0f;
    }
}
