/*
 * ============================================================================
 * 文件：WeaponContentGenerator.cs
 * 用途：编辑器工具（Editor 专属）。
 *       通过 Unity 菜单一键生成整套示例数据资产：
 *       - 5 种弹药（轻/重/能量/狙击/霰弹）
 *       - 3 把示例武器（R-301 / EVA-8 / Wingman，对齐 APEX 手感）
 *       - 一批配件（弹匣/枪托/枪口/瞄具/激光/HopUp）
 *       - 对应的后坐力模式与弹道数据
 *       方便开发者快速搭起可跑通的武器系统 Demo。
 * 所属：CharacterController.FPS.Weapon.Editor（Editor 层）
 * ============================================================================
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CharacterController.FPS.Weapon.Editor
{
    /// <summary>
    /// 示例资产生成器：菜单路径 FPS/Weapon/...。
    /// 运行前自动创建 Assets/WeaponContent/ 目录并写入全部资产。
    /// </summary>
    public static class WeaponContentGenerator
    {
        /// <summary>资产输出根目录</summary>
        private const string RootFolder = "Assets/WeaponContent";

        // ==================== 菜单入口 ====================

        [MenuItem("FPS/Weapon/一键生成示例武器内容")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);

            // 1. 弹药
            var lightAmmo = CreateAmmo("弹药_轻型", AmmoType.Light, 60);
            var heavyAmmo = CreateAmmo("弹药_重型", AmmoType.Heavy, 60);
            var energyAmmo = CreateAmmo("弹药_能量", AmmoType.Energy, 60);
            var sniperAmmo = CreateAmmo("弹药_狙击", AmmoType.Sniper, 40);
            var shotgunAmmo = CreateAmmo("弹药_霰弹", AmmoType.Shotgun, 48);

            // 2. 弹道
            var hitscanBallistic = CreateBallistic("弹道_即时命中", isHitscan: true, speed: 0f, pellets: 1);
            var shotgunBallistic = CreateBallistic("弹道_霰弹", isHitscan: true, speed: 0f, pellets: 8);
            var sniperBallistic = CreateBallistic("弹道_狙击弹丸", isHitscan: false, speed: 200f, pellets: 1);

            // 3. 后坐力模式
            var rifleRecoil = CreateRecoil("后坐力_步枪", 0.8f, 0.5f);
            var shotgunRecoil = CreateRecoil("后坐力_霰弹", 3.0f, 0.8f);
            var pistolRecoil = CreateRecoil("后坐力_手枪", 1.2f, 0.6f);

            // 4. 武器
            CreateWeapon("武器_R301", "R-301 卡宾枪", lightAmmo, hitscanBallistic, rifleRecoil,
                new[] { FireModeType.SemiAuto, FireModeType.FullAuto },
                FireModeType.FullAuto, 600f, 18, damage: 14f, hipSpread: 2.5f);

            CreateWeapon("武器_EVA8", "EVA-8 自动霰弹枪", shotgunAmmo, shotgunBallistic, shotgunRecoil,
                new[] { FireModeType.SemiAuto },
                FireModeType.SemiAuto, 210f, 8, damage: 9f, hipSpread: 4f);

            CreateWeapon("武器_Wingman", "Wingman 左轮", heavyAmmo, sniperBallistic, pistolRecoil,
                new[] { FireModeType.SemiAuto },
                FireModeType.SemiAuto, 150f, 6, damage: 45f, hipSpread: 3f);

            // 5. 配件
            CreateAttachment("配件_标准弹匣", AttachmentType.Magazine, AttachmentRarity.Common,
                magazineBonus: 4, reloadMultiplier: 0.95f);
            CreateAttachment("配件_加长弹匣", AttachmentType.Magazine, AttachmentRarity.Epic,
                magazineBonus: 12, reloadMultiplier: 0.85f);
            CreateAttachment("配件_枪口稳定器", AttachmentType.Barrel, AttachmentRarity.Rare,
                recoilMultiplier: 0.9f, spreadMultiplier: 0.95f);
            CreateAttachment("配件_枪托", AttachmentType.Stock, AttachmentRarity.Rare,
                recoilMultiplier: 0.95f, adsTimeMultiplier: 0.9f);
            CreateAttachment("配件_激光瞄准器", AttachmentType.Laser, AttachmentRarity.Rare,
                spreadMultiplier: 0.8f);
            CreateAttachment("配件_1x全息", AttachmentType.Optic, AttachmentRarity.Common);
            CreateAttachment("配件_转换扳机组", AttachmentType.HopUp, AttachmentRarity.Legendary,
                enablesNewFireMode: true, grantedFireMode: FireModeType.Burst);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WeaponContentGenerator] 示例武器内容已生成到 Assets/WeaponContent/");
        }

        // ==================== 各资产的创建方法 ====================

        /// <summary>创建弹药资产</summary>
        private static AmmoData CreateAmmo(string name, AmmoType type, int maxStack)
        {
            var ammo = ScriptableObject.CreateInstance<AmmoData>();
            ammo.type = type;
            ammo.displayName = name.Replace("弹药_", "") + "弹药";
            ammo.maxStack = maxStack;
            ammo.perPickup = maxStack / 3;
            SaveAsset(ammo, $"{RootFolder}/Ammo/{name}.asset");
            return ammo;
        }

        /// <summary>创建弹道资产</summary>
        private static BallisticData CreateBallistic(string name, bool isHitscan, float speed, int pellets)
        {
            var ballistic = ScriptableObject.CreateInstance<BallisticData>();
            ballistic.isHitscan = isHitscan;
            ballistic.projectileSpeed = speed;
            ballistic.pellets = pellets;
            ballistic.damageFalloffStart = 40f;
            ballistic.damageFalloffEnd = 100f;
            ballistic.minDamageMultiplier = 0.6f;
            SaveAsset(ballistic, $"{RootFolder}/Ballistic/{name}.asset");
            return ballistic;
        }

        /// <summary>创建后坐力模式资产</summary>
        private static RecoilPatternData CreateRecoil(string name, float peakPitch, float randomness)
        {
            var recoil = ScriptableObject.CreateInstance<RecoilPatternData>();
            // 垂直后坐力曲线：前 5 发快速上扬，之后放缓（经典"先快后稳"）
            recoil.verticalPitch = AnimationCurve.Linear(0f, peakPitch, 5f, peakPitch * 0.6f);
            recoil.verticalPitch.AddKey(15f, peakPitch * 0.8f);
            // 水平模式：小幅 S 形
            recoil.horizontalPattern = AnimationCurve.Linear(0f, 0f, 15f, 0f);
            recoil.horizontalPattern.AddKey(5f, 0.3f);
            recoil.horizontalPattern.AddKey(10f, -0.2f);
            recoil.horizontalRandomness = randomness;
            recoil.recoverSpeed = 12f;
            SaveAsset(recoil, $"{RootFolder}/Recoil/{name}.asset");
            return recoil;
        }

        /// <summary>创建武器资产</summary>
        private static WeaponData CreateWeapon(string name, string weaponName, AmmoData ammo,
            BallisticData ballistic, RecoilPatternData recoil,
            FireModeType[] modes, FireModeType defaultMode, float fireRate, int magazineSize,
            float damage, float hipSpread)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = weaponName;
            weapon.ammoType = ammo;
            weapon.damage = damage;
            weapon.headshotMultiplier = 1.5f;
            weapon.ballistic = ballistic;
            weapon.recoilPattern = recoil;

            foreach (var mode in modes)
            {
                weapon.supportedModes.Add(mode);
            }

            weapon.defaultMode = defaultMode;
            weapon.fireRate = fireRate;
            weapon.burstCount = 3;
            weapon.magazineSize = magazineSize;
            weapon.tacticalReloadTime = 2.0f;
            weapon.emptyReloadTime = 2.6f;
            weapon.hipSpread = hipSpread;
            weapon.adsSpread = 0.2f;

            // 标准四槽：瞄具/枪口/弹匣/枪托（霰弹枪换成激光槽）
            if (ammo.type == AmmoType.Shotgun)
            {
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Optic,
                    maxRarity = AttachmentRarity.Legendary
                });
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Laser,
                    maxRarity = AttachmentRarity.Legendary
                });
            }
            else
            {
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Optic,
                    maxRarity = AttachmentRarity.Legendary
                });
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Barrel,
                    maxRarity = AttachmentRarity.Legendary
                });
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Magazine,
                    maxRarity = AttachmentRarity.Legendary
                });
                weapon.attachmentSlots.Add(new WeaponData.AttachmentSlotDef
                {
                    type = AttachmentType.Stock,
                    maxRarity = AttachmentRarity.Legendary
                });
            }

            SaveAsset(weapon, $"{RootFolder}/Weapons/{name}.asset");
            return weapon;
        }

        /// <summary>创建配件资产</summary>
        private static AttachmentData CreateAttachment(string name, AttachmentType slot,
            AttachmentRarity rarity, int magazineBonus = 0, float reloadMultiplier = 1f,
            float recoilMultiplier = 1f, float spreadMultiplier = 1f, float adsTimeMultiplier = 1f,
            bool enablesNewFireMode = false, FireModeType grantedFireMode = FireModeType.SemiAuto)
        {
            var attachment = ScriptableObject.CreateInstance<AttachmentData>();
            attachment.slot = slot;
            attachment.rarity = rarity;
            attachment.displayName = name.Replace("配件_", "");
            attachment.magazineBonus = magazineBonus;
            attachment.reloadMultiplier = reloadMultiplier;
            attachment.recoilMultiplier = recoilMultiplier;
            attachment.spreadMultiplier = spreadMultiplier;
            attachment.adsTimeMultiplier = adsTimeMultiplier;
            attachment.enablesNewFireMode = enablesNewFireMode;
            attachment.grantedFireMode = grantedFireMode;
            SaveAsset(attachment, $"{RootFolder}/Attachments/{name}.asset");
            return attachment;
        }

        // ==================== 工具方法 ====================

        /// <summary>
        /// 保存资产到指定路径（自动建目录）。
        /// </summary>
        private static void SaveAsset(UnityEngine.Object asset, string path)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }

        /// <summary>
        /// 确保文件夹存在（按层级逐级创建）。
        /// AssetDatabase 要求正斜杠路径，这里统一转换。
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            folderPath = folderPath.Replace('\\', '/').TrimEnd('/');

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // 父路径不存在时先递归创建父路径
            int lastSlash = folderPath.LastIndexOf('/');
            string parent = lastSlash > 0 ? folderPath.Substring(0, lastSlash) : "Assets";
            string leaf = lastSlash >= 0 ? folderPath.Substring(lastSlash + 1) : folderPath;

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
