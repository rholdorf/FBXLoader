using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Game3D.SkinModels;

internal class DirectionLight
{
    public Vector3 Direction;
    public Vector3 DiffuseColor;
    public Vector3 SpecularColor;
}

internal class SkinFx
{
    private const int MAX_BONES = 180; // This should match number in custom SkinEffect.fx
    public Camera Cam; // reference to camera
    public Effect Fx;
    public Texture2D DefaultTex;
    public Vector4 DiffuseCol = Vector4.One;
    public Vector3 EmissiveCol = Vector3.Zero;
    public Vector3 SpecularCol = Color.LightYellow.ToVector3();
    public float SpecularPow = 32f;
    public Vector3 AmbientCol = Vector3.Zero;
    public bool FogEnabled;
    public DirectionLight[] Lights; // lights: key, fill, back 
    public float Alpha = 1f;
    public float FogStart;
    public float FogEnd = 1f;
    public Matrix World = Matrix.Identity;
    public Matrix WorldView = Matrix.Identity;

    public SkinFx(ContentManager Content, Camera Cam, string fx_filename, bool enableFog = false)
    {
        Lights = new DirectionLight[3];
        for (var i = 0; i < 3; i++) Lights[i] = new DirectionLight();
        this.Cam = Cam;
        Fx = Content.Load<Effect>(fx_filename);
        DefaultTex = Content.Load<Texture2D>("default_texture");
        var identityBones = new Matrix[MAX_BONES];
        for (var i = 0; i < MAX_BONES; i++)
        {
            identityBones[i] = Matrix.Identity;
        }

        SetBoneTransforms(identityBones);
        Fx.Parameters["TexDiffuse"].SetValue(DefaultTex);
        Fx.Parameters["DiffuseColor"].SetValue(DiffuseCol);
        Fx.Parameters["EmissiveColor"].SetValue(EmissiveCol);
        Fx.Parameters["SpecularColor"].SetValue(SpecularCol);
        Fx.Parameters["SpecularPower"].SetValue(SpecularPow);

        SetDefaultLighting();
        if (enableFog) ToggleFog();
    }

    /// <summary> Sets an array of skinning bone transform matrices. </summary>
    public void SetBoneTransforms(Matrix[] boneTransforms)
    {
        if ((boneTransforms == null) || (boneTransforms.Length == 0)) throw new ArgumentNullException("boneTransforms");
        if (boneTransforms.Length > MAX_BONES) throw new ArgumentException();
        Fx.Parameters["Bones"].SetValue(boneTransforms);
    }

    public void SetDefaultLighting()
    {
        var u = Cam.Up.Y; // I assume up is -Y or +Y
        AmbientCol = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);
        Lights[0].Direction = new Vector3(-0.5265408f, -0.5735765f * u, -0.6275069f); // Key light.
        Lights[0].DiffuseColor = new Vector3(1, 0.9607844f, 0.8078432f);
        Lights[0].SpecularColor = new Vector3(1, 0.9607844f, 0.8078432f);
        Lights[1].Direction = new Vector3(0.7198464f, 0.3420201f * u, 0.6040227f); // Fill light
        Lights[1].DiffuseColor = new Vector3(0.9647059f, 0.7607844f, 0.4078432f);
        Lights[1].SpecularColor = Vector3.Zero;
        Lights[2].Direction = new Vector3(0.4545195f, -0.7660444f * u, 0.4545195f); // Back light
        Lights[2].DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
        Lights[2].SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
        Fx.Parameters["LightDir1"].SetValue(Lights[0].Direction);
        Fx.Parameters["LightDiffCol1"].SetValue(Lights[0].DiffuseColor);
        Fx.Parameters["LightSpecCol1"].SetValue(Lights[0].SpecularColor);
        Fx.Parameters["LightDir2"].SetValue(Lights[1].Direction);
        Fx.Parameters["LightDiffCol2"].SetValue(Lights[1].DiffuseColor);
        Fx.Parameters["LightSpecCol2"].SetValue(Lights[1].SpecularColor);
        Fx.Parameters["LightDir3"].SetValue(Lights[2].Direction);
        Fx.Parameters["LightDiffCol3"].SetValue(Lights[2].DiffuseColor);
        Fx.Parameters["LightSpecCol3"].SetValue(Lights[2].SpecularColor);
    }

    public void SetDirectionalLight(int index, Vector3 direction, Color diffuse_color, Color specular_color)
    {
        if (index >= 3) return;
        Lights[index].Direction = direction;
        Lights[index].DiffuseColor = diffuse_color.ToVector3();
        Lights[index].SpecularColor = specular_color.ToVector3();
        switch (index)
        {
            case 0:
                Fx.Parameters["LightDir1"].SetValue(Lights[0].Direction);
                Fx.Parameters["LightDiffCol1"].SetValue(Lights[0].DiffuseColor);
                Fx.Parameters["LightSpecCol1"].SetValue(Lights[0].SpecularColor);
                break;
            case 1:
                Fx.Parameters["LightDir2"].SetValue(Lights[1].Direction);
                Fx.Parameters["LightDiffCol2"].SetValue(Lights[1].DiffuseColor);
                Fx.Parameters["LightSpecCol2"].SetValue(Lights[1].SpecularColor);
                break;
            case 2:
                Fx.Parameters["LightDir3"].SetValue(Lights[2].Direction);
                Fx.Parameters["LightDiffCol3"].SetValue(Lights[2].DiffuseColor);
                Fx.Parameters["LightSpecCol3"].SetValue(Lights[2].SpecularColor);
                break;
        }
    }

    public void SetFogStart(float fogStart)
    {
        FogEnabled = false;
        FogStart = fogStart;
        ToggleFog();
    }

    public void SetFogEnd(float fogEnd)
    {
        FogEnabled = false;
        FogEnd = fogEnd;
        ToggleFog();
    }

    public void SetFogColor(Color fogColor)
    {
        Fx.Parameters["FogColor"].SetValue(fogColor.ToVector3());
    }

    // T O G G L E   F O G 
    public void ToggleFog()
    {
        if (!FogEnabled)
        {
            if (FogStart == FogEnd)
            {
                Fx.Parameters["FogVector"].SetValue(new Vector4(0, 0, 0, 1));
            }
            else
            {
                // We want to transform vertex positions into view space, take the resulting Z value, then scale and offset according to the fog start/end distances.
                // Because we only care about the Z component, the shader can do all this with a single dot product, using only the Z row of the world+view matrix.
                var scale = 1f / (FogStart - FogEnd);
                var fogVector = new Vector4();
                fogVector.X = WorldView.M13 * scale;
                fogVector.Y = WorldView.M23 * scale;
                fogVector.Z = WorldView.M33 * scale;
                fogVector.W = (WorldView.M43 + FogStart) * scale;
                Fx.Parameters["FogVector"].SetValue(fogVector);
                FogEnabled = true;
            }
        }
        else
        {
            Fx.Parameters["FogVector"].SetValue(Vector4.Zero);
            FogEnabled = false;
        }
    }

    public void SetDrawParams(Camera cam, Texture2D texture = null, Texture2D normalMapTex = null, Texture2D specularTex = null)
    {
        Matrix.Multiply(ref World, ref cam.view, out WorldView); // (used with fog)
        Matrix worldInverse, worldInverseTranspose;
        Matrix.Invert(ref World, out worldInverse);
        Matrix.Transpose(ref worldInverse, out worldInverseTranspose);
        var diffuse = new Vector4();
        var emissive = new Vector3();
        diffuse.X = DiffuseCol.X * Alpha;
        diffuse.Y = DiffuseCol.Y * Alpha;
        diffuse.Z = DiffuseCol.Z * Alpha;
        diffuse.W = Alpha;
        emissive.X = (EmissiveCol.X + AmbientCol.X * DiffuseCol.X) * Alpha;
        emissive.Y = (EmissiveCol.Y + AmbientCol.Y * DiffuseCol.Y) * Alpha;
        emissive.Z = (EmissiveCol.Z + AmbientCol.Z * DiffuseCol.Z) * Alpha;
        Fx.Parameters["World"].SetValue(World);
        Fx.Parameters["WorldViewProj"].SetValue(World * cam.view_proj);
        Fx.Parameters["CamPos"].SetValue(cam.pos);
        if (texture != null)
            Fx.Parameters["TexDiffuse"].SetValue(texture);
        else
            Fx.Parameters["TexDiffuse"].SetValue(DefaultTex);
        //if (specularTex  != null) fx.Parameters["TexSpecular"].SetValue(specularTex);
        Fx.Parameters["WorldInverseTranspose"].SetValue(worldInverseTranspose);
        Fx.Parameters["DiffuseColor"].SetValue(diffuse);
        Fx.Parameters["EmissiveColor"].SetValue(emissive);
        if (normalMapTex == null)
        {
            Fx.CurrentTechnique = Fx.Techniques["Skin_Directional_Fog"];
        }
        else
        {
            Fx.Parameters["TexNormalMap"].SetValue(normalMapTex);
            Fx.CurrentTechnique = Fx.Techniques["Skin_NormalMapped_Directional_Fog"];
        }

        Fx.CurrentTechnique.Passes[0].Apply();
    }

    public void SetDiffuseCol(Vector4 diffuse)
    {
        DiffuseCol = diffuse;
    }

    public void SetEmissiveCol(Vector3 emissive)
    {
        EmissiveCol = emissive;
    }

    public void SetSpecularCol(Vector3 specular)
    {
        SpecularCol = specular;
        Fx.Parameters["SpecularColor"].SetValue(SpecularCol);
    }

    public void SetSpecularPow(float power)
    {
        SpecularPow = power;
        Fx.Parameters["SpecularPower"].SetValue(power);
    }

    public void SetShineAmplify(float amp)
    {
        Fx.Parameters["Shine_Amplify"].SetValue(amp);
    } // currently using to make eyes more shiny (triggered by low alpha)    
}