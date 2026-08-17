using UnityEngine;

namespace CharacterController
{
    /// <summary>
    /// 第一人称视角参数，可在 Inspector 中像移动参数一样配置。
    /// </summary>
    [System.Serializable]
    public class LookParameters
    {
        [Header("Mouse Look")]
        public Vector2 sensitivity = new Vector2(2f, 2f);
        public bool invertY = false;

        [Min(0f)]
        public float smoothTime = 0.08f;

        [Header("Pitch Limits")]
        public float minPitch = -89f;

        public float maxPitch = 89f;

        [Header("Camera")]
        public float eyeHeight = 1.6f;
        public bool lockCursor = true;
    }
}
