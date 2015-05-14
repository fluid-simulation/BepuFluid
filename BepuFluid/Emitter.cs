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
        public Box Box { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        private float _particleScale = 0.1f;

        private Space _space;
        private List<Particle> _particles = new List<Particle>();

        public void Update()
        {
            var particles = _space.Entities.Where(e => (string)e.Tag == "particle").ToArray();
            Entity par1, par2;
            for (int i = 0; i < particles.Length; ++i)
            {
                par1 = particles[i];
                for (int j = 0; j < particles.Length; j++)
                {
                    if (i == j)
                        continue;
                    par2 = particles[j];
                    ComputePressure(par1, par2);
                }
            }
            foreach (var par in particles.Where(p => p.Position.Y < -10))
            {
                _space.Remove(par);
            }
        }

        private void ComputePressure(Entity par1, Entity par2)
        {
            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > 1)
                return;

            var forceX = -3 * distanceVector.X * distance - distanceVector.X / (2* Math.Pow(distance, 3)) + 2 * distanceVector.X;
            par1.LinearVelocity = new Vector3((float)-forceX * 0.01f, 0, 0) + par1.LinearVelocity;
        }

        public Particle EmitParticle()
        {
            Vector3 particlePos = Position;
            particlePos += Forward * Box.Width;
            Particle particle = new Particle(particlePos, _particleScale, _particleScale / 10000);
            particle.LinearVelocity = Forward;
            _space.Add(particle);
            _particles.Add(particle);

            return particle;
        }

        public Emitter(Space space, Vector3 pos, Box box, Vector3 forward)
        {
            this._space = space;
            this.Position = pos;
            this.Forward = forward;
            this.Box = box;
            space.Add(box);
        }
    }
}
