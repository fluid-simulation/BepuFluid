using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;

namespace BepuFluid
{
    class Particle : Sphere
    {
        public Vector3 ColorFieldGradient { get; set; }
        public Vector3 ColorField { get; set; }

        public Particle(Vector3 position, float radius, float mass) : base(position, radius, mass) 
        { 
            this.Tag = "particle";
            this.ColorFieldGradient = Vector3.Zero;
            this.ColorField = Vector3.Zero;
        }
    }
}
