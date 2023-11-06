using System;
using MathAndCollisions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game3D;

internal class Camera
{
    private const float CAM_HEIGHT = 12; // default up-distance from player's root position (depends on character size - 80 up in y direction to look at head)
    private const float HEAD_OFFSET = 12;
    private const float FAR_PLANE = 20000; // farthest camera can see (clip out things further away) 
    public Vector3 Pos; // camera position, target to look at
    private Vector3 _target; // camera position, target to look at
    public Matrix View; // viewing/projection transforms used to transform world vertices to screen coordinates relative to camera
    private readonly Matrix _proj; // viewing/projection transforms used to transform world vertices to screen coordinates relative to camera
    public Matrix ViewProj; // viewing/projection transforms used to transform world vertices to screen coordinates relative to camera
    public Vector3 Up; // up direction for camera and world geometry (may depend on imported geometry's up direction [ie: is up -1 or 1 in y direction] 
    private float _currentAngle; // player-relative angle offset of camera (will explain more later)
    private float _angleVelocity; // speed of camera rotation
    private float _radius = 4000.0f; // distance of camera from player (to look at)
    private Vector3 _unitDirection; // direction of camera (normalized to distance of 1 unit) 
    private readonly InputMonitor _inp; // allow access to input class so can control camera from this class if want to

    public Camera(GraphicsDevice gpu, Vector3 upDirection, InputMonitor inputMonitor)
    {
        Up = upDirection;
        _inp = inputMonitor;
        Pos = new Vector3(2000, 1002, -90);
        _target = Vector3.Zero;
        View = Matrix.CreateLookAt(Pos, _target, Up);
        _proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, gpu.Viewport.AspectRatio, 1.0f, FAR_PLANE);
        ViewProj = View * _proj;
        _unitDirection = View.Forward;
        _unitDirection.Normalize();
    }

    private void UpdateTarget(Vector3 newTarget)
    {
        _target = newTarget;
        View = Matrix.CreateLookAt(Pos, _target, Up);
        ViewProj = View * _proj;
    }

    // U P D A T E    P L A Y E R    C A M 
    public void UpdatePlayerCam(Vector3 heroPos)
    {
        if (_target == Vector3.Zero) _target = heroPos;

        var forward = heroPos - Pos; // vector from camera pointing to hero
        var x1 = forward.X;
        var z1 = forward.Z;

        // GET UP-DOWN LOOK                        
        Pos.Y -= (Pos.Y - (heroPos.Y + CAM_HEIGHT)) * 0.06f; //pos.Y -= (pos.Y - (hero_pos.Y - CAM_HEIGHT)) * 0.06f;  // for upside-down camera

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

        if (_inp.KeyDown(Keys.OemPlus))
        {
            Pos.X -= 10f;
        }
        
        if (_inp.KeyDown(Keys.OemMinus))
        {
            Pos.X += 10f;
        }        

        // G E T   N E W   R O T A T I O N   A N G L E  ( and update camera position )
        _radius = (float)Math.Sqrt(x1 * x1 + z1 * z1);
        if (_angleVelocity != 0.0f)
        {
            _currentAngle = Maf.Calculate2DAngleFromZero(-x1, -z1); // get the angle
            _currentAngle += _angleVelocity; // add more angle velocity
            _currentAngle = Maf.ClampAngle(_currentAngle);
            Pos.X = heroPos.X + _radius * (float)Math.Cos(_currentAngle); // camera is at hero_pos + radius(distance) at current angle (2D rotation around Y axis)
            Pos.Z = heroPos.Z + _radius * (float)Math.Sin(_currentAngle);
            _angleVelocity *= 0.9f;
        }

        // C A M E R A   Z O O M   (move camera toward player if too far away)            
        /*_unitDirection = forward;
        _unitDirection.Normalize();
        var adjust = 0.02f;
        if (_radius is > 400 or < 40) adjust = 1f;
        Pos.X += _unitDirection.X * (_radius - 40f) * adjust;
        Pos.Z += _unitDirection.Z * (_radius - 40f) * adjust;

        _target.X += (heroPos.X - _target.X) * 0.1f;
        _target.Y += (heroPos.Y + HEAD_OFFSET - _target.Y) * 0.1f;
        _target.Z += (heroPos.Z - _target.Z) * 0.1f;*/
        UpdateTarget(_target);
    }
}