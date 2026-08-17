using HFSM;
using UnityEngine;
using CharacterController.FPS;

namespace CharacterController.FPS.Logic
{
    public class FPSFireState : FPSLogicStateBase
    {
        public override FPSLogicState currentType => FPSLogicState.Fire;

        public override void Enter(StateBaseInput input = null)
        {
            base.Enter(input);
            Debug.Log("[FPSFire] 开始开火");
        }

        public override void Update()
        {
            base.Update();

            // 每帧消费掉 Attack 输入，避免松开后残留的 Performed 命令导致反复进出开火状态。
            if (TryGetLatestInput(CharacterInputType.Attack, out var attackCommand) &&
                attackCommand.Phase == CharacterInputPhase.Canceled)
            {
                parentMachine.ChangeState(FPSLogicState.Empty);
                return;
            }

            if (!characterActions.attack.value)
            {
                parentMachine.ChangeState(FPSLogicState.Empty);
            }
        }

        public override void Exit()
        {
            Debug.Log("[FPSFire] 停止开火");
            base.Exit();
        }
    }
}
