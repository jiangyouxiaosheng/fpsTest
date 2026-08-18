/*
 * ============================================================================
 * 文件：WeaponEnums.cs
 * 用途：武器系统的基础枚举定义。
 *       包含弹药类型、射击模式、配件槽位、配件稀有度、武器槽位、
 *       武器状态与换弹类型，是整套武器系统的"词汇表"。
 * 所属：CharacterController.FPS.Weapon（Core 层）
 * ============================================================================
 */

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 弹药类型（对齐 APEX：轻/重/能量/狙击/霰弹 + 特殊）。
    /// 每种弹药是独立资源，由 AmmoData ScriptableObject 定义细节；
    /// 玩家背包（AmmoInventory）按此枚举计数。
    /// </summary>
    public enum AmmoType
    {
        Light,      // 轻型弹药：R-301 / R-99 / P2020 / G7 等
        Heavy,      // 重型弹药：Flatline / Hemlok / Wingman / 30-30 等
        Energy,     // 能量弹药：Volt / Havoc / L-STAR / Nemesis 等
        Sniper,     // 狙击弹药：Longbow / Sentinel / Charge Rifle 等
        Shotgun,    // 霰弹弹药：EVA-8 / Peacekeeper / Mastiff 等
        Special     // 特殊弹药（如 Kraber 的专属弹药、Thermite 等）
    }

    /// <summary>
    /// 射击模式（APEX 中部分武器可切换，如 R-301 / Hemlok / Flatline）。
    /// WeaponData.supportedModes 决定一把武器支持哪些模式。
    /// </summary>
    public enum FireModeType
    {
        SemiAuto,   // 单发：每次按下扳机发射 1 发
        Burst,      // 连发：一次按下发射 N 发（Hemlok 3 连发、Prowler 5 连发）
        FullAuto,   // 全自动：按住扳机持续发射
        Charge      // 充能：按住蓄力、松开释放（Charge Rifle / L-STAR 预热）
    }

    /// <summary>
    /// 配件槽位类型（APEX 六类配件槽）。
    /// 一把武器通过 WeaponData.attachmentSlots 声明自己拥有哪些槽。
    /// </summary>
    public enum AttachmentType
    {
        Optic,      // 瞄具：变焦 1x/2x/3x/4-8x，无等级
        Barrel,     // 枪口/枪管：降低后坐力与枪口火光
        Magazine,   // 弹匣：提升弹容量、加快换弹
        Stock,      // 枪托：降低后坐力、加快 ADS、提升稳定度
        Laser,      // 激光瞄准器：降低腰射散布（霰弹枪专属）
        HopUp       // 特殊配件：改变武器机制（Select Fire / Turbocharger 等）
    }

    /// <summary>
    /// 配件稀有度/等级（对齐 APEX：白→蓝→紫→金，金有独特效果）。
    /// </summary>
    public enum AttachmentRarity
    {
        Common,     // 白色 1 级
        Rare,       // 蓝色 2 级
        Epic,       // 紫色 3 级
        Legendary   // 金色（独特被动，如无限备弹、双重击等）
    }

    /// <summary>
    /// 武器槽位：主武器 / 副武器（APEX 双持两把武器）。
    /// </summary>
    public enum WeaponSlotType
    {
        Primary,    // 主武器槽（1 号位）
        Secondary   // 副武器槽（2 号位）
    }

    /// <summary>
    /// 武器当前状态，供 HUD、动画与逻辑判断使用。
    /// </summary>
    public enum WeaponState
    {
        Idle,       // 待机
        Firing,     // 开火中
        Reloading,  // 换弹中
        Switching,  // 切换武器中
        Ads         // 开镜瞄准中
    }

    /// <summary>
    /// 换弹类型（APEX：空仓换弹更慢，战术换弹更快）。
    /// </summary>
    public enum ReloadType
    {
        Tactical,   // 战术换弹（弹匣内还有剩余子弹，动作快）
        Empty       // 空仓换弹（弹匣打空，动作慢）
    }
}
