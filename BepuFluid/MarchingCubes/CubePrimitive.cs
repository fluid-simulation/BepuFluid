using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BepuFluid.MarchingCubes
{
    /// <summary>
    /// Geometric primitive class for drawing Triangles.
    /// </summary>
    public class CubePrimitive : GeometricPrimitive
    {
        public bool isDrawable = false;

        /// <summary>
        /// Constructs a new cube primitive, using default settings.
        /// </summary>
        public CubePrimitive(GraphicsDevice graphicsDevice)
            : this(graphicsDevice, 1)
        {
        }

        public CubePrimitive() : base() { }


        /// <summary>
        /// Constructs a new cube primitive, with the specified size.
        /// </summary>
        public CubePrimitive(GraphicsDevice graphicsDevice, float size)
        {
            InitializePrimitive(graphicsDevice);
        }
    }
}
