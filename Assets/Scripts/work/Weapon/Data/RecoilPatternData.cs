/*
 * ============================================================================
 * 文件：RecoilPatternData.cs
 * 用途：后坐力模式数据资产（ScriptableObject）。
 *       用两条 AnimationCurve 描述"第几发 → 上抬/水平偏移"的固定后坐力模式
 *       （APEX 的招牌手感），再加水平随机幅度与回弹速度。
 *       运行时由 RecoilController 按当前射击发数采样。
 * 所属：CharacterController.FPS.Weapon（Data 层）
 * ============================================================================
 */

using UnityEngine;

namespace CharacterController.FPS.Weapon
{
    /// <summary>
    /// 后坐力模式。垂直方向为固定曲线（可背板压枪），
    /// 水平方向为模式曲线 + 随机扰动（增加不确定性）。
    /// 曲线的 X 轴为"第几发"，Y 轴为偏移角度。
    /// </summary>
    [CreateAssetMenu(menuName = "FPS/Weapon/Recoil Pattern", fileName = "Recoil_New")]
    public class RecoilPatternData : ScriptableObject
    {
        [Header("垂直后坐力")]
        [Tooltip("每发上抬角度曲线：X=第几发，Y=上抬角度(度)。可编辑成前 5 发上扬、之后放缓的节奏")]
        public AnimationCurve verticalPitch = AnimationCurve.Linear(0f, 0.6f, 15f, 3f);

        [Header("水平后坐力")]
        [Tooltip("水平偏移模式曲线：X=第几发，Y=左右偏移角度(度)。正值右、负值左")]
        public AnimationCurve horizontalPattern = AnimationCurve.Linear(0f, 0f, 15f, 0f);

        [Tooltip("水平随机扰动幅度（度）：在模式偏移上叠加 ±此值")]
        [Range(0f, 2f)]
        public float horizontalRandomness = 0.4f;

        [Header("回弹")]
        [Tooltip("准星回弹速度：停止射击后镜头回到原位的速度")]
        [Range(1f, 40f)]
        public float recoverSpeed = 12f;
    }
}
