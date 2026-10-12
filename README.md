# Poussan

A small 3D game made in Unity to learn the engine. A chick walks around a house
modelled on a real floor plan and interacts with the furniture.

Unity 6.3 LTS, Universal Render Pipeline.

## Features

- House, walls, floors, doors and windows generated from the floor plan by editor scripts
- Furniture placed by script, using free low-poly models
- Doors that open when the chick gets close
- The chick can sit on chairs, the sofa and the toilet, and lie on the bed
- Close-up views: open fridge, stove with a boiling pot, desk, bookcase, wardrobe
- Air fryer with a sliding drawer, kettle with steam, oven that bakes lasagne
- Panorama photo around the house, visible from the windows

## Controls

| Key | Action |
|---|---|
| WASD | Move |
| Shift | Run |
| Q / E | Rotate the camera by 90° |
| Mouse wheel | Zoom |
| Left click | Interact (sit, open a close-up) |
| Esc / Space / right click | Leave a close-up |
| F | Air fryer drawer |
| G | Kettle |
| B | Oven |

## Running the project

1. Open the folder with Unity Hub (Unity 6.3 LTS).
2. Open `Assets/Scenes/SampleScene`.
3. If the house is missing, run from the menu bar:
   `CasaPulcino → Costruisci casa`, then `Costruisci arredi`, then `Costruisci skyline`.
4. Press Play.

## Project structure

- `Assets/Editor/` – scripts that build the house, furniture, close-ups and panorama
- `Assets/Scripts/` – gameplay: movement, camera, interaction, doors, appliances
- `Assets/Models/` – custom models (air fryer, kettle)

## Credits

- [Furniture Kit](https://kenney.nl) by Kenney – CC0
- [KayKit Furniture Bits](https://kaylousberg.itch.io/furniture-bits) by Kay Lousberg – CC0

## Status

Work in progress. 
