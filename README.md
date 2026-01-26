# TileMaster

A 2D tile-based platformer game built with MonoGame and C#/.NET. This project serves as a prototype experimenting with various game development concepts including procedural map generation, physics simulation, entity management, and crafting systems.

## Features

### Core Gameplay
- **Procedural World Generation**: Randomly generated tile-based worlds with varied terrain
- **Physics System**: Player movement with gravity, collision detection, and slope handling
- **Camera System**: Smooth camera following with chunk-based world loading
- **Lighting System**: Dynamic lighting with light sources like torches

### Game Systems
- **Tile System**: Various tile types (Dirt, Grass, Stone, etc.) with different properties
- **Entity System**: Mobs with AI movement patterns and behaviors
- **Inventory Management**: Player and container-based inventory systems
- **Crafting System**: Recipe-based crafting with various items and tools
- **Item System**: Placeable items, tools, and resources with metadata

### Technical Features
- **Chunk-based World Loading**: Efficient world rendering with chunk management
- **Component-based UI**: Myra-based UI system with windows for inventory, crafting, and debugging
- **JSON-based Data**: Configurable tiles, items, recipes, and mobs via JSON files
- **Unit Testing**: Test coverage for core game mechanics and systems

## Getting Started

### Prerequisites
- **.NET 10.0** or later
- **Windows 7** or later (Windows-only application)
- **Visual Studio 2022** or compatible IDE

### Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd "Tile Game/Tiles/TileMaster 2"
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore TileMaster.sln
   ```

3. **Build the solution:**
   ```bash
   dotnet build TileMaster.sln
   ```

4. **Run the game:**
   ```bash
   dotnet run --project TileMaster\TileMaster.csproj
   ```

### Alternative: Visual Studio
1. Open `TileMaster.sln` in Visual Studio 2022
2. Restore NuGet packages (Build → Restore Solution)
3. Build and run the solution (F5)

## Project Structure

```
TileMaster 2/
├── TileMaster/              # Main game project
│   ├── Data/              # JSON configuration files
│   │   ├── Items.json     # Item definitions
│   │   ├── Recipes.json   # Crafting recipes
│   │   ├── Tiles.json     # Tile types and properties
│   │   └── Mobs.json      # Entity definitions
│   ├── Content/           # Game assets and textures
│   ├── UI/               # User interface components
│   ├── Entity/           # Game entities and AI
│   ├── Manager/          # Game systems managers
│   └── Map/              # World generation and map logic
├── TileMaster.Util/       # Utility library
├── TileMaster.Test/       # Unit tests
└── TileMaster.sln        # Solution file
```

## Controls

The game features standard platformer controls:
- **Movement**: Arrow keys or WASD
- **Jump**: Spacebar
- **Inventory**: I key
- **Crafting**: C key
- **Debug Menu**: F1 key
- **Mouse**: Look around and interact with tiles/items

## Configuration

Game data is configured through JSON files in the `Data/` directory:
- **Tiles.json**: Define tile properties, textures, and behaviors
- **Items.json**: Configure items, their properties, and UI icons
- **Recipes.json**: Set up crafting recipes and requirements
- **Mobs.json**: Define entity types and their characteristics

## Development

This project uses several key technologies:
- **MonoGame Framework**: 2D game rendering and input handling
- **Myra**: Cross-platform GUI library for game interfaces
- **Newtonsoft.Json**: JSON serialization for game data
- **xUnit**: Unit testing framework

### Running Tests
```bash
dotnet test TileMaster.Test/TileMaster.Test.csproj
```

## Current Status

This is an **experimental prototype** rather than a complete game. The project serves as a learning platform for:
- Procedural generation algorithms
- 2D physics and collision detection
- Entity component patterns
- Game state management
- UI/UX design for games

## Contributing

As this is primarily a learning/experimental project, contributions focus on:
- Bug fixes and stability improvements
- New tile types and items
- Enhanced procedural generation
- Performance optimizations
- Additional game mechanics

Feel free to experiment with the codebase and submit pull requests for improvements!