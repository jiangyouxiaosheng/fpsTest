namespace CharacterController.FPS
{
    public enum FPSControllerState
    {
        Move,
        Logic
    }

    public enum FPSMoveState
    {
        NormalMove,
        SprintMove,
        Jump
    }

    public enum FPSLogicState
    {
        Empty,
        Fire
    }
}
