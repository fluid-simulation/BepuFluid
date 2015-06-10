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
        private float _particleScale = 0.10f;

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
        private static float _pressureScale = 5.5f;
        private static float _viscosityScale = 10.5f;
        private static float _tensionScale = 30.0f;
        private static float _tensionThreshold = 0.0001f;
        #endregion

        #region Forces

        /// <summary>
        /// 315 / H^9 * 64 * Pi
        /// </summary>
        private const float POLY_KERNEL_SCALE = 315.0f / (float)(64 * Math.PI);

        private void ApplyTension(Particle par)
        {
            var tensionLevel = par.ColorFieldGradient.Length();
            if (tensionLevel > _tensionThreshold)
            {
                float forceX = -_tensionScale * par.ColorFieldSecondGradient.X * par.ColorFieldGradient.X / tensionLevel;
                float forceY = -_tensionScale * par.ColorFieldSecondGradient.Y * par.ColorFieldGradient.Y / tensionLevel;
                float forceZ = -_tensionScale * par.ColorFieldSecondGradient.Z * par.ColorFieldGradient.Z / tensionLevel;

                var forceVector = new Vector3(forceX, forceY, forceZ);

                par.ColorField = forceVector;
                par.ComputedForce += forceVector;
                par.TensionLevel = tensionLevel;
            }
        }

        /// <summary>
        /// 15 / (Pi * h^6)
        /// </summary>
        private const float TENSION_KERNEL_SCALE = (float)(315 / (64 * Math.PI));

        private void ComputeTensionGradient(Particle par1, Particle par2)
        {
            if (par1 == par2)
                return;

            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H || distance == 0)
                return;

            float forceX = -6 * distanceVector.X * (float)Math.Pow((Math.Pow(distance, 2) - Math.Pow(H, 2)), 2);
            float forceY = -6 * distanceVector.Y * (float)Math.Pow((Math.Pow(distance, 2) - Math.Pow(H, 2)), 2);
            float forceZ = -6 * distanceVector.Z * (float)Math.Pow((Math.Pow(distance, 2) - Math.Pow(H, 2)), 2);

            var forceVector = new Vector3(forceX, forceY, forceZ);
            par1.ColorFieldGradient += forceVector * par2.Mass * TENSION_KERNEL_SCALE;
        }

        private void ComputeTensionSecondGradient(Particle par1, Particle par2)
        {
            if (par1 == par2)
                return;

            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H || distance == 0)
                return;

            float forceX = -6 * (float)((Math.Pow(distance, 2) - Math.Pow(H, 2)) * (Math.Pow(distance, 2) + 4 * Math.Pow(distanceVector.X, 2) - Math.Pow(H, 2)));
            float forceY = -6 * (float)((Math.Pow(distance, 2) - Math.Pow(H, 2)) * (Math.Pow(distance, 2) + 4 * Math.Pow(distanceVector.Y, 2) - Math.Pow(H, 2)));
            float forceZ = -6 * (float)((Math.Pow(distance, 2) - Math.Pow(H, 2)) * (Math.Pow(distance, 2) + 4 * Math.Pow(distanceVector.Z, 2) - Math.Pow(H, 2)));

            var forceVector = new Vector3(forceX, forceY, forceZ);
            par1.ColorFieldSecondGradient += forceVector * par2.Mass * TENSION_KERNEL_SCALE;
        }

        /// <summary>
        /// 15 / (Pi * h^6)
        /// </summary>
        private const float PRESSURE_KERNEL_SCALE = (float)(15 / Math.PI);

        private void ComputePressure(Particle par1, Particle par2)
        {
            if (par1 == par2)
                return;

            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return;

            float forceX = (float)(-3 * distanceVector.X * Math.Pow(distance - H, 2)) / distance;
            float forceY = (float)(-3 * distanceVector.Y * Math.Pow(distance - H, 2)) / distance;
            float forceZ = (float)(-3 * distanceVector.Z * Math.Pow(distance - H, 2)) / distance;

            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * -_pressureScale * PRESSURE_KERNEL_SCALE * par2.Mass;
            par1.ComputedForce += forceVector;
        }

        /// <summary>
        /// 45 / (Pi * h^6)
        /// </summary>
        private const float VISCOSITY_KERNEL_SCALE = (float)(45 / Math.PI);

        private void ComputeViscosity(Particle par1, Particle par2)
        {
            if (par1 == par2)
                return;

            var distanceVector = par1.Position - par2.Position;
            var distance = Vector3.Distance(par1.Position, par2.Position);
            if (distance > H)
                return;

            // dv * (h - r) = dv * (second gradient of W(dr, h))
            float forceX = (par2.LinearVelocity.X - par1.LinearVelocity.X) * VISCOSITY_KERNEL_SCALE * (H - distanceVector.X);
            float forceY = (par2.LinearVelocity.Y - par1.LinearVelocity.Y) * VISCOSITY_KERNEL_SCALE * (H - distanceVector.Y);
            float forceZ = (par2.LinearVelocity.Z - par1.LinearVelocity.Z) * VISCOSITY_KERNEL_SCALE * (H - distanceVector.Z);

            var forceVector = new Vector3(forceX, forceY, forceZ);
            forceVector = forceVector * _viscosityScale * par2.Mass;
            par1.ComputedForce += forceVector;
        }

        #endregion

        #region Grid
        private List<Particle>[, ,] _grid;
        private int _gridSize = 64;

        private List<int[]> _nonEmptyCells;

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
                {
                    _grid[x, y, z] = new List<Particle>();
                    _nonEmptyCells.Add(new int[] { x, y, z });
                }

                _grid[x, y, z].Add(par);
            }
        }

        public void Update()
        {
            _grid = new List<Particle>[_gridSize, _gridSize, _gridSize];

            _nonEmptyCells = new List<int[]>();

            foreach (var par in _particles)
            {
                PutParticleInGrid(par);
            }

            foreach (var cellCoord in _nonEmptyCells)
            {
                int x = cellCoord[0];
                int y = cellCoord[1];
                int z = cellCoord[2];

                var gridCell = _grid[x, y, z];

                if (gridCell == null)
                    continue;

                foreach (var par1 in gridCell)
                {
                    par1.ColorField = Vector3.Zero;
                    par1.ColorFieldGradient = Vector3.Zero;
                    par1.ColorFieldSecondGradient = Vector3.Zero;
                    par1.ComputedForce = Vector3.Zero;
                    par1.TensionLevel = 0;

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
                                        if (par1 == par2)
                                            continue;

                                        ComputePressure(par1, par2);
                                        ComputeViscosity(par1, par2);
                                        ComputeTensionGradient(par1, par2);
                                        ComputeTensionSecondGradient(par1, par2);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var par in _particles)
            {
                ApplyTension(par);
                par.LinearVelocity += par.ComputedForce;
            }
        }

        #endregion

        #region Marching Cubes

        public double[, ,] GetParticlesGData(int xSize, int ySize, int zSize, Vector3 translation, Vector3 scale)
        {
            double[, ,] gdata = new double[xSize, ySize, zSize];

            foreach (var par in _particles)
            {
                double level;
                //level = par.ColorField.Length();
                level = 0.09;

                Vector3 pos = par.Position - translation;

                pos.X = pos.X / scale.X;
                pos.Y = pos.Y / scale.Y;
                pos.Z = pos.Z / scale.Z;

                int x = (int)pos.X;
                int y = (int)pos.Y;
                int z = (int)pos.Z;

                if (x < xSize && y < ySize && z < zSize && x >= 0 && y >= 0 && z >= 0)
                {
                    gdata[x, y, z] += level;
                }

                for (int x2 = x - 1; x2 < x + 2; ++x2)
                {
                    for (int z2 = z - 1; z2 < z + 2; ++z2)
                    {
                        if (x2 < xSize && y < ySize && z2 < zSize && x2 >= 0 && y >= 0 && z2 >= 0)
                        {
                            gdata[x2, y, z2] += level / 2;
                        }
                    }
                }
            }

            return gdata;
        }

        #endregion

        public List<string> GetInfo()
        {
            List<string> fullInfo = new List<string>();
            string info;

            fullInfo.Add("");
            fullInfo.Add("Particles:");

            info = "Count: " + _particles.Count;
            fullInfo.Add(info);

            info = "Kernel size: " + H;
            fullInfo.Add(info);

            info = "Viscosity: " + _viscosityScale;
            fullInfo.Add(info);

            info = "Pressure: " + _pressureScale;
            fullInfo.Add(info);

            info = "Tension: " + _tensionScale;
            fullInfo.Add(info);

            return fullInfo;
        }

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
