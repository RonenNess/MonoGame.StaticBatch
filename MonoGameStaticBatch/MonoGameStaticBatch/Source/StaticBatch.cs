/// <summary>
/// Utility to render static grid of sprites.
/// Author: Ronen Ness.
/// Since: 2018.
/// </summary>
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MonoGame.StaticBatch
{
    /// <summary>
    /// Callback to begin drawing on a spritebatch.
    /// </summary>
    /// <param name="batch">Spritebatch to begin.</param>
    public delegate void BeginBatchHandler(SpriteBatch batch);

    /// <summary>
    /// A utility to render static grid of render targets.
    /// 
    /// - Why:
    /// To optimize rendering things like tilemaps and other scenes made from lots of static objects.
    /// 
    /// - How it works:
    /// This class divide the screen into a grid of constant size render targets (textures). 
    /// You can then draw on the render targets to update them, and when you draw the grid itself you can provide a rect
    /// representing camera position and screen size, and it will only render the visible textures.
    /// Note: it only create render targets for parts that have something to draw on.
    /// 
    /// How to use:
    /// 1. Call AddSprite() to add renders to grid.
    /// 2. Call Build() to create the grid itself.
    /// 3. Call Draw() to draw the grid on spritebatch.
    /// Some hints:
    ///  - When building the grid, you can either release the list of sprites to free some memory, or keep so you can make changes and rebuild.
    /// </summary>
    public class StaticBatch
    {
        /// <summary>
        /// Static Sprite you can draw on the grid.
        /// </summary>
        public class StaticSprite
        {
            /// <summary>
            /// Texture to draw.
            /// </summary>
            public Texture2D Texture { get; private set; }

            /// <summary>
            /// Destination rect.
            /// </summary>
            public Rectangle DestRect { get; private set; }

            /// <summary>
            /// Source rect.
            /// </summary>
            public Rectangle SourceRect { get; private set; }

            /// <summary>
            /// Drawing color.
            /// </summary>
            public Color Color { get; private set; }

            /// <summary>
            /// Drawing rotation.
            /// </summary>
            public float Rotation { get; private set; }

            /// <summary>
            /// Z-ordering value.
            /// </summary>
            public float ZIndex { get; private set; }

            /// <summary>
            /// Sprite origin.
            /// </summary>
            public Vector2 Origin { get; private set; }

            /// <summary>
            /// Optional sprite effect.
            /// </summary>
            public SpriteEffects SpriteEffect { get; private set; }

            /// <summary>
            /// Default sprite origin to use.
            /// </summary>
            public static Vector2 DefaultOrigin = Vector2.Zero;
            
            /// <summary>
            /// Default rendering color.
            /// </summary>
            public static Color DefaultColor = Color.White;

            /// <summary>
            /// The bounding rect containing this sprite.
            /// </summary>
            public Rectangle BoundingRect { get; private set; }

            /// <summary>
            /// Create the sprite to draw on the static grid.
            /// </summary>
            /// <param name="texture">Texture to draw.</param>
            /// <param name="dest">Destination rect.</param>
            /// <param name="src">Source rect.</param>
            /// <param name="color">Tint color.</param>
            /// <param name="zindex">Z-order value.</param>
            /// <param name="rotation">Drawing rotation.</param>
            /// <param name="origin">Optional sprite origin.</param>
            /// <param name="effect">Optional sprite effects.</param>
            public StaticSprite(Texture2D texture, Rectangle dest, Rectangle? src = null, Color? color = null, float zindex = 0f, float rotation = 0f, Vector2? origin = null, SpriteEffects effect = SpriteEffects.None)
            {
                // set basic params
                Texture = texture;
                DestRect = dest;
                SourceRect = src ?? Texture.Bounds;
                Color = color ?? DefaultColor;
                Rotation = rotation;
                ZIndex = zindex;
                Origin = origin ?? DefaultOrigin;
                SpriteEffect = effect;

                // calculate bounding rect with rotation and origin applied
                if (rotation != 0)
                {
                    BoundingRect = Utils.GetRotatedBoundingBox(DestRect, rotation, Origin, SourceRect);
                }
                // if no rotation, bounding rect is just destination rect
                else
                {
                    BoundingRect = DestRect;
                }
            }
        }

        /// <summary>
        /// Single texture in grid + its metadata.
        /// </summary>
        protected class GridTexture
        {
            /// <summary>
            /// The render target itself.
            /// </summary>
            public RenderTarget2D Texture { get; private set; }

            /// <summary>
            /// Destination rectangle.
            /// </summary>
            public Rectangle DestRect { get; private set; }

            /// <summary>
            /// Create the grid texture chunk.
            /// </summary>
            /// <param name="device">Graphic device.</param>
            /// <param name="index">Index in grid.</param>
            /// <param name="size">Texture size.</param>
            public GridTexture(GraphicsDevice device, Point index, Point size)
            {
                Texture = new RenderTarget2D(device, size.X, size.Y);
                DestRect = new Rectangle(index * size, size);
            }

            /// <summary>
            /// Clear grid texture.
            /// </summary>
            ~GridTexture()
            {
                Texture.Dispose();
            }
        }

        /// <summary>
        /// Return how many textures were actually drawn last Draw() call.
        /// </summary>
        public int LastDrawCallsCount { get; private set; }

        /// <summary>
        /// Graphic device to use.
        /// </summary>
        private GraphicsDevice _device;

        /// <summary>
        /// Default sorting mode when building or drawing the static grid.
        /// </summary>
        public SpriteSortMode DefaultSortMode = SpriteSortMode.FrontToBack;

        /// <summary>
        /// Default sampler state to use when building or drawing the static grid.
        /// </summary>
        public SamplerState DefaultSamplerState = SamplerState.PointClamp;

        /// <summary>
        /// Default blend state to use when building or drawing the static grid.
        /// </summary>
        public BlendState DefaultBlend = BlendState.AlphaBlend;

        /// <summary>
        /// Size, in pixels, of a single render target in grid.
        /// </summary>
        private Point _chunkSize;

        /// <summary>
        /// Grid of textures.
        /// </summary>
        Dictionary<Point, GridTexture> _textures;

        /// <summary>
        /// Current sprites waiting to be built on grid.
        /// </summary>
        List<StaticSprite> _spritesPending;

        /// <summary>
        /// Create the grid.
        /// </summary>
        /// <param name="chunksSize">Size, in pixels, of a single render target.</param>
        public StaticBatch(GraphicsDevice device, Point chunksSize)
        {
            _chunkSize = chunksSize;
            _textures = new Dictionary<Point, GridTexture>();
            _spritesPending = new List<StaticSprite>();
            _device = device;
        }

        /// <summary>
        /// Render the visible grid parts on a given spritebatch.
        /// </summary>
        /// <param name="batch">Spritebatch to render on.</param>
        /// <param name="viewport">If provided, will only draw parts inside the rectangle boundaries. 
        /// If null, will draw all visible textures. Use this to represent camera / visible viewport.</param>
        /// <param name="beginAndEnd">If true, will call Begin batch before drawing parts, and call End when done. 
        /// Set to false if you want to begin drawing yourself with different params.</param>
        /// <param name="offset">Optional offset to draw grid (useful for basic camera).</param>
        public void Draw(SpriteBatch batch, Rectangle? viewport = null, bool beginAndEnd = true, Point? offset = null)
        {
            // zero draws count
            LastDrawCallsCount = 0;

            // begin drawing
            if (beginAndEnd)
            {
                batch.Begin(DefaultSortMode, DefaultBlend, DefaultSamplerState);
            }

            // draw without viewport
            if (viewport == null)
            {
                foreach (var tex in _textures)
                {
                    DrawGridTexture(batch, tex.Value, offset);
                }
            }
            // draw with viewport
            else
            {
                // calc start and end index
                Point startIndex = viewport.Value.Location / _chunkSize;
                Point endIndex = startIndex + viewport.Value.Size / _chunkSize;

                // draw visible parts
                Point currIndex = new Point();
                GridTexture currTex;
                for (currIndex.X = startIndex.X; currIndex.X <= endIndex.X + 1; ++currIndex.X)
                {
                    for (currIndex.Y = startIndex.Y; currIndex.Y <= endIndex.Y + 1; ++currIndex.Y)
                    {
                        if (_textures.TryGetValue(currIndex, out currTex))
                        {
                            DrawGridTexture(batch, currTex, offset);
                        }
                    }
                }
            }

            // end drawing
            if (beginAndEnd)
            {
                batch.End();
            }
        }

        /// <summary>
        /// Draw a single texture from grid.
        /// </summary>
        /// <param name="batch">Spritebatch to draw on. Expected to be after 'Begin' was called.</param>
        /// <param name="texture">The grid texture itself.</param>
        protected void DrawGridTexture(SpriteBatch batch, GridTexture texture, Point? offset)
        {
            // draw with offset
            if (offset.HasValue)
            {
                var dest = texture.DestRect;
                dest.Location += offset.Value;
                batch.Draw(texture.Texture, dest, Color.White);
            }
            // draw without offset
            else
            {
                batch.Draw(texture.Texture, texture.DestRect, Color.White);
            }

            // count draw calls
            LastDrawCallsCount++;
        }

        /// <summary>
        /// Clear whole grid.
        /// </summary>
        public void Clear()
        {
            _textures = new Dictionary<Point, GridTexture>();
        }

        /// <summary>
        /// Clear the texture of a specific grid index.
        /// </summary>
        /// <param name="index">Grid index to clear.</param>
        public void Clear(Point index)
        {
            _textures.Remove(index);
        }

        /// <summary>
        /// Add sprite to draw on the static grid.
        /// </summary>
        /// <param name="sprite">Sprite to add to grid.</param>
        public void AddSprite(StaticSprite sprite)
        {
            _spritesPending.Add(sprite);
        }

        /// <summary>
        /// Add range of sprites to draw on the static grid.
        /// </summary>
        /// <param name="sprites">Enumerable of sprites.</param>
        public void AddSprites(IEnumerable<StaticSprite> sprites)
        {
            _spritesPending.AddRange(sprites);
        }

        /// <summary>
        /// Build the grid from the list of sprites you previously added.
        /// </summary>
        /// <param name="batch">Spritebatch to use for rendering.</param>
        /// <param name="clear">If true, will clear previous textures first. If false, will redraw on them.</param>
        /// <param name="releaseSpritesList">If true, will clear the current list of sprites after done building grid.</param>
        /// <param name="beginBatchHandler">If provided, this function will be called every time we need to begin a drawing batch. 
        /// This provides the ability to choose how to begin the drawing batch, and add your own params and flags to it.</param>
        public void Build(SpriteBatch batch, bool clear = true, bool releaseSpritesList = true, BeginBatchHandler beginBatchHandler = null)
        {
            // clear previous textures if set to clear.
            if (clear)
            {
                Clear();
            }

            // dictionary to sort static sprites into cells, before rendering them
            Dictionary<Point, List<StaticSprite>> sortedSprites = new Dictionary<Point, List<StaticSprite>>();

            // used internally
            Point pointOne = new Point(1, 1);

            // arrange static sprites in grid
            foreach (var sprite in _spritesPending)
            {
                // calc the indexes this sprite is a part of
                Point startIndex = (sprite.BoundingRect.Location / _chunkSize);
                Point endIndex = startIndex + (sprite.BoundingRect.Size / _chunkSize) + pointOne;

                // add to sorted sprites dictionary
                Point currIndex = new Point();
                for (currIndex.X = startIndex.X; currIndex.X <= endIndex.X; ++currIndex.X)
                {
                    for (currIndex.Y = startIndex.Y; currIndex.Y <= endIndex.Y; ++currIndex.Y)
                    {
                        sortedSprites.GetOrCreate(currIndex).Add(sprite);
                    }
                }
            }

            // now that we got sprites arranged into cells, draw them
            foreach (var indexAndSprite in sortedSprites)
            {
                // get current grid texture
                GridTexture currTexture;
                if (!_textures.TryGetValue(indexAndSprite.Key, out currTexture))
                {
                    _textures[indexAndSprite.Key] = currTexture = new GridTexture(_device, indexAndSprite.Key, _chunkSize);
                }

                // begin drawing batch
                if (beginBatchHandler != null)
                {
                    beginBatchHandler(batch);
                }
                else
                {
                    batch.Begin(DefaultSortMode, DefaultBlend, DefaultSamplerState);
                }

                // set rendering target
                _device.SetRenderTarget(currTexture.Texture);
                _device.Clear(Color.Transparent);

                // draw all sprites on it
                foreach (var sprite in indexAndSprite.Value)
                {
                    // set relative dest rect
                    var dest = sprite.DestRect;
                    dest.Location -= currTexture.DestRect.Location;

                    // draw sprite on texture
                    batch.Draw(sprite.Texture,
                        dest, 
                        sprite.SourceRect, 
                        sprite.Color, 
                        sprite.Rotation,
                        sprite.Origin, 
                        sprite.SpriteEffect, 
                        sprite.ZIndex);
                }

                // end drawing current texture
                batch.End();
                _device.SetRenderTarget(null);
            }

            // release previous list
            if (releaseSpritesList)
            {
                _spritesPending.Clear();
            }
        }
    }
}
