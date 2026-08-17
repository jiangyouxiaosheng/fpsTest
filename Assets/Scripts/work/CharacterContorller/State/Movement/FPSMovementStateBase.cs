using CharacterController;
using CharacterController.FPS;
using HFSM;
using UnityEngine;

namespace CharacterController.FPS.Movement
{
    public abstract class FPSMovementStateBase : StateBase<FPSMoveState>
    {
        protected PlanarMovementParameters planarMovementParameters = new();
        protected VerticalMovementParameters verticalMovementParameters = new();

        protected CharacterActor characterActor { get; private set; }
        protected CharacterBrain characterBrain { get; private set; }

        protected new FPSCharacterMovementStateMachine parentMachine
            => (FPSCharacterMovementStateMachine)base.parentMachine;

        protected float currentPlanarSpeedLimit = 0f;

        protected CharacterActions characterActions
        {
            get
            {
                return characterBrain == null
                    ? new CharacterActions()
                    : characterBrain.CharacterActions;
            }
        }

        protected bool TryGetInput(CharacterInputType type, out CharacterInputCommand command)
        {
            if (characterBrain != null)
            {
                return characterBrain.TryGetInputCommand(type, out command);
            }

            command = default;
            return false;
        }

        protected bool TryGetLatestInput(CharacterInputType type, out CharacterInputCommand command)
        {
            if (characterBrain != null)
            {
                return characterBrain.TryGetLatestInputCommand(type, out command);
            }

            command = default;
            return false;
        }

        public override void Init()
        {
            characterActor = parentMachine.CharacterActor;
            characterBrain = parentMachine.CharacterBrain;
            database = parentMachine.database;
        }

        public override void FixUpdate()
        {
            base.FixUpdate();
            float dt = Time.deltaTime;

            ProcessVelocity(dt);
        }

        public virtual void PreCharacterSimulation(float dt)
        {
        }

        public virtual void PostCharacterSimulation(float dt)
        {
        }

        protected virtual void ProcessVelocity(float dt)
        {
            if (parentMachine == null)
            {
                ProcessGravity(dt);
                return;
            }

            Vector3 targetVelocity = ProcessPlanarMovement(dt);
            PlanarMovementParameters.PlanarMovementProperties motionInfo = GetMotionValues(targetVelocity);

            float acceleration = motionInfo.acceleration;
            bool needToAccelerate = CustomUtilities.Multiply(
                parentMachine.InputMovementReference,
                currentPlanarSpeedLimit).sqrMagnitude >= characterActor.PlanarVelocity.sqrMagnitude;

            if (needToAccelerate)
            {
                acceleration *= motionInfo.angleAccelerationMultiplier;
            }
            else
            {
                acceleration = motionInfo.deceleration;
            }

            characterActor.PlanarVelocity = Vector3.MoveTowards(
                characterActor.PlanarVelocity,
                targetVelocity,
                acceleration * dt);

            ProcessGravity(dt);
        }

        protected abstract Vector3 ProcessPlanarMovement(float dt);

        protected virtual void ProcessGravity(float dt)
        {
            if (!verticalMovementParameters.useGravity)
            {
                return;
            }

            verticalMovementParameters.UpdateParameters();

            float gravity = verticalMovementParameters.gravity;
            if (!characterActor.IsStable)
            {
                characterActor.VerticalVelocity +=
                    CustomUtilities.Multiply(-characterActor.Up, gravity, dt);
            }
        }

        protected virtual PlanarMovementParameters.PlanarMovementProperties GetMotionValues(Vector3 targetPlanarVelocity)
        {
            float angleCurrentTargetVelocity =
                Vector3.Angle(characterActor.PlanarVelocity, targetPlanarVelocity);

            var currentVelocityInfo = new PlanarMovementParameters.PlanarMovementProperties();

            switch (characterActor.CurrentState)
            {
                case CharacterActorState.NotGrounded:
                    currentVelocityInfo.acceleration = planarMovementParameters.notGroundedAcceleration;
                    currentVelocityInfo.deceleration = planarMovementParameters.notGroundedDeceleration;
                    currentVelocityInfo.angleAccelerationMultiplier =
                        planarMovementParameters.notGroundedAngleAccelerationBoost.Evaluate(angleCurrentTargetVelocity);
                    break;

                case CharacterActorState.StableGrounded:
                    currentVelocityInfo.acceleration = planarMovementParameters.stableGroundedAcceleration;
                    currentVelocityInfo.deceleration = planarMovementParameters.stableGroundedDeceleration;
                    currentVelocityInfo.angleAccelerationMultiplier =
                        planarMovementParameters.stableGroundedAngleAccelerationBoost.Evaluate(angleCurrentTargetVelocity);
                    break;

                case CharacterActorState.UnstableGrounded:
                    currentVelocityInfo.acceleration = planarMovementParameters.unstableGroundedAcceleration;
                    currentVelocityInfo.deceleration = planarMovementParameters.unstableGroundedDeceleration;
                    currentVelocityInfo.angleAccelerationMultiplier =
                        planarMovementParameters.unstableGroundedAngleAccelerationBoost.Evaluate(angleCurrentTargetVelocity);
                    break;
            }

            return currentVelocityInfo;
        }
    }
}
