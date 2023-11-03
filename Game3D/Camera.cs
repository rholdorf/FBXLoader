using System;
using MathAndCollisions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game3D; // C A M E R A

internal class Camera
{
    private const float CAM_HEIGHT = 12; // default up-distance from player's root position (depends on character size - 80 up in y direction to look at head)
    private const float HEAD_OFFSET = 12;
    private const float FAR_PLANE = 2000; // farthest camera can see (clip out things further away) 
    public Vector3 pos, target; // camera position, target to look at
    public Matrix view, proj, view_proj; // viewing/projection transforms used to transform world vertices to screen coordinates relative to camera
    public Vector3 Up; // up direction for camera and world geometry (may depend on imported geometry's up direction [ie: is up -1 or 1 in y direction] 
    private float _currentAngle; // player-relative angle offset of camera (will explain more later)
    private float _angleVelocity; // speed of camera rotation
    private float _radius = 100.0f; // distance of camera from player (to look at)
    private Vector3 _unitDirection; // direction of camera (normalized to distance of 1 unit) 
    private readonly Input _inp; // allow access to input class so can control camera from this class if want to
    private readonly Maf _maf;

    public Camera(GraphicsDevice gpu, Vector3 upDirection, Input input)
    {
        Up = upDirection;
        _inp = input;
        pos = new Vector3(20, 12, -90);
        target = Vector3.Zero;
        _maf = new Maf();
        view = Matrix.CreateLookAt(pos, target, Up);
        proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, gpu.Viewport.AspectRatio, 1.0f, FAR_PLANE);
        view_proj = view * proj;
        _unitDirection = view.Forward;
        _unitDirection.Normalize();
    }

    // M O V E   C A M E R A   (simple manual camera movement [set pos to put at an exact position] - [probably won't use this] )
    public void MoveCamera(Vector3 move)
    {
        pos += move;
        view = Matrix.CreateLookAt(pos, target, Up);
        view_proj = view * proj;
    }

    // U P D A T E   T A R G E T   (use this mostly [new_target should be player position usually] - [probably will use] )
    private void UpdateTarget(Vector3 newTarget)
    {
        target = newTarget;
        view = Matrix.CreateLookAt(pos, target, Up);
        view_proj = view * proj;
    }

    // U P D A T E    P L A Y E R    C A M 
    public void UpdatePlayerCam(Vector3 heroPos)
    {
        if (target == Vector3.Zero) target = heroPos;

        var camPadLeftRight = _inp.gp.ThumbSticks.Right.X;
        var camPadUpDown = _inp.gp.ThumbSticks.Right.Y;

        var forward = heroPos - pos; // vector from camera pointing to hero
        var x1 = forward.X;
        var y1 = forward.Y;
        var z1 = forward.Z;

        // GET UP-DOWN LOOK                        
        pos.Y -= (pos.Y - (heroPos.Y + CAM_HEIGHT)) * 0.06f; //pos.Y -= (pos.Y - (hero_pos.Y - CAM_HEIGHT)) * 0.06f;  // for upside-down camera

        if (camPadUpDown > Input.DEAD_ZONE || camPadUpDown < -Input.DEAD_ZONE)
        {
            if (camPadUpDown < -Input.DEAD_ZONE * 2)
            {
                var targHeight = heroPos.Y + CAM_HEIGHT + 120f; //float targ_height = hero_pos.Y - CAM_HEIGHT - 120f;              // flipped camera version      //if (pos.Y > targ_height) pos.Y -= (pos.Y - targ_height) * 0.06f; // "  
                if (pos.Y < targHeight) pos.Y += (targHeight - pos.Y) * 0.06f;
            }

            if (camPadUpDown > Input.DEAD_ZONE * 2)
            {
                if (pos.Y > heroPos.Y - 5) pos.Y -= (pos.Y - (heroPos.Y - 5)) * 0.06f;
            } //if (CamPad_UpDown > 0.0f) { if (pos.Y < hero_pos.Y + 5) pos.Y += (hero_pos.Y + 5 - pos.Y) * 0.06f; } // flipped cam version
        }

        // ROTATE CAMERA (accelerate rotation in a direction)
        if (_inp.KeyDown(Keys.OemPeriod))
        {
            _angleVelocity -= Maf.RADIANS_QUARTER;
            if (_angleVelocity < -Maf.RADIANS_1) _angleVelocity = -Maf.RADIANS_1; // ANALOG ROTATE CAMERA
        }

        if (_inp.KeyDown(Keys.OemComma))
        {
            _angleVelocity += Maf.RADIANS_QUARTER;
            if (_angleVelocity > Maf.RADIANS_1) _angleVelocity = Maf.RADIANS_1; // ANALOG ROTATE CAMERA
        }

        if ((camPadLeftRight > Input.DEAD_ZONE) || (camPadLeftRight < -Input.DEAD_ZONE))
        {
            if (camPadLeftRight < 0f)
            {
                _angleVelocity -= camPadLeftRight * 0.0038f;
                if (_angleVelocity < -Maf.RADIANS_1) _angleVelocity = -Maf.RADIANS_1; // ANALOG ROTATE CAMERA
            }

            if (camPadLeftRight > 0f)
            {
                _angleVelocity -= camPadLeftRight * 0.0038f;
                if (_angleVelocity > Maf.RADIANS_1) _angleVelocity = Maf.RADIANS_1; // ANALOG ROTATE CAMERA
            }
        }

        // G E T   N E W   R O T A T I O N   A N G L E  ( and update camera position )
        _radius = (float)Math.Sqrt(x1 * x1 + z1 * z1);
        if (_angleVelocity != 0.0f)
        {
            _currentAngle = Maf.Calculate2DAngleFromZero(-x1, -z1); // get the angle
            _currentAngle += _angleVelocity; // add aditional angle velocity
            _currentAngle = Maf.ClampAngle(_currentAngle);
            pos.X = heroPos.X + _radius * (float)Math.Cos(_currentAngle); // camera is at hero_pos + radius(distance) at current angle (2D rotation around Y axis)
            pos.Z = heroPos.Z + _radius * (float)Math.Sin(_currentAngle);
            _angleVelocity *= 0.9f;
        }

        // C A M E R A   Z O O M   (move camera toward player if too far away)            
        _unitDirection = forward;
        _unitDirection.Normalize();
        var adjust = 0.02f;
        if ((_radius > 400) || (_radius < 40)) adjust = 1f;
        pos.X += _unitDirection.X * (_radius - 40f) * adjust;
        pos.Z += _unitDirection.Z * (_radius - 40f) * adjust;

        target.X += (heroPos.X - target.X) * 0.1f;
        target.Y += (heroPos.Y + HEAD_OFFSET - target.Y) * 0.1f;
        target.Z += (heroPos.Z - target.Z) * 0.1f;
        UpdateTarget(target);
    }
}