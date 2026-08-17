using CharacterController;
using CharacterController.FPS.Logic;
using CharacterController.FPS.Movement;
using UnityEngine;

namespace CharacterController.FPS
{
    public class FPSCharacterStateManager : MonoBehaviour
    {
        [SerializeField] private CharacterActor characterActor;
        [SerializeField] private CharacterBrain characterBrain;
        [SerializeField] private Transform cameraReference;

        public FPSCharacterMovementStateMachine MoveStateMachine { get; private set; }
        public FPSCharacterLogicStateMachine LogicStateMachine { get; private set; }

        private void Awake()
        {
            if (characterActor == null)
            {
                characterActor = GetComponent<CharacterActor>();
            }

            if (characterBrain == null)
            {
                characterBrain = GetComponent<CharacterBrain>();
            }

            MoveStateMachine = new FPSCharacterMovementStateMachine(characterActor, characterBrain);
            LogicStateMachine = new FPSCharacterLogicStateMachine(characterActor, characterBrain);

            if (cameraReference == null && Camera.main != null)
            {
                cameraReference = Camera.main.transform;
            }

            if (cameraReference != null)
            {
                MoveStateMachine.ExternalReference = cameraReference;
            }
            else
            {
                MoveStateMachine.MovementReferenceMode =
                    MovementReferenceParameters.MovementReferenceMode.World;
            }

            InitStates();
        }

        private void Start()
        {
            MoveStateMachine.Start();
            LogicStateMachine.Start();
        }

        private void Update()
        {
            MoveStateMachine?.Update();
            LogicStateMachine?.Update();
        }

        private void FixedUpdate()
        {
            MoveStateMachine?.FixUpdate();
            LogicStateMachine?.FixUpdate();
        }

        private void InitStates()
        {
            var normalMove = new FPSNormalMoveState();
            var sprintMove = new FPSSprintMoveState();
            var jump = new FPSJumpState();

            MoveStateMachine.AddState(normalMove);
            MoveStateMachine.AddState(sprintMove);
            MoveStateMachine.AddState(jump);
            MoveStateMachine.SetDefaultState(FPSMoveState.NormalMove);

            var empty = new FPSEmptyLogicState();
            var fire = new FPSFireState();

            LogicStateMachine.AddState(empty);
            LogicStateMachine.AddState(fire);
            LogicStateMachine.SetDefaultState(FPSLogicState.Empty);
        }
    }
}
