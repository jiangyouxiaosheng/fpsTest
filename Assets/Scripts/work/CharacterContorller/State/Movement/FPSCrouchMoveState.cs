using CharacterController.FPS;
using HFSM;
using UnityEngine;

namespace CharacterController.FPS.Movement
{
    public class FPSCrouchMoveState : FPSMovementStateBase
    {
        public override FPSMoveState currentType => FPSMoveState.CrouchMove;

        public override void Enter(StateBaseInput input = null)
        {
            base.Enter(input);
        }

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

            if (ShouldExitCrouch())
            {
                parentMachine.ChangeState(FPSMoveState.NormalMove);
            }
        }

        public override void FixUpdate()
        {
            base.FixUpdate();

            float dt = Time.deltaTime;
            float targetHeight = characterActor.DefaultBodySize.y * crouchParameters.heightRatio;
            characterActor.CheckAndInterpolateHeight(
                targetHeight,
                crouchParameters.sizeLerpSpeed * dt,
                SizeReferenceType.Bottom);
        }

        public override void Exit()
        {
            characterActor.CheckAndInterpolateHeight(
                characterActor.DefaultBodySize.y,
                1f,
                SizeReferenceType.Bottom);

            base.Exit();
        }

        protected override Vector3 ProcessPlanarMovement(float dt)
        {
            currentPlanarSpeedLimit =
                planarMovementParameters.baseSpeedLimit * crouchParameters.speedMultiplier;

            return CustomUtilities.Multiply(
                parentMachine.InputMovementReference,
                currentPlanarSpeedLimit);
        }

        bool ShouldExitCrouch()
        {
            if (!crouchParameters.enableCrouch)
            {
                return true;
            }

            if (crouchParameters.inputMode == InputMode.Toggle)
            {
                return TryGetInput(CharacterInputType.Crouch, out var crouchCommand) &&
                       crouchCommand.BoolValue;
            }

            return !characterActions.crouch.value;
        }
    }
}
