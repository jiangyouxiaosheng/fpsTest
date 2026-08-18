using HFSM;
using UnityEngine;

namespace CharacterController.FPS.Logic
{
    public class FPSInteractState : FPSLogicStateBase
    {
        public string interactionName = "Interaction";
        public float interactionDuration = 1f;

        float timer;

        public override FPSLogicState currentType => FPSLogicState.Interact;

        public override void Enter(StateBaseInput input = null)
        {
            base.Enter(input);

            timer = 0f;
            Debug.Log($"[FPSInteract] 开始交互：{interactionName}");
        }

        public override void Update()
        {
            base.Update();

            // 每帧消费掉交互输入，避免残留的 Performed 命令导致交互结束后再次进入。
            TryGetLatestInput(CharacterInputType.Interact, out _);

            timer += Time.deltaTime;

            if (timer >= interactionDuration)
            {
                Debug.Log($"[FPSInteract] 交互完成：{interactionName}");
                parentMachine.ChangeState(FPSLogicState.Empty);
            }
        }

        public override void Exit()
        {
            Debug.Log($"[FPSInteract] 结束交互：{interactionName}");
            base.Exit();
        }
    }
}
