using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;
using Microsoft.Xna.Framework.Graphics;

namespace BepuFluid
{
    class Emitter
    {
        private Space _space;
        public Box Box { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }

        public Entity EmitParticle()
        {
            Vector3 particlePos = Position;
            particlePos += Forward * (Box.Width + 0.1f);
            Sphere particle = new Sphere(particlePos, 1, 1);
            particle.LinearVelocity = Forward;
            _space.Add(particle);

            return particle;
        }

        public Emitter(Space space, Vector3 pos, Box box, Vector3 forward)
        {
            this._space = space;
            this.Position = pos;
            this.Forward = forward;
            this.Box = box;
        }
    }
}
