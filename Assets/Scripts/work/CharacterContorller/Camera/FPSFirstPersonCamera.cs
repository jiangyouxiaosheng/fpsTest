using UnityEngine;

namespace CharacterController.FPS
{
    /// <summary>
    /// FPS 第一人称相机：
    /// - 根据鼠标输入旋转角色 yaw 和相机 pitch
    /// - 在 LateUpdate 中跟随角色到眼睛高度
    /// - 参数通过 <see cref="LookParameters"/> 在 Inspector 中配置
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FPSFirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private CharacterActor characterActor;
        [SerializeField] private CharacterBrain characterBrain;
        [SerializeField] private LookParameters lookParameters = new LookParameters();

        private float yaw;
        private float pitch;
        private float yawVelocity;
        private float pitchVelocity;
        private bool cursorLockedByUs;

        public CharacterActor CharacterActor
        {
            get => characterActor;
            set => characterActor = value;
        }

        public CharacterBrain CharacterBrain
        {
            get => characterBrain;
            set => characterBrain = value;
        }

        public LookParameters LookParameters => lookParameters;

        private void OnEnable()
        {
            if (lookParameters.lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorLockedByUs = true;
            }
        }

        private void OnDisable()
        {
            if (cursorLockedByUs)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorLockedByUs = false;
            }
        }

        private void Start()
        {
            if (characterActor == null)
            {
                characterActor = FindAnyObjectByType<CharacterActor>();
            }

            if (characterBrain == null)
            {
                characterBrain = FindAnyObjectByType<CharacterBrain>();
            }

            if (characterActor != null)
            {
                yaw = GetCharacterYaw();
            }

            pitch = NormalizeAngle(transform.eulerAngles.x);
        }

        private void LateUpdate()
        {
            if (characterActor == null || characterBrain == null)
            {
                return;
            }

            ApplyLook(characterBrain.LookInput);
            FollowCharacter();
        }

        private void ApplyLook(Vector2 lookInput)
        {
            float currentYaw = GetCharacterYaw();
            float deltaYaw = lookInput.x * lookParameters.sensitivity.x;
            float targetYaw = currentYaw + deltaYaw;

            float deltaPitch = (lookParameters.invertY ? lookInput.y : -lookInput.y)
                               * lookParameters.sensitivity.y;
            float targetPitch = Mathf.Clamp(
                pitch + deltaPitch,
                lookParameters.minPitch,
                lookParameters.maxPitch);

            if (lookParameters.smoothTime > 0f)
            {
                yaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, lookParameters.smoothTime);
                pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookParameters.smoothTime);
                pitch = Mathf.Clamp(pitch, lookParameters.minPitch, lookParameters.maxPitch);
            }
            else
            {
                yaw = targetYaw;
                pitch = targetPitch;
                yawVelocity = 0f;
                pitchVelocity = 0f;
            }

            // 只把水平旋转同步给角色，俯仰角只留在相机上。
            float yawDelta = Mathf.DeltaAngle(currentYaw, yaw);
            if (Mathf.Abs(yawDelta) > 0.0001f)
            {
                characterActor.RotateYaw(yawDelta);
            }
        }

        private void FollowCharacter()
        {
            if (transform.parent == characterActor.transform)
            {
                transform.localPosition = new Vector3(0f, lookParameters.eyeHeight, 0f);
                transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else
            {
                // 使用 transform.position 而不是 CharacterActor.Position：
                // CharacterActor.Position 是物理刚体的原始位置，而 transform.position 是经过插值后的视觉位置。
                // 相机跟随插值后的位置可以避免角色移动时画面抖动。
                transform.position = characterActor.transform.position
                                     + characterActor.transform.up * lookParameters.eyeHeight;
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        private float GetCharacterYaw()
        {
            Vector3 forward = Vector3.ProjectOnPlane(characterActor.Forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }
            else if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
    }
}
