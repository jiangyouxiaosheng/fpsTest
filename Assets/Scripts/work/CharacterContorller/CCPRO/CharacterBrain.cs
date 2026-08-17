using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterController
{
    public enum BrainType
    {
        Player,
        AI,
        Network,
    }

    [DefaultExecutionOrder(-100)]
    public class CharacterBrain : MonoBehaviour
    {
        enum InputSourceType
        {
            Player,
            AI,
            Network,
        }

        public BrainType brainType = BrainType.Player;

        [FormerlySerializedAs("UpdateMode")]
        [SerializeField, HideInInspector]
        UpdateModeType updateModeCompatibility = UpdateModeType.Update;

        bool isAI => brainType == BrainType.AI;
        bool isNetwork => brainType == BrainType.Network;
        bool isPlayer => brainType == BrainType.Player;

        [SerializeField]
        InputHandlerSettings inputHandlerSettings = new InputHandlerSettings();


        // [SerializeField]
        // NetCharacterInput netCharacterInput;

        // IEnemyBrain aiBehaviour;

        CharacterActions characterActions = new CharacterActions();
        CharacterActions sampledCharacterActions = new CharacterActions();

        [SerializeField, Min(8)]
        int inputQueueCapacity = 64;

        [SerializeField, Min(0.02f)]
        float inputCommandLifeTime = 0.3f;

        CharacterInputCommandQueue inputCommandQueue;
        CharacterInputMask enabledInputMask = CharacterInputMask.All;
        Vector2 lastQueuedMovement;
        uint inputSequence;

        bool actionsInitialized;
        bool aiReferenceWarningShown;
        bool networkReferenceWarningShown;

        public bool IsAI => isAI;

        public CharacterActions CharacterActions => characterActions;
        public int PendingInputCount => inputCommandQueue?.Count ?? 0;

        /// <summary>
        /// 每帧更新的视角输入（鼠标 delta / 右摇杆），供第一人称相机等系统使用。
        /// 不经过 FixedUpdate 队列，避免鼠标 delta 在物理帧中重复或丢失。
        /// </summary>
        public Vector2 LookInput { get; private set; }

        public void UpdateBrainValues(float dt)
        {
            AdvanceActions(dt);
            EnqueueCurrentActions();
        }

        public bool TryGetInputCommand(CharacterInputType type, out CharacterInputCommand command)
        {
            if (inputCommandQueue == null)
            {
                command = default;
                return false;
            }

            return inputCommandQueue.TryConsume(type, Time.time, out command);
        }

        public bool TryGetLatestInputCommand(CharacterInputType type, out CharacterInputCommand command)
        {
            if (inputCommandQueue == null)
            {
                command = default;
                return false;
            }

            return inputCommandQueue.TryConsumeLatest(type, Time.time, out command);
        }

        public bool IsInputEnabled(CharacterInputType type)
        {
            return (enabledInputMask & type.ToMask()) != 0;
        }

        public void EnableInput(CharacterInputMask mask)
        {
            enabledInputMask |= mask;
        }

        public void DisableInput(CharacterInputMask mask, bool clearQueuedCommands = true)
        {
            enabledInputMask &= ~mask;

            if (!clearQueuedCommands || inputCommandQueue == null)
            {
                return;
            }

            for (int i = 0; i <= (int)CharacterInputType.UICancel; i++)
            {
                CharacterInputType type = (CharacterInputType)i;
                if ((mask & type.ToMask()) != 0)
                {
                    inputCommandQueue.Remove(type);
                }
            }
        }

        public void SetEnabledInputs(CharacterInputMask mask, bool clearDisabledCommands = true)
        {
            CharacterInputMask disabledMask = CharacterInputMask.All & ~mask;
            enabledInputMask = mask;

            if (clearDisabledCommands)
            {
                DisableInput(disabledMask, true);
                enabledInputMask = mask;
            }
        }

        public void ClearInputCommands()
        {
            inputCommandQueue?.Clear();
        }

        void AdvanceActions(float dt)
        {
            SampleActions();
            characterActions.ClearFrameFlags();
            characterActions.SetValues(sampledCharacterActions);
            characterActions.Update(dt);
        }

        void SampleActions()
        {
            sampledCharacterActions.Reset();

            switch (ResolveInputSource())
            {
                case InputSourceType.Player:
                    SamplePlayerActions();
                    break;
                    // case InputSourceType.AI:
                    //     SampleAIActions();
                    //     break;
                    // case InputSourceType.Network:
                    //     SampleNetworkActions();
                    //     break;
            }
        }

        void SamplePlayerActions()
        {
            if (inputHandlerSettings.InputHandler != null)
            {
                sampledCharacterActions.SetValues(inputHandlerSettings.InputHandler);
            }
        }

        // void SampleAIActions()
        // {
        //     if (aiBehaviour == null)
        //     {
        //         ResolveExternalReferences();
        //     }

        //     if (aiBehaviour == null)
        //     {
        //         LogMissingAIReference();
        //         return;
        //     }

        //     aiReferenceWarningShown = false;
        //     sampledCharacterActions.SetValues(aiBehaviour.characterActions);
        // }

        // void SampleNetworkActions()
        // {
        //     if (netCharacterInput == null)
        //     {
        //         ResolveExternalReferences();
        //     }

        //     if (netCharacterInput == null)
        //     {
        //         LogMissingNetworkReference();
        //         return;
        //     }

        //     networkReferenceWarningShown = false;
        //     sampledCharacterActions.SetValues(netCharacterInput.CharacterActions);
        // }

        protected virtual void Awake()
        {
            inputCommandQueue = new CharacterInputCommandQueue(inputQueueCapacity);
            InitializeActions();
            ResolveExternalReferences();
        }

        protected virtual void OnEnable()
        {
            InitializeActions();
            ResolveExternalReferences();
            ResetLocalActions();
            ApplyPlayerInputMode();
        }

        protected virtual void OnDisable()
        {
            ResetLocalActions();
        }

        protected virtual void Update()
        {
            LookInput = inputHandlerSettings.InputHandler != null
                ? inputHandlerSettings.InputHandler.GetVector2("Look")
                : Vector2.zero;
        }

        protected virtual void FixedUpdate()
        {
            AdvanceActions(Time.fixedDeltaTime);
            EnqueueCurrentActions();
        }

        void EnqueueCurrentActions()
        {
            if (inputCommandQueue == null)
            {
                return;
            }

            float currentTime = Time.time;
            inputCommandQueue.RemoveExpired(currentTime);

            EnqueueBool(CharacterInputType.Jump, characterActions.jump, currentTime);
            EnqueueBool(CharacterInputType.Run, characterActions.run, currentTime);
            EnqueueBool(CharacterInputType.Interact, characterActions.interact, currentTime);
            EnqueueBool(CharacterInputType.Roll, characterActions.roll, currentTime);
            EnqueueBool(CharacterInputType.Lock, characterActions.@lock, currentTime);
            EnqueueBool(CharacterInputType.Attack, characterActions.attack, currentTime);
            EnqueueBool(CharacterInputType.HeavyAttack, characterActions.heavyAttack, currentTime);
            EnqueueBool(CharacterInputType.Crouch, characterActions.crouch, currentTime);
            EnqueueBool(CharacterInputType.OpenUI, characterActions.OpenUI, currentTime);
            EnqueueBool(CharacterInputType.OpenConsole, characterActions.OpenConsoleUI, currentTime);
            EnqueueMovement(characterActions.movement.value, currentTime);
        }

        void EnqueueBool(CharacterInputType type, BoolAction action, float currentTime)
        {
            if (!action.value && !action.Canceled)
            {
                return;
            }

            CharacterInputPhase phase = action.Started
                ? CharacterInputPhase.Started
                : action.Canceled
                    ? CharacterInputPhase.Canceled
                    : CharacterInputPhase.Performed;

            EnqueueCommand(type, phase, action.value, Vector2.zero, currentTime);
        }

        void EnqueueMovement(Vector2 movement, float currentTime)
        {
            bool hasMovement = movement != Vector2.zero;
            bool hadMovement = lastQueuedMovement != Vector2.zero;

            if (!hasMovement && !hadMovement)
            {
                return;
            }

            CharacterInputPhase phase = !hadMovement && hasMovement
                ? CharacterInputPhase.Started
                : hadMovement && !hasMovement
                    ? CharacterInputPhase.Canceled
                    : CharacterInputPhase.Performed;

            EnqueueCommand(CharacterInputType.Movement, phase, hasMovement, movement, currentTime);
            lastQueuedMovement = movement;
        }

        void EnqueueCommand(
            CharacterInputType type,
            CharacterInputPhase phase,
            bool boolValue,
            Vector2 vector2Value,
            float currentTime)
        {
            if (!IsInputEnabled(type))
            {
                return;
            }

            inputSequence++;
            inputCommandQueue.Enqueue(new CharacterInputCommand(
                type,
                phase,
                boolValue,
                vector2Value,
                currentTime,
                currentTime + inputCommandLifeTime,
                inputSequence));
        }

        InputSourceType ResolveInputSource()
        {
            if (isAI)
            {
                return InputSourceType.AI;
            }

            if (isNetwork)
            {
                return InputSourceType.Network;
            }

            return InputSourceType.Player;
        }

        void InitializeActions()
        {
            if (actionsInitialized)
            {
                return;
            }

            characterActions.InitializeActions();
            sampledCharacterActions.InitializeActions();
            actionsInitialized = true;
        }

        void ResolveExternalReferences()
        {
            if (inputHandlerSettings.InputHandler == null)
            {
                inputHandlerSettings.InputHandler = GetComponent<InputHandler>();
            }
        }

        void ResetLocalActions()
        {
            characterActions.Reset();
            sampledCharacterActions.Reset();
            LookInput = Vector2.zero;
            lastQueuedMovement = Vector2.zero;
            inputCommandQueue?.Clear();
        }

        void ApplyPlayerInputMode()
        {
            if (!isPlayer)
            {
                return;
            }

            SetHandlerEnabled(inputHandlerSettings, true);
        }

        void LogMissingAIReference()
        {
            if (aiReferenceWarningShown)
            {
                return;
            }

            aiReferenceWarningShown = true;
            Debug.LogWarning($"{name} 的 CharacterBrain 未找到 IEnemyBrain，已清空 AI 输入。", this);
        }

        void LogMissingNetworkReference()
        {
            if (networkReferenceWarningShown)
            {
                return;
            }

            networkReferenceWarningShown = true;
            Debug.LogWarning($"{name} 的 CharacterBrain 未找到 NetCharacterInput，已清空网络输入。", this);
        }

        static void SetHandlerEnabled(InputHandlerSettings settings, bool enabled)
        {
            if (settings == null || settings.InputHandler == null)
            {
                return;
            }

            if (enabled)
            {
                settings.InputHandler.Enable();
            }
            else
            {
                settings.InputHandler.Disable();
            }
        }

        public enum UpdateModeType
        {
            FixedUpdate,
            Update
        }
    }
}
