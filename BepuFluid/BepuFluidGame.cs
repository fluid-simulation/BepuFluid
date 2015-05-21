using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BEPUphysics;
using BEPUphysics.Entities;
using Vector3 = BEPUutilities.Vector3;

namespace BepuFluid
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BepuFluidGame : Game
    {
        GraphicsDeviceManager graphics;

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
        private ParticleManager Emitter;
        #endregion

        public BepuFluidGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Camera = new Camera(this, new Vector3(0, 3, -5), 5);

            InitializeSpace();

            base.Initialize();
        }

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
            Emitter = new ParticleManager(space, emitterPos, emitterBox, Vector3.UnitZ * -1);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //This 1x1x1 cube model will represent the box entities in the space.
            CubeModel = Content.Load<Model>("cube");
            SphereModel = Content.Load<Model>("sphere");

            var emitterBox = Emitter.Box;
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
            width = 0.1f;
            Box box3 = new Box(center, width, height, length);
            center.X -= 5;
            Box box4 = new Box(center, width, height, length);
            
            space.Add(box1);
            space.Add(box2);
            space.Add(box3);
            space.Add(box4);
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
            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || KeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            //Update the camera
            Camera.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            if (MouseState.LeftButton == ButtonState.Pressed)
            {
                for (int i = 0; i < 1; ++i)
                Emit();
            }

            //Steps the simulation forward one time step.
            space.Update();
            //Emitter.Update();
            base.Update(gameTime);
        }
        private void Emit()
        {
            var particle = Emitter.EmitParticle();
            var scaleMatrix = Matrix.CreateScale(particle.Radius);
            Components.Add(new EntityModel(particle, SphereModel, scaleMatrix, this));
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
            DrawMarchingCubes();
        }

        private double[, ,] GetGData()
        {
            if (gData == null)
            {
                var rand = new System.Random();
                double[, ,] gdata = new double[10, 10, 10];
                for (int x = 0; x < 10; ++x)
                {
                    for (int y = 0; y < 10; ++y)
                    {
                        for (int z = 0; z < 10; ++z)
                        {
                            gdata[x, y, z] = rand.NextDouble();
                        }
                    }
                }
                gData = gdata;
            }

            return gData;
        }
        double[, ,] gData;
        private void DrawMarchingCubes()
        {
            var gdata = GetGData();

            MarchingCubes.MarchingCubes.Poligonizator.Init(9, gdata, this.GraphicsDevice);
            var primitive = MarchingCubes.MarchingCubes.Poligonizator.Process(this.GraphicsDevice, 0.6);

           // Matrix world = BepuToXnaMatrix(Camera.WorldMatrix);
            Matrix view = BepuToXnaMatrix(Camera.ViewMatrix);
            Matrix projection = BepuToXnaMatrix(Camera.ProjectionMatrix);

            Matrix world = Matrix.CreateTranslation(0, 0, 0);
           //Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 3), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
           //Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 800f / 480f, 0.01f, 100f);

            if (primitive.VertexCount > 0)
            {
                primitive.InitializePrimitive(this.GraphicsDevice);
                primitive.Draw(world, view, projection, Color.Red);
            }

        }

        private Microsoft.Xna.Framework.Matrix BepuToXnaMatrix(BEPUutilities.Matrix m)
        {
            Matrix res = new Matrix(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
            return res;
        }
    }
}
