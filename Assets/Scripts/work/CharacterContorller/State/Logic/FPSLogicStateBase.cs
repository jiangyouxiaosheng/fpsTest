using CharacterController;
using HFSM;
using CharacterController.FPS;

namespace CharacterController.FPS.Logic
{
    public abstract class FPSLogicStateBase : StateBase<FPSLogicState>
    {
        protected CharacterActor characterActor { get; private set; }
        protected CharacterBrain characterBrain { get; private set; }

        protected new FPSCharacterLogicStateMachine parentMachine
            => (FPSCharacterLogicStateMachine)base.parentMachine;

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
    }
}
