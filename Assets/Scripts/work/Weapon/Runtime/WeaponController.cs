/*
 * ============================================================================
 * 文件：WeaponController.cs
 * 用途：武器系统的主控制器（MonoBehaviour，挂在角色身上）。
 *       串联整套系统：输入（开火/换弹/切枪/ADS/切模式）→ 射击模式策略 →
 *       弹药消耗 → 弹道生成（即时命中/实体弹丸）→ 后坐力应用。
 *       这是唯一需要挂到角色上的"入口组件"。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using System.Collections;
using CharacterController.FPS.Weapon.Ballistic;
using CharacterController.FPS.Weapon.FireMode;
using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 武器控制器：角色持枪入口。
    /// 外部（如 HFSM 的 FPSFireState 或直接读输入）调用
    /// TryFire / StopFire / StartReload / ToggleAds 等公开方法驱动本组件。
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("武器栏：主/副武器 + 弹药背包")]
        [SerializeField] private WeaponInventory inventory;

        [Tooltip("枪口位置（子弹出生点）")]
        [SerializeField] private Transform muzzle;

        [Tooltip("玩家视角相机（射线起点与后坐力作用目标）")]
        [SerializeField] private Camera viewCamera;

        [Tooltip("后坐力控制器（可选）")]
        [SerializeField] private RecoilController recoilController;

        [Header("ADS 开镜")]
        [Tooltip("开镜 FOV")]
        [SerializeField] private float adsFov = 55f;

        [Tooltip("开镜/关镜 FOV 过渡速度")]
        [SerializeField] private float adsLerpSpeed = 12f;

        [Header("弹道")]
        [Tooltip("可命中层级")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("即时命中最大射程（米）")]
        [SerializeField] private float hitscanRange = 500f;

        [Tooltip("实体弹丸预制体（用于能量/狙击武器；为空则用代码生成基础弹丸）")]
        [SerializeField] private Projectile projectilePrefab;

        /// <summary>当前激活的武器实例（可为空）</summary>
        public WeaponInstance Weapon => inventory != null ? inventory.CurrentWeapon : null;

        /// <summary>是否正在开镜</summary>
        public bool IsAds { get; private set; }

        /// <summary>是否正在换弹</summary>
        public bool IsReloading { get; private set; }

        /// <summary>是否按住扳机</summary>
        public bool IsTriggerHeld { get; private set; }

        // 射击模式策略缓存（每种类型一个实例，切换时复用）
        private readonly IFireMode[] _fireModes =
        {
            new SemiAutoFireMode(),
            new BurstFireMode(),
            new FullAutoFireMode(),
        };

        /// <summary>当前射击模式</summary>
        private IFireMode _currentMode;

        /// <summary>射速冷却计时（秒）</summary>
        private float _fireCooldown;

        /// <summary>当前累计散布（随连射增大，停止后恢复）</summary>
        private float _currentSpread;

        /// <summary>原始 FOV（开镜后恢复用）</summary>
        private float _baseFov;

        /// <summary>换弹协程引用（防止重复触发）</summary>
        private Coroutine _reloadRoutine;

        private void Awake()
        {
            if (viewCamera != null)
            {
                _baseFov = viewCamera.fieldOfView;
            }
        }

        private void Update()
        {
            // 推进射速冷却
            if (_fireCooldown > 0f)
            {
                _fireCooldown -= Time.deltaTime;
            }

            // 按住扳机时按当前模式持续发射（全自动）
            // 注意：这里只检查"是否允许进入开火状态"，射速/连发节奏由各模式
            // 与 FireOneShot 内部冷却共同控制，避免冷却未结束时阻止连发推进。
            if (IsTriggerHeld && _currentMode != null && Weapon != null && !IsReloading)
            {
                _currentMode.OnTriggerHold(this, Time.deltaTime);
            }

            // 散布恢复（停止射击后逐渐回到基础值）
            RecoverSpread();

            // ADS FOV 过渡
            UpdateAdsFov();
        }

        // ==================== 输入接口（供 HFSM 状态或直接输入调用） ====================

        /// <summary>
        /// 按下扳机：切换为开火状态并通知当前模式。
        /// </summary>
        public void TryFire()
        {
            if (Weapon == null || IsReloading)
            {
                return;
            }

            IsTriggerHeld = true;
            _currentMode?.OnTriggerDown(this);
        }

        /// <summary>
        /// 松开扳机：停止开火并通知当前模式。
        /// </summary>
        public void StopFire()
        {
            IsTriggerHeld = false;
            _currentMode?.OnTriggerUp(this);

            // 复位后坐力"开火中"标志，使镜头开始回弹
            if (recoilController != null)
            {
                recoilController.IsFiring = false;
            }
        }

        /// <summary>
        /// 开始换弹。弹匣已满或没有备弹时忽略。
        /// </summary>
        public void StartReload()
        {
            if (Weapon == null || IsReloading)
            {
                return;
            }

            if (Weapon.CurrentAmmo >= Weapon.MagazineSize)
            {
                return;
            }

            // 弹匣打空时若无备弹，拒绝空换弹
            var ammoData = Weapon.Data.ammoType;
            if (ammoData != null && inventory.AmmoInventory.GetCount(ammoData) <= 0)
            {
                Debug.Log("[WeaponController] 无备弹，无法换弹");
                return;
            }

            IsReloading = true;
            StopFire();

            if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
            }

            _reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        /// <summary>
        /// 切换开镜状态。
        /// </summary>
        public void ToggleAds(bool ads)
        {
            IsAds = ads;
        }

        /// <summary>
        /// 切换武器槽位（主/副）。
        /// </summary>
        public void SwitchSlot(WeaponSlotType slot)
        {
            if (inventory == null)
            {
                return;
            }

            if (inventory.SwitchTo(slot))
            {
                OnWeaponChanged();
            }
        }

        /// <summary>
        /// 在两个槽位间轮换。
        /// </summary>
        public void CycleSlot()
        {
            if (inventory != null && inventory.CycleSlot())
            {
                OnWeaponChanged();
            }
        }

        /// <summary>
        /// 切换当前武器的射击模式（在支持的模式间轮换）。
        /// </summary>
        public void CycleFireMode()
        {
            if (Weapon == null)
            {
                return;
            }

            Weapon.CycleFireMode();
            SetupFireMode();
        }

        /// <summary>
        /// 拾取武器放入指定槽位（供地面拾取交互调用）。
        /// </summary>
        public void PickupWeapon(WeaponSlotType slot, WeaponData data, int ammoInMagazine)
        {
            if (inventory == null)
            {
                return;
            }

            inventory.EquipWeapon(slot, data, ammoInMagazine);
            OnWeaponChanged();
        }

        /// <summary>
        /// 给当前武器装配配件（供拾取配件/背包 UI 调用）。
        /// </summary>
        public bool EquipAttachment(AttachmentData attachment)
        {
            if (Weapon == null)
            {
                return false;
            }

            bool success = AttachmentSystem.TryEquip(Weapon, attachment);
            if (success && attachment.enablesNewFireMode)
            {
                // HopUp 赋予了新模式，刷新当前模式（若当前模式不再可用则切回默认）
                SetupFireMode();
            }

            return success;
        }

        // ==================== 内部逻辑 ====================

        /// <summary>
        /// 单发开火（由各射击模式调用）。返回是否成功发射。
        /// 执行顺序：状态检查 → 消耗弹药 → 散布计算 → 弹道生成 → 后坐力。
        /// </summary>
        /// <param name="bypassCooldown">
        /// true = 跳过射速冷却（连发模式内部节奏用 burstInterval 控制，
        /// 不受 fireRate 冷却限制，如 Hemlok 三连发）；false = 受射速冷却限制。
        /// </param>
        public bool FireOneShot(bool bypassCooldown = false)
        {
            var weapon = Weapon;
            if (weapon == null || IsReloading)
            {
                return false;
            }

            // 射速冷却检查（连发模式可绕过，由自身节奏控制）
            if (!bypassCooldown && _fireCooldown > 0f)
            {
                return false;
            }

            // 消耗弹药；空匣触发空仓换弹
            if (!weapon.ConsumeAmmo())
            {
                StartReload();
                return false;
            }

            // 重置射速冷却（发/分钟 → 秒/发）
            _fireCooldown = 60f / Mathf.Max(1f, weapon.Data.fireRate);

            // 计算本发方向（含散布）
            // 起点：枪口 → 相机 → 自身位置；方向：相机前向 → 自身前向
            Vector3 origin;
            if (muzzle != null)
            {
                origin = muzzle.position;
            }
            else if (viewCamera != null)
            {
                origin = viewCamera.transform.position;
            }
            else
            {
                origin = transform.position;
            }

            Vector3 direction = viewCamera != null ? viewCamera.transform.forward : transform.forward;
            direction = ApplySpread(direction);

            // 生成弹道（即时命中 or 实体弹丸）
            SpawnBallistic(origin, direction, weapon);

            // 施加后坐力
            if (recoilController != null && weapon.Data.recoilPattern != null)
            {
                recoilController.Pattern = weapon.Data.recoilPattern;
                recoilController.ShotIndex = weapon.ShotIndex;
                recoilController.IsFiring = true;
                recoilController.ApplyRecoil();
            }

            // 开火音效/动画/枪口闪光扩展点：在此触发事件

            return true;
        }

        /// <summary>
        /// 换弹协程：等待换弹时间 → 从备弹转入弹匣。
        /// </summary>
        private IEnumerator ReloadRoutine()
        {
            var weapon = Weapon;
            if (weapon == null)
            {
                IsReloading = false;
                yield break;
            }

            weapon.State = WeaponState.Reloading;

            // 根据当前弹匣状态选择换弹时长（战术快/空仓慢）
            ReloadType reloadType = weapon.CurrentReloadType;
            float reloadTime = weapon.GetReloadTime(reloadType);

            yield return new WaitForSeconds(reloadTime);

            // 从备弹补满弹匣（缺多少补多少）
            int need = weapon.MagazineSize - weapon.CurrentAmmo;
            if (inventory != null && inventory.AmmoInventory != null && weapon.Data.ammoType != null)
            {
                int taken = inventory.AmmoInventory.ConsumeAmmo(weapon.Data.ammoType, need);
                weapon.AddAmmoToMagazine(taken);
            }

            weapon.State = WeaponState.Idle;
            weapon.ResetShotIndex();
            IsReloading = false;
            _reloadRoutine = null;

            // 换弹完成事件扩展点
        }

        /// <summary>
        /// 生成弹道：按 BallisticData 决定走即时命中还是实体弹丸。
        /// </summary>
        private void SpawnBallistic(Vector3 origin, Vector3 direction, WeaponInstance weapon)
        {
            var ballistic = weapon.Data.ballistic;
            if (ballistic == null)
            {
                return;
            }

            float headshotMul = weapon.Data.headshotMultiplier + weapon.HeadshotMultiplierBonus;

            if (ballistic.isHitscan)
            {
                // 即时命中：单发或霰弹多发
                if (ballistic.pellets <= 1)
                {
                    HitscanShot.Fire(origin, direction, hitscanRange, weapon.Damage,
                        ballistic, headshotMul, gameObject, hitMask);
                }
                else
                {
                    HitscanShot.FireSpread(origin, direction, hitscanRange, weapon.Damage,
                        ballistic, headshotMul, gameObject, hitMask,
                        ballistic.pellets, weapon.Data.hipSpread * 0.5f);
                }
            }
            else
            {
                // 实体弹丸：生成并发射
                Projectile projectile = SpawnProjectile(origin);
                if (projectile != null)
                {
                    projectile.speed = ballistic.projectileSpeed;
                    projectile.gravityScale = ballistic.gravityScale;
                    projectile.damage = weapon.Damage;
                    projectile.headshotMultiplier = headshotMul;
                    projectile.source = gameObject;
                    projectile.hitMask = hitMask;
                    projectile.Launch(origin, direction);
                }
            }
        }

        /// <summary>
        /// 生成弹丸对象：优先用预制体，否则代码创建基础弹丸。
        /// </summary>
        private Projectile SpawnProjectile(Vector3 origin)
        {
            if (projectilePrefab != null)
            {
                return Instantiate(projectilePrefab, origin, Quaternion.identity);
            }

            // 代码生成：空物体 + 弹丸组件（无需预制体即可运行）
            var go = new GameObject("Projectile");
            go.transform.position = origin;
            return go.AddComponent<Projectile>();
        }

        /// <summary>
        /// 对射击方向施加当前散布（锥形随机偏移）。
        /// </summary>
        private Vector3 ApplySpread(Vector3 direction)
        {
            var weapon = Weapon;
            if (weapon == null)
            {
                return direction;
            }

            // 基础散布：开镜更准；再乘配件修正
            float baseSpread = IsAds ? weapon.Data.adsSpread : weapon.Data.hipSpread;
            float totalSpread = (baseSpread + _currentSpread) * weapon.SpreadMultiplier;

            if (totalSpread <= 0f)
            {
                return direction;
            }

            // 锥形随机偏移
            Quaternion offset = Quaternion.Euler(
                Random.Range(-totalSpread, totalSpread),
                Random.Range(-totalSpread, totalSpread),
                0f);
            return offset * direction;
        }

        /// <summary>
        /// 散布恢复：未开火时逐渐回到基础值。
        /// </summary>
        private void RecoverSpread()
        {
            if (Weapon == null)
            {
                _currentSpread = 0f;
                return;
            }

            if (!IsTriggerHeld)
            {
                _currentSpread = Mathf.Lerp(_currentSpread, 0f,
                    Weapon.Data.spreadRecoverySpeed * Time.deltaTime);
            }
            else
            {
                // 连射中散布缓慢累积（简单模型：每次开火 +0.5，封顶 5）
                _currentSpread = Mathf.Min(_currentSpread + 0.5f * Time.deltaTime * 10f, 5f);
            }
        }

        /// <summary>
        /// ADS FOV 过渡。
        /// </summary>
        private void UpdateAdsFov()
        {
            if (viewCamera == null || Weapon == null)
            {
                return;
            }

            float target = IsAds ? adsFov : _baseFov;
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, target,
                adsLerpSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 武器变化（切枪/拾取）后：重建射击模式、重置后坐力与状态。
        /// </summary>
        private void OnWeaponChanged()
        {
            // 停止一切进行中的动作
            StopFire();
            if (IsReloading && _reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                IsReloading = false;
                _reloadRoutine = null;
            }

            if (Weapon == null)
            {
                _currentMode = null;
                return;
            }

            // 初始化武器实例状态
            Weapon.State = WeaponState.Idle;
            Weapon.ResetShotIndex();

            // 设置射击模式
            SetupFireMode();

            // 重置后坐力
            if (recoilController != null)
            {
                recoilController.ResetRecoil();
            }
        }

        /// <summary>
        /// 根据武器当前模式建立策略实例。
        /// 若当前模式不在可用列表（如卸下 Select Fire 后），回退到默认模式。
        /// </summary>
        private void SetupFireMode()
        {
            if (Weapon == null)
            {
                return;
            }

            FireModeType mode = Weapon.CurrentMode;

            // 校验模式是否可用（基础支持 或 HopUp 赋予）；不可用则回退默认
            if (!IsModeAvailable(mode))
            {
                mode = Weapon.Data.defaultMode;
                Weapon.SetCurrentMode(mode);
            }

            _currentMode = mode switch
            {
                FireModeType.SemiAuto => _fireModes[0],
                FireModeType.Burst => _fireModes[1],
                FireModeType.FullAuto => _fireModes[2],
                _ => _fireModes[2]
            };

            _currentMode.OnSwitchTo(this);
        }

        /// <summary>
        /// 判断某射击模式对当前武器是否可用（基础支持列表 或 HopUp 赋予）。
        /// </summary>
        private bool IsModeAvailable(FireModeType mode)
        {
            if (Weapon == null)
            {
                return false;
            }

            if (Weapon.Data.supportedModes.Contains(mode))
            {
                return true;
            }

            foreach (var kv in Weapon.Attachments)
            {
                if (kv.Value.enablesNewFireMode && kv.Value.grantedFireMode == mode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
