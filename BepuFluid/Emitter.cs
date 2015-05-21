using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;
using Microsoft.Xna.Framework.Graphics;

namespace BepuFluid
{
    class ParticleManager
    {
        public Box Box { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        private float _particleScale = 0.1f;

        private Space _space;
        private List<Particle> _particles = new List<Particle>();

        #region Forces Fields
        private static float H = 1f;
        private static float _pressureScale = 0.1f;
        private static float _viscosityScale = 0.01f;
        private static float _tensionScale = 10f;
        #endregion

        public void Update()
        {
            Particle par1, par2;
            Vector3 [] tensionForceParts = new Vector3 [_particles.Count];
            for (int i = 0; i < _particles.Count; ++i)
            {
                Vector3 tensionForcePart = Vector3.Zero;
                par1 = _particles[i];
                for (int j = 0; j < _particles.Count; j++)
                {
                    if (i == j)
                        continue;
                    par2 = _particles[j];
                    tensionForcePart += GetTensionPart(par1, par2);
                }
                par1.TensionPartForce = tensionForcePart;
                tensionForceParts[i] = tensionForcePart;
            }
            /*
            for (int i = 0; i < _particles.Count; ++i)
            {
                par1 = _particles[i];
                for (int j = 0; j < _particles.Count; j++)
                {
                    if (i == j)
                        continue;
                    par1.LinearVelocity += -_tensionScale * par1.Mass * (tensionForceParts[i] - tensionForceParts[j]);
                }
            }*/
        }

        private Vector3 GetTensionPart(Entity par1, Entity par2)
        {
            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return Vector3.Zero;

            float forceX = (float)(-3 * distanceVector.X * distance - distanceVector.X / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.X);
            float forceY = (float)(-3 * distanceVector.Y * distance - distanceVector.Y / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Y);
            float forceZ = (float)(-3 * distanceVector.Z * distance - distanceVector.Z / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Z);
            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * par2.Mass;
            return forceVector;
        }

        private void ComputeForces(Entity par1, Entity par2)
        {
            ComputeViscosity(par1, par2);
            ComputePressure(par1, par2);

            //ApplyTension(par1, par2);
        }

        private void ComputePressure(Entity par1, Entity par2)
        {
            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return;

            float forceX = (float)(-3 * distanceVector.X * distance - distanceVector.X / (2* Math.Pow(distance, 3)) + 2 * distanceVector.X);
            float forceY = (float)(-3 * distanceVector.Y * distance - distanceVector.Y / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Y);
            float forceZ = (float)(-3 * distanceVector.Z * distance - distanceVector.Z / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Z);
            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * -_pressureScale * par2.Mass;
            par1.LinearVelocity = par1.LinearVelocity + forceVector;
        }

        private void ComputeViscosity(Entity par1, Entity par2)
        {
            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return;

            // dv * (h - r) = dv * (second gradient of W(dr))
            float forceX = (par2.LinearVelocity.X - par1.LinearVelocity.X) * (H - distanceVector.X);
            float forceY = (par2.LinearVelocity.Y - par1.LinearVelocity.Y) * (H - distanceVector.Y);
            float forceZ = (par2.LinearVelocity.Z - par1.LinearVelocity.Z) * (H - distanceVector.Z);
            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * _viscosityScale * par2.Mass;
            par1.LinearVelocity = par1.LinearVelocity + forceVector;
        }

        /// <summary>
        /// Applies previously calculated tension forces to Entities, 
        /// if they can be cast to a Particle.
        /// </summary>
        /// <param name="par1"></param>
        /// <param name="par2"></param>
        private void ApplyTension(Entity par1, Entity par2)
        {
            Particle p1 = par1 as Particle;
            Particle p2 = par2 as Particle;

            // Check if both entities are a Particle
            if (p1 != null && p2 != null)
            {
                p1.LinearVelocity += -_tensionScale * p1.Mass * (p1.TensionPartForce - p2.TensionPartForce);
            }
        }

        private void ComputeTension(Entity par1, Entity par2)
        {
            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return;

            float forceX = (float)(-3 * distanceVector.X * distance - distanceVector.X / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.X);
            float forceY = (float)(-3 * distanceVector.Y * distance - distanceVector.Y / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Y);
            float forceZ = (float)(-3 * distanceVector.Z * distance - distanceVector.Z / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Z);
            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * -_tensionScale * par2.Mass;
            par1.LinearVelocity = par1.LinearVelocity + forceVector;
        }

        void Events_PairCreated(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.BroadPhaseEntry other, BEPUphysics.NarrowPhaseSystems.Pairs.NarrowPhasePair pair)
        {
            ComputeForces(sender.Entity, ((EntityCollidable)other).Entity);
        }

        public Particle EmitParticle()
        {
            Vector3 particlePos = Position;
            particlePos += Forward * Box.Width;
            Particle particle = new Particle(particlePos, _particleScale, _particleScale / 10000);
            particle.LinearVelocity = Forward;
            _space.Add(particle);
            _particles.Add(particle);

            particle.CollisionInformation.Events.PairCreated += Events_PairCreated;

            return particle;
        }

        public ParticleManager(Space space, Vector3 pos, Box box, Vector3 forward)
        {
            this._space = space;
            this.Position = pos;
            this.Forward = forward;
            this.Box = box;
            space.Add(box);
        }
    }
}
