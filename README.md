# PuzzleAI - Hybrid AI System (Unity)

**A hybrid AI puzzle game demonstrating procedural maze generation, A* pathfinding and ML-Agents reinforcement learning.**

> **Module:** COM6026M, York St John University
> **Author:** Ty Bingham (230254233)

The game presents a push box challenge in which an agent must navigate a procedurally generated maze and then push a box to a randomised goal position in an open arena. A new maze is generated every episode meaning the agent faces a completely different environment each time.

## How to Run

A pre-built Windows executable is available to download from https://github.com/EXPlasmite/PuzzleAI/releases/tag/v1.0

1. Download and unzip the release
2. Run **PuzzleAI.exe** to launch the game
3. The AI agent will run automatically - no input required

To control the agent manually set Behavior Type to **Heuristic Only** and use WASD.

## Building from Source

To build from source:

1. Open the project in Unity 2022.3 LTS or later
2. Open the Level scene in Assets/Scenes
3. File → Build Settings → Build

## AI System

| Technique | Purpose |
|-----------|---------|
| Procedural Maze Generation | Generates a unique 21x22 maze every episode using recursive backtracking |
| A* Pathfinding | Navigates the agent through the maze to the box - 100% success rate |
| ML-Agents PPO | Trained agent pushes box to goal - 28 training runs, 20M+ steps |

## Project Structure

- `Assets/Scripts/` - All C# scripts including PushAgent, MazeGenerator, GridSystem and AStarPathfinder
- `Assets/Models/` - Trained ML-Agents ONNX model
- `Assets/Prefabs/` - Wall prefab used for procedural maze generation
- `Assets/Scenes/` - Level scene containing the full hybrid AI system
- `Assets/Dungeon Pack/` - Third party dungeon tileset, music and camera script (Unity Asset Store Standard License)
- `Assets/ML-Agents/` - Unity ML-Agents package files
- `results/` - Training logs from all 28 training runs showing reward progression
- `config/` - ML-Agents training configuration yaml

## Retraining

To retrain from scratch:

mlagents-learn config/PushAgent.yaml --run-id=NewRunID

Then press Play in Unity. Requires Python 3.10 and mlagents pip package.

## Known Issues

- Agent struggles to push box when goal is positioned directly above or below the box due to approach angle constraints
- Training is slow on CPU - approximately 1.5 to 2 hours per million steps

---

*Developed as part of COM6026M Artificial Intelligence for Games, York St John University, 2025-2026*