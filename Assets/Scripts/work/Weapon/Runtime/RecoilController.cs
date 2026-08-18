/*
 * ============================================================================
 * 文件：RecoilController.cs
 * 用途：后坐力控制器（MonoBehaviour，挂在相机/枪械根节点上）。
 *       根据 RecoilPatternData 的曲线，按"当前第几发"计算本发后坐力偏移
 *       （垂直上抬 + 水平模式偏移 + 随机扰动），并把累计偏移应用到相机旋转；
 *       停止射击后按 recoverSpeed 平滑回弹。
 * 所属：CharacterController.FPS.Weapon（Runtime 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 后坐力控制器：模拟 APEX 的"固定后坐力模式 + 水平随机 + 准星回弹"。
    /// WeaponController 在每次开火时调用 ApplyRecoil()，
    /// 本组件每帧把当前累计偏移叠加到相机 localRotation 上。
    /// </summary>
    public class RecoilController : MonoBehaviour
    {
        [Header("应用目标")]
        [Tooltip("后坐力作用到的相机 Transform（留空则用本组件所在物体）")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("后坐力强度总倍率（可全局调节手感）")]
        [SerializeField] private float recoilScale = 1f;

        /// <summary>当前使用的后坐力模式（由 WeaponController 注入）</summary>
        public RecoilPatternData Pattern { get; set; }

        /// <summary>当前射击发数（由 WeaponController 维护）</summary>
        public int ShotIndex { get; set; }

        // 累计的后坐力偏移（角度）
        private float _currentPitch;
        private float _currentYaw;

        // 上一帧应用的偏移（用于计算每帧增量，避免旋转无限累积）
        private float _prevPitch;
        private float _prevYaw;
        private bool _isFiring;

        /// <summary>是否正在射击（开火中不回收，停止后回弹）</summary>
        public bool IsFiring
        {
            get => _isFiring;
            set => _isFiring = value;
        }

        private void Awake()
        {
            if (cameraTransform == null)
            {
                cameraTransform = transform;
            }
        }

        /// <summary>
        /// 本发后坐力：从模式曲线采样 + 水平随机，累加进偏移。
        /// </summary>
        public void ApplyRecoil()
        {
            if (Pattern == null)
            {
                return;
            }

            // 垂直：按射击发数采样固定曲线
            float pitch = Pattern.verticalPitch.Evaluate(ShotIndex) * recoilScale;

            // 水平：模式曲线偏移 + 随机扰动
            float yaw = (Pattern.horizontalPattern.Evaluate(ShotIndex)
                         + Random.Range(-Pattern.horizontalRandomness, Pattern.horizontalRandomness))
                        * recoilScale;

            _currentPitch += pitch;
            _currentYaw += yaw;
        }

        /// <summary>
        /// 重置后坐力偏移与射击发数（换弹/切枪/换模式时调用）。
        /// 先将已应用的偏移还回相机，再清零内部状态。
        /// </summary>
        public void ResetRecoil()
        {
            if (cameraTransform != null)
            {
                // 还回上一帧已应用的偏移，使相机回到开火前姿态
                cameraTransform.localRotation *= Quaternion.Euler(-_prevPitch, -_prevYaw, 0f);
            }

            _currentPitch = 0f;
            _currentYaw = 0f;
            _prevPitch = 0f;
            _prevYaw = 0f;
            ShotIndex = 0;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                return;
            }

            // 停止射击后平滑回弹（当前偏移向 0 收敛，增量自然变为回弹量）
            if (!_isFiring && Pattern != null)
            {
                float recovery = Pattern.recoverSpeed * Time.deltaTime;
                _currentPitch = Mathf.Lerp(_currentPitch, 0f, recovery);
                _currentYaw = Mathf.Lerp(_currentYaw, 0f, recovery);
            }

            // 只应用"本帧偏移 - 上一帧偏移"的增量，避免旋转无限累积
            float deltaPitch = _currentPitch - _prevPitch;
            float deltaYaw = _currentYaw - _prevYaw;
            cameraTransform.localRotation *= Quaternion.Euler(deltaPitch, deltaYaw, 0f);

            _prevPitch = _currentPitch;
            _prevYaw = _currentYaw;
        }
    }
}
