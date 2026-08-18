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
        CrouchMove,
        Jump
    }

    public enum FPSLogicState
    {
        Empty,
        Fire,
        Interact
    }
}
