using Game3D.SkinModels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game3D;

public class Game1 : Game
{
    // DISPLAY
    private const int SCREEN_WIDTH = 1024, SCREEN_HEIGHT = 768; // TARGET FORMAT        
    private readonly GraphicsDeviceManager _graphics;
    private GraphicsDevice _gpu;
    private SpriteBatch _spriteBatch;
    private Camera _cam;

    // RECTANGLES
    private Rectangle _desktopRect;

    // RENDER TARGETS & TEXTURES
    private RenderTarget2D _mainTarget;

    // INPUT & UTILS
    private Input _inp;

    // MODELS & CHARACTERS
    private SkinModelLoader _skinModelLoader; // does the work of loading our characters                
    private SkinFx _skinFx; // controls for SkinEffect
    private SkinModel[] _hero; // main character
    const int IDLE = 0, WALK = 1, RUN = 2; // (could use enum but easier to index without casting) 
    private readonly Vector3 _heroPos = new(0, 1, 0);
    private Matrix _mtxHeroRotate;

    private readonly RasterizerState _rsCcw = new()
    {
        FillMode = FillMode.Solid,
        CullMode = CullMode.CullCounterClockwiseFace
    };
    
    private bool _init = true;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.IsBorderless = true;
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 14;
        _graphics.IsFullScreen = false;
        _graphics.PreferredDepthStencilFormat = DepthFormat.None;
        _graphics.ApplyChanges();
        Window.Position = new Point(0, 0);
        _gpu = GraphicsDevice;

        var pp = _gpu.PresentationParameters;
        _spriteBatch = new SpriteBatch(_gpu);
        _mainTarget = new RenderTarget2D(_gpu, SCREEN_WIDTH, SCREEN_HEIGHT, false, pp.BackBufferFormat, DepthFormat.Depth24);
        _desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        //new Rectangle(0, 0, _screenW, _screenH);

        // INPUT
        _inp = new Input(pp, _mainTarget);
        // INIT 3D             
        _cam = new Camera(_gpu, Vector3.Up, _inp);
        _hero = new SkinModel[3];

        base.Initialize();
    }

    protected override void LoadContent()
    {
        //Content.Load<SpriteFont>("Font");

        // { S K I N - M O D E L   L O A D  ----------------------------------------
        _skinFx = new SkinFx(Content, _cam, "SkinEffect"); // skin effect parameter controls     
        _skinModelLoader = new SkinModelLoader(Content, _gpu); // need for: runtime load FBX skinned model animations
        _skinModelLoader.SetDefaultOptions(0.1f, "default_gray"); // pad the animation a bit for smooth looping, set a debug texture (if no texture on a mesh)   

        // load animation (custom settings, size = 35%) 
        _hero[IDLE] = _skinModelLoader.Load("Kid/kid_idle.fbx", "Kid", true, 3, _skinFx, rescale: 0.35f);

        // I n i t   P l a y e r:
        _mtxHeroRotate = Matrix.CreateFromYawPitchRoll(MathHelper.Pi, 0, 0); // let's have the character facing the camera at first          
        _skinFx.World = _mtxHeroRotate;
    }

    protected override void Update(GameTime gameTime)
    {
        _inp.Update();
        if (_init)
        {
            // INITIALIZES STARTING ANIMATIONS, CHARACTERS, AND LEVEL (for whatever level we're on) ____________________________ 
            _hero[IDLE].BeginAnimation(0, gameTime); // begin playing animation
            _init = false;
        }

        if (_inp.back_down || _inp.KeyDown(Keys.Escape)) Exit(); // change to menu for exit later

        _cam.UpdatePlayerCam(_heroPos);
        _hero[IDLE].Update(gameTime);
        //hero[WALK].Update(gameTime);
        //hero[RUN].Update(gameTime);

        base.Update(gameTime);
    }


    void Set3DStates()
    {
        _gpu.BlendState = BlendState.NonPremultiplied;
        _gpu.DepthStencilState = DepthStencilState.Default;
        if (_gpu.RasterizerState.CullMode == CullMode.None) _gpu.RasterizerState = _rsCcw;
    }

    protected override void Draw(GameTime gameTime)
    {
        _gpu.SetRenderTarget(_mainTarget);
        _gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 1.0f, 0);

        Set3DStates();

        //hero[IDLE].Draw(cam, skinFx.world);  // normal way

        // RENDER CHARACTER                    // specialized way
        var kid = _hero[IDLE];
        for (var i = 0; i < kid.Meshes.Length; i++)
        {
            var mesh = kid.Meshes[i];
            if (mesh.Opacity < 0.6f) continue;
            _skinFx.SetDiffuseCol(Color.White.ToVector4());
            _skinFx.SetSpecularCol(new Vector3(0.2f, 0.3f, 0.05f));
            _skinFx.SetSpecularPow(256f);
            _skinFx.World = _mtxHeroRotate * Matrix.CreateTranslation(_heroPos); // ***MUST DO THIS BEFORE: SET DRAW PARAMS***
            // (If we wanted, we could swap out a shirt or something by setting skinFx.texture = ...)
            // TO DO: set up a DrawMesh that takes a custom transforms list for animation blending
            kid.DrawMesh(i, _cam, _skinFx.World, false);
        }

        //RENDER SHINY TRANSPARENT STUFF(eyes )
        _skinFx.SetShineAmplify(100f);
        for (var i = 0; i < kid.Meshes.Length; i++)
        {
            var mesh = kid.Meshes[i];
            if (mesh.Opacity >= 0.6f) continue;
            // Make adjustments for eyes: 
            var oldAlpha = _skinFx.Alpha;
            _skinFx.Alpha = 0.2f;
            _skinFx.SetDiffuseCol(Color.Blue.ToVector4());
            _skinFx.SetSpecularCol(new Vector3(100f, 100f, 100f));
            // TO DO: custom DrawMesh that takes a custom blendTransform
            _hero[IDLE].DrawMesh(i, _cam, _skinFx.World, false);
            _skinFx.Alpha = oldAlpha;
        }

        _skinFx.SetShineAmplify(1f);

        // DRAW MAIN TARGET TO BACKBUFFER -------------------------------------------------------------------------------------------------------
        _gpu.SetRenderTarget(null);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(_mainTarget, _desktopRect, Color.White);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}