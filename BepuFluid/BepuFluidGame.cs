using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BEPUphysics;
using BEPUphysics.Entities;
using Vector3 = BEPUutilities.Vector3;
using BepuFluid.Utils;
using System.Collections.Generic;
using System;

namespace BepuFluid
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BepuFluidGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        InfoDrawer infoDrawer;
        InputHelper inputHelper;

        /// <summary>
        /// World in which the simulation runs.
        /// </summary>
        Space space;
        /// <summary>
        /// Graphical model to use for the boxes in the scene.
        /// </summary>
        public Model CubeModel;
        public Model SphereModel;
        /// <summary>
        /// Controls the viewpoint and how the user can see the world.
        /// </summary>
        public Camera Camera;

        /// <summary>
        /// Contains the latest snapshot of the keyboard's input state.
        /// </summary>
        public KeyboardState KeyboardState;
        /// <summary>
        /// Contains the latest snapshot of the mouse's input state.
        /// </summary>
        public MouseState MouseState;

        #region Particles
        private ParticleManager particleManager;
        private bool _updateParticles = false;

        private void Emit()
        {
            var particle = particleManager.EmitParticle();
            var scaleMatrix = Matrix.CreateScale(particle.Radius);
            //Components.Add(new EntityModel(particle, SphereModel, scaleMatrix, this));
        }
        #endregion

        public BepuFluidGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 900;
            graphics.PreferredBackBufferHeight = 675;
            Content.RootDirectory = "Content";

            inputHelper = new InputHelper();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Camera = new Camera(this, new Vector3(0, 10, -5), 5);

            InitializeSpace();

            base.Initialize();
        }

        #region Space and Boxes
        private void InitializeSpace()
        {
            //Construct a new space for the physics simulation to occur within.
            space = new Space();

            //Set the gravity of the simulation by accessing the simulation settings of the space.
            space.ForceUpdater.Gravity = new Vector3(0, -9.81f, 0);

            //Make a box representing the ground and add it to the space.
            space.Add(new Box(Vector3.Zero, 30, 1, 30));

            // rynna
            AddThrough();

            // boundary box
            AddBoundaryBox();

            // Emitter
            Vector3 emitterPos = new Vector3(-2.5f, 15, 28);
            Box emitterBox = new Box(emitterPos, 3, 3, 3);
            particleManager = new ParticleManager(space, emitterPos, emitterBox, Vector3.UnitZ * -1, TRANSLATION);
        }

        private void AddThrough()
        {
            Vector3 center = new Vector3(0, 10, 20);
            int width = 10;
            int length = 20;
            int angleX = 0;
            int angleZ = 100;
            Through through = new Through(center, width, length, angleX, angleZ);
            space.Add(through.Box1);
            space.Add(through.Box2);
        }

        private void AddBoundaryBox()
        {
            float width = 20;
            float height = 3;
            float length = 0.1f;
            Vector3 center = new Vector3(0, 0, 7);
            Box box1 = new Box(center, width, height, length);
            center.Z += 8;
            Box box2 = new Box(center, width, height, length);

            center.Z -= 3;
            length = 10;
            width = 0.3f;
            Box box3 = new Box(center, width, height, length);
            center.X -= 5;
            Box box4 = new Box(center, width, height, length);

            space.Add(box1);
            space.Add(box2);
            space.Add(box3);
            space.Add(box4);
        }
        #endregion

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //This 1x1x1 cube model will represent the box entities in the space.
            CubeModel = Content.Load<Model>("cube");
            SphereModel = Content.Load<Model>("sphere");

            var emitterBox = particleManager.EmitterBox;
            Matrix emitterScaling = Matrix.CreateScale(emitterBox.Width, emitterBox.Height, emitterBox.Length);
            Components.Add(new EntityModel(emitterBox, CubeModel, emitterScaling, this));

            //Go through the list of entities in the space and create a graphical representation for them.
            foreach (Entity e in space.Entities)
            {
                Box box = e as Box;
                if (box != null) //This won't create any graphics for an entity that isn't a box since the model being used is a box.
                {
                    Matrix scaling = Matrix.CreateScale(box.Width, box.Height, box.Length); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    //Add the drawable game component for this entity to the game.
                    var model = new EntityModel(e, CubeModel, scaling, this);
                    model.IsOpaque = true;
                    Components.Add(model);
                }
            }

            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            infoDrawer = new InfoDrawer(this.Content);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            inputHelper.Update();
            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();

            //Update the camera
            Camera.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (MouseState.LeftButton == ButtonState.Pressed)
            {
                for (int i = 0; i < 1; ++i)
                    Emit();
            }

            ProcessInput();

            //Steps the simulation forward one time step.
            space.Update();

            if (_updateParticles)
                particleManager.Update();

            base.Update(gameTime);
        }

        private void ProcessInput()
        {

            if (inputHelper.IsNewPress(Keys.F3))
            {
                infoDrawer.ToggleFullInfo();
            }

            if (inputHelper.IsNewPress(Keys.F5))
            {
                if (DIM_SIZE > 1)
                {
                    DIM_SIZE = DIM_SIZE / 2;
                }
            }
            if (inputHelper.IsNewPress(Keys.F6))
            {
                if (DIM_SIZE < 64)
                {
                    DIM_SIZE = DIM_SIZE * 2;
                }
            }

            if (inputHelper.IsNewPress(Keys.F1))
            {
                _updateParticles = !_updateParticles;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawMarchingCubes();

            spriteBatch.Begin();

            UpdateFps();

            var info = GetInfo();

            infoDrawer.Draw(spriteBatch, info);

            base.Draw(gameTime);
            spriteBatch.End();
        }

        #region Info Display
        int frames = 0;
        double fps = 0;
        DateTime prev, now;

        private void UpdateFps()
        {
            if (frames == 0)
            {
                prev = DateTime.Now;
            }

            frames++;
            now = DateTime.Now;

            TimeSpan timeDiff = now - prev;
            int elapsed = timeDiff.Milliseconds;


            if (elapsed > 0)
                fps = 1000 * (double)frames / elapsed;

            if (frames > 4)
            {
                frames = 0;
            }
        }

        private List<string> GetInfo()
        {
            var infoList = new List<string>();
            string info;

            info = "Press F1 to toggle particles' physics (currently ";
            info += _updateParticles ? "ON" : "OFF";
            info += ")";
            infoList.Add(info);

            infoList.Add("");
            infoList.Add("Particles:");

            if (_updateParticles)
            {
                infoList.AddRange(particleManager.GetInfo());
            }
            else
            {
                info = "Count: " + particleManager.ParticlesCount;
                infoList.Add(info);
            }

            infoList.Add("");

            info = "Marching Cubes:";
            infoList.Add(info);

            info = "Dimensions: " + DIM_SIZE + ",   F5: -  F6: +";
            infoList.Add(info);

            info = "IsoLevel: " + ISO_LEVEL;
            infoList.Add(info);

            info = "Vertex Count: " + _vertexCount;
            infoList.Add(info);

            infoList.Add("");

            info = "Frames per Second: " + fps;
            infoList.Add(info);

            return infoList;
        }

        #endregion

        #region MarchingCubes
        private int DIM_SIZE = 64;
        private double ISO_LEVEL = 0.15;
        private Vector3 TRANSLATION = new Vector3(-7, 0, 7);

        private int _vertexCount = 0;

        private void DrawMarchingCubes()
        {
            float xScale = (float)12  / DIM_SIZE;
            float yScale = (float)17 / DIM_SIZE;
            float zScale = (float)25 / DIM_SIZE;
            Vector3 scale = new Vector3(xScale, yScale, zScale);
            var gdata = particleManager.GetParticlesGData(DIM_SIZE, DIM_SIZE, DIM_SIZE, TRANSLATION, scale);
            //var gdata = getRandomGdata(dimSize);

            MarchingCubes.Poligonizator.Init(DIM_SIZE - 1, gdata, this.GraphicsDevice);
            var primitive = MarchingCubes.Poligonizator.Process(this.GraphicsDevice, ISO_LEVEL);

            Matrix view = BepuToXnaMatrix(Camera.ViewMatrix);
            Matrix projection = BepuToXnaMatrix(Camera.ProjectionMatrix);
            //Matrix world = Matrix.CreateTranslation(0, 0, 0);
            Matrix world = Matrix.CreateTranslation(TRANSLATION.X, TRANSLATION.Y, TRANSLATION.Z);
            world = Matrix.CreateScale(scale.X, scale.Y, scale.Z) * world;
            

            //Matrix.Multiply(world, 0.07f);
            _vertexCount = primitive.VertexCount;
            if (primitive.VertexCount > 0)
            {
                primitive.InitializePrimitive(this.GraphicsDevice);
                primitive.Draw(world, view, projection, Color.Blue);
            }

        }

        private double[, ,] getRandomGdata(int dimSize)
        {
            double[,,] gdata = new double[dimSize, dimSize, dimSize];

            var rand = new System.Random();
            for(int x = 0; x < dimSize; ++x)
            {
                for (int y = 0; y < dimSize; ++y)
                {
                    for (int z = 0; z < dimSize; ++z)
                    {
                        gdata[x, y, z] = rand.NextDouble();
                    }
                }                
            }
            return gdata;
        }
        private Microsoft.Xna.Framework.Vector3 BepuToXnaVector(Vector3 v)
        {
            return new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
        }
        private Microsoft.Xna.Framework.Matrix BepuToXnaMatrix(BEPUutilities.Matrix m)
        {
            Matrix res = new Matrix(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
            return res;
        }

        #endregion
    }
}
