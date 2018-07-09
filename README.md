# MonoGame.StaticBatch

Helper class to batch together static sprites and boost performance

# What Is It

`MonoGame.StaticBatch` allow you to batch together multiple static sprites, to reduce significantly their drawing times (and drawing calls).
As an added bonus, it also add grid-based culling to skip sprites that are not currently visible in screen.

## How It Works

The class will divide your scene into a grid of render targets, and will draw all your static sprites during the initialization (hence they need to be static).

When drawing the static batch, instead of drawing all the individual sprites (lots of draw calls!) it will only draw a small subset of the static textures grid.

## Advantages

When drawing lots of sprites, using the static batch boost performance significantly. More specifically, you enjoy the following benefits:

1. Much less draw calls.
2. Automatic grid-based culling of invisible pixels.
3. Zero work on hidden pixels (pixels covered by other sprites).

# Demo

To watch a live demo of the static batch, clone this repo and Build & Run the project as Window / Console application.

What you'll see is a scene with tilemap, lots of bones, skeletons, and trees. 
By default, the scene will be drawn using the static batch. However, you can switch between drawing methods and compare performance and FPS.

## Credits

Sprites used in demo are from the following sources:

- [https://opengameart.org/content/skeleton-warrior-1](https://opengameart.org/content/skeleton-warrior-1)
- [https://opengameart.org/content/32x32-tilemap-grass-dungeon-floors-snow-water](https://opengameart.org/content/32x32-tilemap-grass-dungeon-floors-snow-water)
- [https://opengameart.org/content/lpc-all-seasons-apple-tree](https://opengameart.org/content/lpc-all-seasons-apple-tree)
