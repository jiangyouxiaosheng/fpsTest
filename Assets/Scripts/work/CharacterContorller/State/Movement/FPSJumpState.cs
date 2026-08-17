using CharacterController.FPS;
using HFSM;
using UnityEngine;

namespace CharacterController.FPS.Movement
{
    public record FPSJumpStateInput : StateBaseInput
    {
        public bool ShouldJump { get; }

        public FPSJumpStateInput(bool shouldJump)
        {
            ShouldJump = shouldJump;
        }
    }

    public class FPSJumpState : FPSMovementStateBase
    {
        public override FPSMoveState currentType => FPSMoveState.Jump;

        public override void Enter(StateBaseInput input = null)
        {
            base.Enter(input);

            characterActor.ForceNotGrounded(10);

            if (input is FPSJumpStateInput { ShouldJump: true })
            {
                Jump();
            }
        }

        public override void Update()
        {
            base.Update();

            if (!characterActor.IsGrounded)
            {
                return;
            }

            if (CanSprint())
            {
                parentMachine.ChangeState(FPSMoveState.SprintMove);
            }
            else
            {
                parentMachine.ChangeState(FPSMoveState.NormalMove);
            }
        }

        protected override Vector3 ProcessPlanarMovement(float dt)
        {
            return characterActor.PlanarVelocity;
        }

        void Jump()
        {
            verticalMovementParameters.UpdateParameters();

            Vector3 jumpDirection = characterActor.Up;
            characterActor.Velocity -= Vector3.Project(characterActor.Velocity, jumpDirection);
            characterActor.Velocity += CustomUtilities.Multiply(
                jumpDirection,
                verticalMovementParameters.jumpSpeed);
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
