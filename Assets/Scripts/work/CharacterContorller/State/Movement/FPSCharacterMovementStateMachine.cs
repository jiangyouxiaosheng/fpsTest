using CharacterController;
using HFSM;
using UnityEngine;
using CharacterController.FPS;

namespace CharacterController.FPS.Movement
{
    public class FPSCharacterMovementStateMachine
        : StateMachine<FPSControllerState, FPSMoveState>
    {
        public override FPSControllerState currentType => FPSControllerState.Move;

        public CharacterActor CharacterActor { get; private set; }
        public CharacterBrain CharacterBrain { get; private set; }

        public MovementReferenceParameters MovementReferenceParameters { get; } = new();
        public Vector3 InputMovementReference => MovementReferenceParameters.InputMovementReference;
        public Vector2 CurrentMovementInput { get; private set; }

        public Transform ExternalReference
        {
            get => MovementReferenceParameters.externalReference;
            set => MovementReferenceParameters.externalReference = value;
        }

        public MovementReferenceParameters.MovementReferenceMode MovementReferenceMode
        {
            get => MovementReferenceParameters.movementReferenceMode;
            set => MovementReferenceParameters.movementReferenceMode = value;
        }

        public FPSCharacterMovementStateMachine(CharacterActor actor, CharacterBrain brain)
        {
            CharacterActor = actor;
            CharacterBrain = brain;
        }

        public new FPSMovementStateBase currentState
            => (FPSMovementStateBase)base.currentState;

        public override void Init()
        {
            if (CharacterActor != null)
            {
                CharacterActor.OnPreSimulation += PreCharacterSimulation;
                CharacterActor.OnPostSimulation += PostCharacterSimulation;
                MovementReferenceParameters.Initialize(CharacterActor);
            }

            base.Init();
        }

        public override void Exit()
        {
            if (CharacterActor != null)
            {
                CharacterActor.OnPreSimulation -= PreCharacterSimulation;
                CharacterActor.OnPostSimulation -= PostCharacterSimulation;
            }

            base.Exit();
        }

        public override void FixUpdate()
        {
            if (CharacterBrain != null &&
                CharacterBrain.TryGetLatestInputCommand(
                    CharacterInputType.Movement,
                    out CharacterInputCommand movementCommand))
            {
                CurrentMovementInput = movementCommand.Vector2Value;
            }

            MovementReferenceParameters.UpdateData(CurrentMovementInput);
            base.FixUpdate();
        }

        void PreCharacterSimulation(float dt)
        {
            currentState?.PreCharacterSimulation(dt);
        }

        void PostCharacterSimulation(float dt)
        {
            currentState?.PostCharacterSimulation(dt);
        }
    }
}
