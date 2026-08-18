namespace CharacterController.FPS.Logic
{
    public class FPSEmptyLogicState : FPSLogicStateBase
    {
        public override FPSLogicState currentType => FPSLogicState.Empty;

        public override void Update()
        {
            base.Update();

            if (TryGetInput(CharacterInputType.Interact, out var interactCommand) &&
                interactCommand.BoolValue)
            {
                parentMachine.ChangeState(FPSLogicState.Interact);
                return;
            }

            if (TryGetInput(CharacterInputType.Attack, out var attackCommand) &&
                attackCommand.BoolValue)
            {
                parentMachine.ChangeState(FPSLogicState.Fire);
            }
        }
    }
}
