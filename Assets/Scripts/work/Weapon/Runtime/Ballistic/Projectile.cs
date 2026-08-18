/*
 * ============================================================================
 * 文件：Projectile.cs
 * 用途：实体弹丸（MonoBehaviour）。
 *       用于能量/狙击等需要"飞行时间 + 下坠"的武器（APEX 弹道制）。
 *       由 WeaponController 在开火时生成：携带速度与重力，飞行中做连续
 *       碰撞检测，命中物体后施加伤害并自动销毁。
 * 所属：CharacterController.FPS.Weapon.Ballistic（Runtime 层）
 * ============================================================================
 */

using CharacterController.FPS.Weapon.Damage;
using UnityEngine;

namespace CharacterController.FPS.Weapon.Ballistic
{
    /// <summary>
    /// 实体弹丸：有速度、重力与生命周期。
    /// 使用"上一帧位置→本帧位置"的连续射线检测，避免高速穿透薄墙。
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("弹道参数（由生成方注入）")]
        [Tooltip("飞行速度（米/秒）")]
        public float speed = 300f;

        [Tooltip("重力影响系数（0 = 无下坠）")]
        public float gravityScale = 0f;

        [Tooltip("最大存活时间（秒），超时自动销毁")]
        public float maxLifetime = 3f;

        [Tooltip("基础伤害")]
        public float damage = 20f;

        [Tooltip("爆头倍率")]
        public float headshotMultiplier = 1.5f;

        [Tooltip("伤害来源（开枪者）")]
        public GameObject source;

        [Tooltip("可命中层级")]
        public LayerMask hitMask;

        [Tooltip("是否生成拖尾（弹道可见，适合远距离观察弹道）")]
        public bool spawnTrail = false;

        /// <summary>是否已命中（防止重复伤害）</summary>
        private bool _hasHit;

        /// <summary>当前速度向量（含重力累积）</summary>
        private Vector3 _velocity;

        /// <summary>出生时间（计时生命周期）</summary>
        private float _bornTime;

        /// <summary>上一帧位置（用于连续碰撞检测）</summary>
        private Vector3 _lastPosition;

        /// <summary>运行时创建的拖尾材质（销毁时释放，避免泄漏）</summary>
        private Material _trailMaterial;

        /// <summary>
        /// 初始化弹丸并开始飞行（生成方调用）。
        /// </summary>
        public void Launch(Vector3 origin, Vector3 direction)
        {
            transform.position = origin;
            _velocity = direction.normalized * speed;
            _lastPosition = origin;
            _bornTime = Time.time;

            // 可选：生成拖尾，让远距离弹道肉眼可见（类似曳光弹）
            if (spawnTrail)
            {
                SetupTrail();
            }
        }

        /// <summary>
        /// 生成拖尾（Tracer）：在弹丸上挂 TrailRenderer，
        /// 并基于子弹自身材质复制一份自发光材质，保证 URP 下清晰可见。
        /// 拖尾长度 = trail.time × speed（速度越快、time 越大，可见弹道越长）。
        /// </summary>
        private void SetupTrail()
        {
            var trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            trail.time = 0.2f;               // 拖尾持续时长（秒）
            trail.minVertexDistance = 0.08f; // 顶点最小间距（防止高速飞行的断线）
            trail.widthMultiplier = 0.04f;   // 拖尾宽度
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.alignment = LineAlignment.View;

            // 复制子弹材质并开启自发光，形成发光的弹道线
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                _trailMaterial = new Material(meshRenderer.sharedMaterial);
                _trailMaterial.EnableKeyword("_EMISSION");
                _trailMaterial.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.4f) * 2f);
                trail.sharedMaterial = _trailMaterial;
            }
        }

        /// <summary>弹丸销毁时释放运行时创建的拖尾材质</summary>
        private void OnDestroy()
        {
            if (_trailMaterial != null)
            {
                Destroy(_trailMaterial);
            }
        }

        private void Update()
        {
            if (_hasHit)
            {
                return;
            }

            // 生命周期检查
            if (Time.time - _bornTime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 施加重力（下坠）
            _velocity += Physics.gravity * gravityScale * Time.deltaTime;

            // 计算本帧位移，做连续碰撞检测
            Vector3 newPosition = transform.position + _velocity * Time.deltaTime;
            float stepDistance = Vector3.Distance(_lastPosition, newPosition);

            if (Physics.Raycast(_lastPosition, _velocity.normalized, out RaycastHit hit,
                    stepDistance, hitMask))
            {
                OnHit(hit);
                return;
            }

            _lastPosition = newPosition;
            transform.position = newPosition;
        }

        /// <summary>
        /// 命中处理：施加伤害 + 销毁弹丸。
        /// </summary>
        private void OnHit(RaycastHit hit)
        {
            _hasHit = true;

            // 爆头判定：命中碰撞体或其父级挂有 HeadshotMarker 标记
            bool isHeadshot = hit.collider.GetComponentInParent<HeadshotMarker>() != null;
            float finalDamage = isHeadshot ? damage * headshotMultiplier : damage;

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var info = new DamageInfo(
                    finalDamage,
                    isHeadshot ? DamageType.Headshot : DamageType.Bullet,
                    hit.point,
                    hit.normal,
                    source);
                damageable.TakeDamage(info);
            }

            // 命中特效/音效扩展点：在此触发池化特效

            Destroy(gameObject);
        }
    }
}
