using UnityEngine;
using CharacterController.FPS;

namespace CharacterController.FPS.Movement
{
    public class FPSNormalMoveState : FPSMovementStateBase
    {
        public override FPSMoveState currentType => FPSMoveState.NormalMove;

        public override void Update()
        {
            base.Update();

            if (!characterActor.IsGrounded)
            {
                parentMachine.ChangeState(FPSMoveState.Jump);
                return;
            }

            if (TryGetInput(CharacterInputType.Jump, out var jumpCommand) &&
                jumpCommand.BoolValue)
            {
                parentMachine.ChangeState(FPSMoveState.Jump, new FPSJumpStateInput(true));
                return;
            }

            if (TryGetInput(CharacterInputType.Crouch, out var crouchCommand) &&
                crouchCommand.BoolValue)
            {
                parentMachine.ChangeState(FPSMoveState.CrouchMove);
                return;
            }

            if (CanSprint())
            {
                parentMachine.ChangeState(FPSMoveState.SprintMove);
            }
        }

        protected override Vector3 ProcessPlanarMovement(float dt)
        {
            currentPlanarSpeedLimit = planarMovementParameters.baseSpeedLimit;
            return CustomUtilities.Multiply(
                parentMachine.InputMovementReference,
                currentPlanarSpeedLimit);
        }

        bool CanSprint()
        {
            if (!characterActor.IsStable)
            {
                return false;
            }

            if (parentMachine.CurrentMovementInput == Vector2.zero)
            {
                return false;
            }

            return characterActions.run.value;
        }
    }
}
