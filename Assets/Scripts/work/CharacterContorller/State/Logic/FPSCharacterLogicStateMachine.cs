using CharacterController;
using HFSM;
using CharacterController.FPS;

namespace CharacterController.FPS.Logic
{
    public class FPSCharacterLogicStateMachine
        : StateMachine<FPSControllerState, FPSLogicState>
    {
        public override FPSControllerState currentType => FPSControllerState.Logic;

        public CharacterActor CharacterActor { get; private set; }
        public CharacterBrain CharacterBrain { get; private set; }

        public FPSCharacterLogicStateMachine(CharacterActor actor, CharacterBrain brain)
        {
            CharacterActor = actor;
            CharacterBrain = brain;
        }

        public new FPSLogicStateBase currentState
            => (FPSLogicStateBase)base.currentState;
    }
}
