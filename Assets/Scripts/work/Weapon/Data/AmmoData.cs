/*
 * ============================================================================
 * 文件：AmmoData.cs
 * 用途：弹药类型数据资产（ScriptableObject）。
 *       定义一种弹药的显示名、图标、堆叠上限与拾取数量，
 *       武器通过 WeaponData.ammoType 引用它来声明"吃哪种子弹"。
 * 所属：CharacterController.FPS.Weapon（Data 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 弹药数据资产。每种弹药类型（轻/重/能量/狙击/霰弹/特殊）一个资产，
    /// 玩家背包（AmmoInventory）按 AmmoData 引用计数。
    /// </summary>
    [CreateAssetMenu(menuName = "FPS/Weapon/Ammo Data", fileName = "Ammo_New")]
    public class AmmoData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("弹药枚举类型，用于背包按类型计数")]
        public AmmoType type;

        [Tooltip("弹药显示名称，如'轻型弹药'")]
        public string displayName = "轻型弹药";

        [Tooltip("背包图标")]
        public Sprite icon;

        [Header("拾取与堆叠")]
        [Tooltip("每格最大堆叠数量（APEX 轻弹 60/格）")]
        public int maxStack = 60;

        [Tooltip("单次拾取获得的数量")]
        public int perPickup = 20;

        [Tooltip("是否为特殊弹药（如 Kraber 专属弹，只能由该武器使用）")]
        public bool isSpecial;
    }
}
