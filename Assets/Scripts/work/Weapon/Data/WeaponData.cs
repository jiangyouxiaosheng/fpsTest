/*
 * ============================================================================
 * 文件：WeaponData.cs
 * 用途：武器静态数据资产（ScriptableObject），是整套武器系统的核心数据载体。
 *       每把枪一个资产：伤害、射速、弹匣、换弹、精度、后坐力、弹道、
 *       支持的射击模式与配件槽位全部在这里配置，运行时零硬编码。
 * 所属：CharacterController.FPS.Weapon（Data 层）
 * ============================================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 武器静态数据。新增一把武器 = 新建一个此资产并填表，无需改代码。
    /// 运行时由 WeaponInstance 持有并基于它计算"基础值 × 配件修正"。
    /// </summary>
    [CreateAssetMenu(menuName = "FPS/Weapon/Weapon Data", fileName = "Weapon_New")]
    public class WeaponData : ScriptableObject
    {
        /// <summary>
        /// 单个配件槽的定义：槽位类型 + 该槽最高可装的稀有度等级。
        /// </summary>
        [Serializable]
        public class AttachmentSlotDef
        {
            [Tooltip("槽位类型（瞄具/枪口/弹匣/枪托/激光/HopUp）")]
            public AttachmentType type;

            [Tooltip("该槽最高可装的稀有度（Common=白 … Legendary=金）")]
            public AttachmentRarity maxRarity = AttachmentRarity.Legendary;
        }

        [Header("基础信息")]
        [Tooltip("武器名称，如 R-301 卡宾枪")]
        public string weaponName = "新武器";

        [Tooltip("该武器使用的弹药类型（决定吃哪种子弹）")]
        public AmmoData ammoType;

        [Header("伤害")]
        [Tooltip("单发基础伤害")]
        public float damage = 18f;

        [Tooltip("爆头伤害倍率（基础伤害 × 此值）")]
        public float headshotMultiplier = 1.5f;

        [Header("射击模式")]
        [Tooltip("支持的射击模式列表（如 [单发, 全自动] 表示可切换）")]
        public List<FireModeType> supportedModes = new() { FireModeType.FullAuto };

        [Tooltip("默认使用的射击模式")]
        public FireModeType defaultMode = FireModeType.FullAuto;

        [Tooltip("射速：发/分钟（RPM）")]
        public float fireRate = 600f;

        [Tooltip("连发模式下每次扣动扳机发射的子弹数（Hemlok=3）")]
        public int burstCount = 3;

        [Tooltip("连发模式内每发之间的间隔（秒）")]
        public float burstInterval = 0.08f;

        [Header("弹匣与换弹")]
        [Tooltip("弹匣容量（发）")]
        public int magazineSize = 18;

        [Tooltip("战术换弹时间（秒）：弹匣还有子弹时")]
        public float tacticalReloadTime = 2.0f;

        [Tooltip("空仓换弹时间（秒）：弹匣打空时")]
        public float emptyReloadTime = 2.6f;

        [Header("精度")]
        [Tooltip("腰射散布（角度）")]
        public float hipSpread = 3f;

        [Tooltip("开镜散布（角度）")]
        public float adsSpread = 0.2f;

        [Tooltip("散布恢复速度（越大越快回到最小值）")]
        public float spreadRecoverySpeed = 20f;

        [Tooltip("开镜所需时间（秒）")]
        public float adsTime = 0.25f;

        [Header("后坐力")]
        [Tooltip("后坐力模式资产（垂直曲线 + 水平模式）")]
        public RecoilPatternData recoilPattern;

        [Header("弹道")]
        [Tooltip("弹道数据资产（即时命中 or 弹丸、速度、衰减、弹丸数）")]
        public BallisticData ballistic;

        [Header("配件槽")]
        [Tooltip("本武器拥有的配件槽列表（如 瞄具+枪口+弹匣+枪托）")]
        public List<AttachmentSlotDef> attachmentSlots = new();
    }
}
