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
        public Vector3 TensionPartForce { get; set; }

        public Particle(Vector3 position, float radius, float mass) : base(position, radius, mass) { this.Tag = "particle"; }
    }
}
