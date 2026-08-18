/*
 * ============================================================================
 * 文件：WeaponRecoil.cs
 * 用途：枪械后坐力动画（纯代码实现，不依赖 Animator 动画文件）。
 *       每次开火时给枪身一个"向后缩 + 枪口上抬 + 随机水平偏转"的冲击，
 *       之后按 recoverSpeed 指数回弹到原始姿态，形成完整的"后坐→回位"动画。
 *       挂在枪械模型根节点上，由 WeaponFireTest / WeaponController 在开火时调用
 *       TriggerRecoil() 触发；这是给枪械添加的后坐力代码本体。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 枪械后坐力动画控制器：
    /// - 位置后坐：枪身沿自身 Z 轴向后缩（kickBackDistance）
    /// - 旋转后坐：枪口上抬（kickUpAngle）+ 随机水平偏转（randomYawRange）
    /// - 回弹动画：停止施加后每帧向初始姿态指数收敛（recoverSpeed）
    /// 所有偏移都是"累计量"，连射时多次 TriggerRecoil 会叠加，松开后一起回弹。
    /// </summary>
    public class WeaponRecoil : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("要做后坐力动画的枪身 Transform（留空 = 本组件所在物体）")]
        [SerializeField] private Transform recoilTransform;

        [Header("后坐力参数")]
        [Tooltip("后坐时枪身后退的距离（米）")]
        [SerializeField] private float kickBackDistance = 0.06f;

        [Tooltip("后坐时枪口上抬的角度（度）")]
        [SerializeField] private float kickUpAngle = 3f;

        [Tooltip("后坐时随机水平偏转的幅度（度），左右随机")]
        [SerializeField] private float randomYawRange = 0.8f;

        [Tooltip("回弹速度（越大回得越快，0 = 不回弹）")]
        [SerializeField] private float recoverSpeed = 10f;

        // 当前累计的位置偏移（每帧向 0 收敛）
        private Vector3 _positionOffset;

        // 当前累计的旋转偏移（欧拉角，每帧向 0 收敛）
        private Vector3 _rotationOffset;

        // 初始姿态（回弹基准，Awake 时记录）
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;

        private void Awake()
        {
            if (recoilTransform == null)
            {
                recoilTransform = transform;
            }

            _baseLocalPosition = recoilTransform.localPosition;
            _baseLocalRotation = recoilTransform.localRotation;
        }

        /// <summary>
        /// 触发一发后坐力（每次开火成功后调用一次）。
        /// 位置向后缩 + 枪口上抬 + 随机水平偏转，全部累加到当前偏移上。
        /// </summary>
        public void TriggerRecoil()
        {
            _positionOffset += new Vector3(0f, 0f, -kickBackDistance);
            _rotationOffset += new Vector3(
                -kickUpAngle,
                UnityEngine.Random.Range(-randomYawRange, randomYawRange),
                0f);
        }

        /// <summary>
        /// 立即复位后坐力偏移（换弹/切枪/重开时调用，可选）。
        /// </summary>
        public void ResetRecoil()
        {
            _positionOffset = Vector3.zero;
            _rotationOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (recoilTransform == null)
            {
                return;
            }

            // 指数衰减回弹：冲击后每帧向初始姿态收敛，形成"后坐→回位"动画
            float t = 1f - Mathf.Exp(-recoverSpeed * Time.deltaTime);
            _positionOffset = Vector3.Lerp(_positionOffset, Vector3.zero, t);
            _rotationOffset = Vector3.Lerp(_rotationOffset, Vector3.zero, t);

            recoilTransform.localPosition = _baseLocalPosition + _positionOffset;
            recoilTransform.localRotation = _baseLocalRotation * Quaternion.Euler(_rotationOffset);
        }
    }
}
