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
        private Emitter Emitter;
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

            // Emitter
            Vector3 emitterPos = new Vector3(-2.5f, 15, 28);
            Box emitterBox = new Box(emitterPos, 3, 3, 3);
            Emitter = new Emitter(space, emitterPos, emitterBox, Vector3.UnitZ * -1);
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
                    Components.Add(new EntityModel(e, CubeModel, scaling, this));
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
            Emitter.Update();
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
        }
    }
}
