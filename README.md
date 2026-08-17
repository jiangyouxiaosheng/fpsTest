# FPS Test

一个类《收获日 2》（Payday 2）的联机 FPS 项目。

基于 Unity + HFSM + CCPRO 实现角色控制状态机，当前主要完成 FPS 角色基础控制框架。

## 项目简介

本项目目标是制作一个类似《收获日 2》的合作联机 FPS 游戏：

- 第一人称射击
- 角色移动 / 疾跑 / 跳跃
- 开火与武器系统
- 后续扩展：AI 队友、联机同步、任务系统等

当前仓库已实现：

- FPS 角色控制状态机
- 普通移动
- 疾跑（仅改变移动速度）
- 跳跃与空中惯性
- 按住开火状态（当前为日志占位，未接入实际武器）
- 基于 `CharacterBrain` 的输入接入
- 使用 HFSM 双层状态机：移动层 + 逻辑层

## 技术栈

- Unity
- HFSM（Hierarchical Finite State Machine）
- CCPRO（Character Controller Pro）
- Input System

## 控制方式

| 操作 | 按键 |
| --- | --- |
| 移动 | WASD / 左摇杆 |
| 疾跑 | 左 Shift / 左摇杆按下 |
| 跳跃 | Space |
| 开火 | 鼠标左键 |

## 状态机结构

```text
FPSCharacterStateManager
├── FPSCharacterMovementStateMachine
│   ├── FPSNormalMoveState   // 普通移动 / 待机
│   ├── FPSSprintMoveState   // 疾跑
│   └── FPSJumpState         // 跳跃 / 空中
└── FPSCharacterLogicStateMachine
    ├── FPSEmptyLogicState   // 空闲
    └── FPSFireState         // 按住开火
```

## 目录结构

```text
Assets/Scripts/work/CharacterContorller/
├── FPSCharacterStateManager.cs
└── State/
    ├── FPSCharacterEnums.cs
    ├── Logic/
    │   ├── FPSCharacterLogicStateMachine.cs
    │   ├── FPSLogicStateBase.cs
    │   ├── FPSEmptyLogicState.cs
    │   └── FPSFireState.cs
    └── Movement/
        ├── FPSCharacterMovementStateMachine.cs
        ├── FPSMovementStateBase.cs
        ├── FPSNormalMoveState.cs
        ├── FPSSprintMoveState.cs
        └── FPSJumpState.cs
```

## 使用方式

1. 打开 Unity 项目。
2. 将 `FPSCharacterStateManager` 挂到玩家 GameObject 上。
3. 确保玩家身上有 `CharacterActor`、`CharacterBrain`、`InputSystemHandler`。
4. 将主相机拖到 `FPSCharacterStateManager` 的 `Camera Reference`（可选）。
5. 运行场景即可测试移动、疾跑、跳跃、开火。

## 后续计划

- 接入真实武器开火 / 换弹 / 射击反馈
- 添加瞄准、下蹲、翻滚等动作
- 实现敌人 AI
- 实现联机同步
- 添加任务 / 抢劫玩法
