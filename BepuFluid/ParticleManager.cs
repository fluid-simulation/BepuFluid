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
        #region Emitter
        public Box EmitterBox { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        private float _particleScale = 0.1f;

        private Space _space;
        private List<Particle> _particles = new List<Particle>();

        public Particle EmitParticle()
        {
            Vector3 particlePos = Position;
            particlePos += Forward * EmitterBox.Width;
            Particle particle = new Particle(particlePos, _particleScale, _particleScale / 10000);
            particle.LinearVelocity = Forward;
            _space.Add(particle);
            _particles.Add(particle);

            //particle.CollisionInformation.Events.PairCreated += Events_PairCreated;

            PutParticleInGrid(particle);

            return particle;
        }
        #endregion

        #region Forces Fields
        private static float H = 1f;
        private static float _pressureScale = 1.1f;
        private static float _viscosityScale = 1.1f;
        private static float _tensionScale = 0.01f;
        #endregion

        #region Forces
        public void UpdateForces()
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
                par1.ColorFieldGradient = tensionForcePart;
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
            if (distance > H || distance == 0)
                return Vector3.Zero;

            float forceX = (float)(-3 * distanceVector.X * distance - distanceVector.X / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.X);
            float forceY = (float)(-3 * distanceVector.Y * distance - distanceVector.Y / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Y);
            float forceZ = (float)(-3 * distanceVector.Z * distance - distanceVector.Z / (2 * Math.Pow(distance, 3)) + 2 * distanceVector.Z);
            var forceVector = new Vector3(forceX, forceY, forceZ);
            //forceVector = forceVector * par2.Mass;
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
            if (par1 == par2)
                return;

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
            if (par1 == par2)
                return;

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
                p1.LinearVelocity += -_tensionScale * p1.Mass * (p1.ColorFieldGradient - p2.ColorFieldGradient);
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
        #endregion

        #region Grid
        private List<Particle>[, ,] _grid;
        private int _gridSize = 64;

        private Vector3 _translation;

        private void PutParticleInGrid(Particle par)
        {
            var pos = par.Position - _translation;
            if (pos.X < _gridSize && pos.Y < _gridSize && pos.Z < _gridSize &&
                pos.X > 0 && pos.Y > 0 && pos.Z > 0)
            {
                int x = (int)pos.X;
                int y = (int)pos.Y;
                int z = (int)pos.Z;

                if (_grid[x, y, z] == null)
                    _grid[x, y, z] = new List<Particle>();

                _grid[x, y, z].Add(par);
            }
        }

        public void Update()
        {
            _grid = new List<Particle>[_gridSize, _gridSize, _gridSize];

            foreach (var par in _particles)
            {
                PutParticleInGrid(par);
            }

            for (int x = 0; x < _gridSize; ++x)
            {
                for (int y = 0; y < _gridSize; ++y)
                {
                    for (int z = 0; z < _gridSize; ++z)
                    {
                        var gridCell = _grid[x, y, z];

                        if (gridCell == null)
                            continue;

                        foreach (var par1 in gridCell)
                        {
                            par1.ColorField = Vector3.Zero;
                            par1.ColorFieldGradient = Vector3.Zero;

                            foreach (var par2 in gridCell)
                            {
                                if (par1 == par2)
                                    continue;

                                ComputePressure(par1, par2);
                                ComputeViscosity(par1, par2);
                                par1.ColorFieldGradient += GetTensionPart(par1, par2);
                            }

                            for (int x2 = x - 1; x2 < x + 2; ++x2)
                            {
                                for (int y2 = y - 1; y2 < y + 2; ++y2)
                                {
                                    for (int z2 = z - 1; z2 < z + 2; ++z2)
                                    {
                                        if (x2 >= 0 && x2 < _gridSize && y2 >= 0 && y2 < _gridSize && z2 >= 0 && z2 < _gridSize)
                                        {
                                            var secondCell = _grid[x2, y2, z2];

                                            if (secondCell == null)
                                                continue;

                                            foreach (var par2 in secondCell)
                                            {
                                                ComputePressure(par1, par2);
                                                ComputeViscosity(par1, par2);
                                                par1.ColorFieldGradient += GetTensionPart(par1, par2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < _gridSize; ++x)
            {
                for (int y = 0; y < _gridSize; ++y)
                {
                    for (int z = 0; z < _gridSize; ++z)
                    {
                        var gridCell = _grid[x, y, z];

                        if (gridCell == null)
                            continue;

                        foreach (var par1 in gridCell)
                        {
                            foreach (var par2 in gridCell)
                            {
                                if (par1 == par2)
                                    continue;

                                var tensionForcePart = -_tensionScale * (par1.ColorFieldGradient - par2.ColorFieldGradient);
                                //par1.LinearVelocity += tensionForcePart;
                                par1.ColorField += tensionForcePart;
                            }

                            for (int x2 = x - 1; x2 < x + 2; ++x2)
                            {
                                for (int y2 = y - 1; y2 < y + 2; ++y2)
                                {
                                    for (int z2 = z - 1; z2 < z + 2; ++z2)
                                    {
                                        if (x2 >= 0 && x2 < _gridSize && y2 >= 0 && y2 < _gridSize && z2 >= 0 && z2 < _gridSize)
                                        {
                                            var secondCell = _grid[x2, y2, z2];

                                            if (secondCell == null)
                                                continue;

                                            foreach (var par2 in secondCell)
                                            {
                                                var tensionForcePart = -_tensionScale * (par1.ColorFieldGradient - par2.ColorFieldGradient);
                                                //par1.LinearVelocity += tensionForcePart;
                                                par1.ColorField += tensionForcePart;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Marching Cubes
        public double[, ,] GetParticlesGData(int xSize, int ySize, int zSize, Vector3 translation, Vector3 scale)
        {
            double[, ,] gdata = new double[xSize, ySize, zSize];

            foreach (var par in _particles)
            {
                Vector3 pos = par.Position - translation;
                int x = (int)(pos.X / scale.X);
                int y = (int)(pos.Y / scale.Y);
                int z = (int)(pos.Z / scale.Z);

                if (x < xSize && y < ySize && z < zSize && x >= 0 && y >= 0 && z >= 0)
                {
                    double isoLevel = par.ColorField.LengthSquared();//(par.ColorField.X + par.ColorField.Y + par.ColorField.Z) / 9;
                    gdata[x, y, z] += isoLevel; //1.0;
                }
            }

            return gdata;
        }
        #endregion

        public ParticleManager(Space space, Vector3 boxPosition, Box box, Vector3 forward, Vector3 translation)
        {
            this._space = space;
            this.Position = boxPosition;
            this.Forward = forward;
            this.EmitterBox = box;
            space.Add(box);

            _grid = new List<Particle>[_gridSize, _gridSize, _gridSize];
            _translation = translation;
        }
    }
}
