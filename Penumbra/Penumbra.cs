using System;
using System.Collections.ObjectModel;
using System.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Penumbra
{
    /// <summary>
    /// GPU based 2D lighting and shadowing engine with penumbra support. Operates with
    /// <see cref="Light"/>s and shadow <see cref="Hull"/>s, where light is a
    /// colored light source which casts shadows on shadow hulls that are outlines of scene
    /// geometry (polygons) impassable by light.
    /// </summary>
    /// <remarks>
    /// Before rendering scene, ensure to call <c>PenumbraComponent.BeginDraw</c> to swap render target
    /// in order for the component to be able to later apply generated lightmap.
    /// </remarks>
    public class Penumbra
    {
        private readonly PenumbraEngine _engine = new PenumbraEngine();
        private ContentManager _content;

        private bool _initialized;
        private bool _beginDrawCalled;

		public Game Game
		{
			get;
			private set;
		}

		public GraphicsDevice GraphicsDevice
		{
			get
			{
				return Game.GraphicsDevice;
			}
		}

        /// <summary>
        /// Constructs a new instance of <see cref="PenumbraComponent"/>.
        /// </summary>
        /// <param name="game">Game object to associate the engine with.</param>
        public Penumbra(Game game)
        {
			Game = game;
        }

        /// <summary>
        /// Gets or sets if debug outlines should be drawn for shadows and light sources and
        /// if logging is enabled.
        /// </summary>
        public bool Debug
        {
            get { return _engine.Debug; }
            set { _engine.Debug = value; }
        }

        /// <summary>
        /// Gets or sets the ambient color of the scene. Color is in non-premultiplied format.
        /// </summary>
        public Color AmbientColor
        {
            get { return _engine.AmbientColor; }
            set { _engine.AmbientColor = value; }
        }

        /// <summary>
        /// Gets or sets the custom transformation matrix used by the component.
        /// </summary>
        public Matrix Transform
        {
            get { return _engine.Camera.Custom; }
            set { _engine.Camera.Custom = value; }
        }

        /// <summary>
        /// Gets or sets if this component is used with <see cref="SpriteBatch"/> and its transform should
        /// be automatically applied. Default value is <c>true</c>.
        /// </summary>
        public bool SpriteBatchTransformEnabled
        {
            get { return _engine.Camera.SpriteBatchTransformEnabled; }
            set { _engine.Camera.SpriteBatchTransformEnabled = value; }
        }

        /// <summary>
        /// Gets the list of lights registered with the engine.
        /// </summary>
		public System.Collections.Generic.List<Light> Lights
		{
			get { return _engine.Lights; }
			set { _engine.Lights = value; }
		}

        /// <summary>
        /// Gets the list of shadow hulls registered with the engine.
        /// </summary>
		public HullList Hulls
		{
			get { return _engine.Hulls; }
			set { _engine.Hulls = value; }
		}

        /// <summary>
        /// Gets the diffuse map render target used by Penumbra.
        /// </summary>
        public RenderTarget2D DiffuseMap => _engine.Textures.DiffuseMap;

        /// <summary>
        /// Gets the lightmap render target used by Penumbra.
        /// </summary>
        public RenderTarget2D LightMap => _engine.Textures.Lightmap;

        /// <summary>
        /// Explicitly initializes the engine. This should only be called if the
        /// component was not added to the game's components list through <c>Components.Add</c>.
        /// </summary>
        public void Initialize()
        {
			var deviceManager = (GraphicsDeviceManager)Game.Services.GetService(typeof(IGraphicsDeviceManager));
            _content = new ResourceContentManager(Game.Services,
#if WINDOWSDX
                new ResourceManager("Penumbra.Resource.WindowsDX", typeof(PenumbraComponent).Assembly)
#elif DESKTOPGL
                new ResourceManager("Penumbra.Resource.DesktopGL", typeof(Penumbra).Assembly)
#endif
            );
            _engine.Load(
				GraphicsDevice, 
				deviceManager, 
				Game.Window,
				Game.Content.Load<Effect>("shaders/PenumbraHull"),
                Game.Content.Load<Effect>("shaders/PenumbraLight"),
                Game.Content.Load<Effect>("shaders/PenumbraShadow"),
                Game.Content.Load<Effect>("shaders/PenumbraTexture")
			);
            _initialized = true;
        }

        /// <summary>
        /// Sets up the lightmap generation sequence. This should be called before Draw.
        /// </summary>
        public void BeginDraw()
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    $"{nameof(Penumbra)} is not initialized. Make sure to call {nameof(Initialize)} when setting up a game.");

			_engine.PreRender();
            _beginDrawCalled = true;
        }

        /// <summary>
        /// Generates the lightmap, blends it with whatever was drawn to the scene between the
        /// calls to BeginDraw and this and presents the result to the backbuffer.
        /// </summary>
        public void Draw()
        {
            if (!_beginDrawCalled)
                throw new InvalidOperationException(
                    $"{nameof(BeginDraw)} must be called before rendering a scene to be lit and calling {nameof(Draw)}.");

			_engine.Render();
            _beginDrawCalled = false;
        }

		public void Refresh()
		{
			_engine.Textures.Refresh();
		}

        protected void UnloadContent()
        {
            _engine.Dispose();
            _content?.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                UnloadContent();
        }
    }
}