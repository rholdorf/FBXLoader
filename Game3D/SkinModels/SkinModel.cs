//#define USING_COLORED_VERTICES  // uncomment this in both SkinModel and SkinModelLoader if using

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// ASSIMP INSTRUCTIONS:
// AssimpNET is (cross platform) .NET wrapper for Open Asset Import Library 
// Add the AssimpNET nuget to your solution:
// - in the solution explorer, right click on the project
// - select manage nuget packages
// - click browse
// - type in assimpNET and install it to the solution and project via the checkbox on the right.

// THIS IS BASED ON WORK BY:  WIL MOTIL (a modified slightly older version)
// https://github.com/willmotil/MonoGameUtilityClasses

namespace Game3D.SkinModels
{
    class SkinModel
    {
        private readonly GraphicsDevice _gpu;
        private readonly SkinFx _skinFx; // using to control SkinEffect             
        private const int MAX_BONES = 180;
        private readonly Matrix[] _skinShaderMatrices; // these are the real final bone matrices they end up on the shader
        public SkinMesh[] Meshes;
        public ModelNode RootNodeOfTree; // actual model root node - base node of the model - from here we can locate any node in the chain        

        // animations
        public readonly List<RigAnimation> Animations = new();
        private int _currentAnim;
        private bool _animationRunning;
        private const bool LOOP_ANIMATION = true;
        private float _timeStart;
        private float _currentAnimFrameTime;
        private float _overrideAnimFrameTime = -1; // mainly for testing to step thru each frame

        public SkinModel(GraphicsDevice gpu, SkinFx skinEffect)
        {
            _gpu = gpu;
            _skinFx = skinEffect;
            _skinShaderMatrices = new Matrix[MAX_BONES];
            ResetShaderMatrices();
        }

        private void ResetShaderMatrices()
        {
            for (var i = 0; i < MAX_BONES; i++)
            {
                _skinShaderMatrices[i] = Matrix.Identity;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_animationRunning) UpdateModelAnimations(gameTime); // determine local transforms for animation
            UpdateNodes(RootNodeOfTree); // update the skeleton 
            UpdateMeshAnims(); // update any regular mesh animations
        }

        ///<summary> Gets the animation frame (based on elapsed time) for all nodes and loads them into the model node transforms. </summary>
        private void UpdateModelAnimations(GameTime gameTime)
        {
            if (Animations.Count <= 0 || _currentAnim >= Animations.Count) return;

            AnimationTimeLogic(gameTime); // process what to do based on animation time (frames, duration, complete | looping)

            var cnt = Animations[_currentAnim].AnimatedNodes.Count; // loop thru animated nodes
            for (var n = 0; n < cnt; n++)
            {
                var animNode = Animations[_currentAnim].AnimatedNodes[n]; // get animation keyframe lists (each animNode)
                var node = animNode.NodeRef; // get bone associated with this animNode (could be mesh-node) 
                node.LocalMtx = Animations[_currentAnim].Interpolate(_currentAnimFrameTime, animNode); // interpolate keyframes (animate local matrix) for this bone
            }
        }

        /// <summary> What to do at a certain animation time. </summary>
        private void AnimationTimeLogic(GameTime gameTime)
        {
            _currentAnimFrameTime = (float)gameTime.TotalGameTime.TotalSeconds - _timeStart; // *.1f; // if we want to purposely slow it for testing
            var animTotalDuration = (float)Animations[_currentAnim].DurationInSeconds + (float)Animations[_currentAnim].DurationInSecondsAdded; // add extra for looping

            // if we need to see a single frame; let us override the current frame
            if (_overrideAnimFrameTime >= 0f)
            {
                _currentAnimFrameTime = _overrideAnimFrameTime;
                if (_overrideAnimFrameTime > animTotalDuration) _overrideAnimFrameTime = 0f;
            }

            // Animation time exceeds total duration.
            if (_currentAnimFrameTime > animTotalDuration)
            {
                // LOOP ANIMATION                
                _currentAnimFrameTime -= animTotalDuration; // loop back to start
                _timeStart = (float)gameTime.TotalGameTime.TotalSeconds; // reset startTime
            }
        }

        /// <summary> Updates the skeleton (combined) after updating the local animated transforms </summary>
        private void UpdateNodes(ModelNode node)
        {
            // if there's a parent, we can add the local bone onto it to get the resulting bone location in skeleton:
            if (node.Parent != null)
                node.CombinedMtx = node.LocalMtx * node.Parent.CombinedMtx;
            else
                node.CombinedMtx = node.LocalMtx; // no parent so just provide the local matrix transform

            // loop thru the flat-list of bones for this node (bone could effect more than 1 mesh):
            for (var i = 0; i < node.UniqueMeshBones.Count; i++)
            {
                var bn = node.UniqueMeshBones[i]; // refers to the bone in uniqueMeshBones list (holds mesh#, bone#, etc)

                // Update the shader matrix (final bone transform) - we combine with the offset matrix to negate the original inverse we used 
                // to be able to do local vertex transforms correctly (which we did because vertices start out relative to a bind pose which we need to find a 
                // version where that's not so (for local transforms to work correctly) [ thus we used the inverse bind on the original bind-pose bones ]) 
                // So by adding the offset_mtx back on, the vertices will be transformed in a bind-relative way and reach the correct destination

                Meshes[bn.MeshIndex].ShaderMatrices[bn.BoneIndex] = bn.OffsetMtx * node.CombinedMtx; // converts resulting vert transforms back to bind-pose-relative space
            }

            foreach (var n in node.Children) UpdateNodes(n); // do same for children
        }

        /// In draw, this should enable us to call on this in relation to the world transform.
        private void UpdateMeshAnims()
        {
            if (Animations.Count <= 0) return;
            for (var i = 0; i < Meshes.Length; i++)
            {
                // try to handle when we just have mesh transforms                                                      
                if (Animations[_currentAnim].AnimatedNodes.Count > 0)
                {
                    // clear out the combined transforms
                    Meshes[i].NodeWithAnimTrans.CombinedMtx = Matrix.Identity;
                }
            }
        }

        public void BeginAnimation(int animationIndex, GameTime gameTime)
        {
            _timeStart = (float)gameTime.TotalGameTime.TotalSeconds; // capture the start time
            _currentAnim = animationIndex; // set current animation
            _animationRunning = true;
        }

        private void AssignMaterials(SkinMesh m, bool useMaterialSpec)
        {
            _skinFx.AmbientCol.X = m.Ambient.X;
            _skinFx.AmbientCol.Y = m.Ambient.Y;
            _skinFx.AmbientCol.Z = m.Ambient.Z; //Vec4 to Vec3
            _skinFx.EmissiveCol.X = m.Emissive.X;
            _skinFx.EmissiveCol.Y = m.Emissive.Y;
            _skinFx.EmissiveCol.Z = m.Emissive.Z; //Vec4 to Vec3
            _skinFx.DiffuseCol = m.Diffuse;
            if (useMaterialSpec)
            {
                _skinFx.SpecularCol.X = m.Specular.X;
                _skinFx.SpecularCol.Y = m.Specular.Y;
                _skinFx.SpecularCol.Z = m.Specular.Z;
                _skinFx.SpecularPow = m.ShineStrength; // I think...                   
            }
        }


        // We might use this one a lot - great for looping thru meshes in game and specifying unique parameters for each mesh based on what it is
        // Note: It would be possible to send in a transform in the above to add a temporary offset to a bone (like to aim the head in another direction) 
        /// <summary> Draws the mesh by the index. </summary>
        public void DrawMesh(int meshIndex, Camera cam, Matrix world, bool useMeshMaterials = true, bool useMaterialSpec = false)
        {
            var m = Meshes[meshIndex];
            _skinFx.Fx.Parameters["Bones"].SetValue(m.ShaderMatrices); // provide the bone matrices of this mesh
            if (useMeshMaterials) AssignMaterials(m, useMaterialSpec);
            _skinFx.World = world * m.NodeWithAnimTrans.CombinedMtx; // set model's world transform

            // DO LAST (this will apply the technique with any parameters set before it (or in it)): 
            _skinFx.SetDrawParams(cam, m.TexDiffuse, m.TexNormalMap, m.TexSpecular);
            _gpu.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, m.Vertices, 0, m.Vertices.Length, m.Indices, 0, m.Indices.Length / 3, VertexNormMapSkin.VertexDeclaration);
        }

        public class ModelBone
        {
            public string Name; // bone name
            public int MeshIndex = -1; // which mesh? 
            public int BoneIndex = -1; // which bone? 
            public int NumWeightedVerts = 0; // number of weighted verts that use this bone for influence

            public Matrix OffsetMtx; // bind-pose transforms
        } // Model Bone Class



        // a transform - some are bones(joints) - some not (part of tree). Each could link to more than 1 mesh and so have more than 1 offset.    
        public class ModelNode
        {
            public string Name; // mesh or bone name (whichever it is)
            public ModelNode Parent; // parent node       (usually parent bone) 
            public readonly List<ModelNode> Children = new(); // child tree

            public bool HasRealBone, IsBoneOnRoute, IsMeshNode; // used for debugging:       

            // Each mesh has a list of shader-matrices - this keeps track of which meshes these bones apply to (and the bone index)
            public readonly List<ModelBone> UniqueMeshBones = new(); // points to mesh & bone that corresponds to this node bone.

            public Matrix LocalMtx; // transform relative to parent         
            public Matrix CombinedMtx; // tree-accumulated transforms  (global-space transform for shader to use - skin matrix)
        }

        /// <summary> Models are composed of meshes each with their own textures and sets of vertices associated to them. </summary>
        public class SkinMesh
        {
            public ModelNode NodeWithAnimTrans; // reference of node containing animated transform
            public string Name = "";
            public int MeshNumber;
            public bool HasBones;
            public string TexName;
            public string TexNormMapName;
            public string TexHeightMapName;
            public string TexReflectionMapName;
            public string TexSpecularName;
            public Texture2D TexDiffuse;
            public Texture2D TexSpecular;
            public Texture2D TexNormalMap;
            public Texture2D TexHeightMap;

            public Texture2D TexReflectionMap;

            //public Texture2D tex_lightMap, tex_ambientOcclusion;     // maybe these 2 are better baked into tex_diffuse?            
            public VertexNormMapSkin[] Vertices;
            public int[] Indices;
            public ModelBone[] MeshBones;
            public Matrix[] ShaderMatrices;

            public int MaterialIndex;
            public string MaterialName; // (for this index)

            public Vector3 Min, Max, Mid;

            // MESH MATERIAL (add more as needed - like if you want pbr):
            public Vector4 Ambient = Vector4.One; // minimum light color
            public Vector4 Diffuse = Vector4.One; // regular material colorization
            public Vector4 Specular = Vector4.One; // specular highlight color 
            public Vector4 Emissive = Vector4.One; // amplify a color brightness (not requiring light - similar to ambient really - kind of a glow without light)                
            public float Opacity = 1.0f; // how opaque or see-through is it?          
            public float Reflectivity = 0.0f; // strength of reflections
            public float Shininess = 0.0f; // how much light shines off
            public float ShineStrength = 1.0f; // probably specular power (can use to narrow & intensifies highlights - ie: more wet or metallic looking)
            public float BumpScale = 0.0f; // amplify or reduce normal-map effect  

            public bool IsTwoSided = false; // useful for glass and ice
            //public Vector4 colorTransparent = Vector4.One;  
            //public Vector4 reflective = Vector4.One;
            //public float transparency = 0.0f;
            //public bool isPbrMaterial = false;
            //public string blendMode   = "Default";
            //public string shadingMode = "Default";
            //public bool hasShaders    = false;
            //public bool isWireFrameEnabled = false;
        }

        /// <summary> All the 'animNodes' are in RigAnimation & the nodes have lists of frames of animations.</summary>
        public class RigAnimation
        {
            public double DurationInSeconds; // same in seconds
            public double DurationInSecondsAdded; // added seconds
            public List<AnimNodes> AnimatedNodes; // holds the animated nodes

            ///<summary> animation blending between key-frames </summary>
            public Matrix Interpolate(double animTime, AnimNodes nodeAnim)
            {
                var durationSecs = DurationInSeconds + DurationInSecondsAdded;

                while (animTime > durationSecs) // If the requested play-time is past the end of the animation, loop it (ie: time = 20 but duration = 16 so time becomes 4)
                    animTime -= durationSecs;

                Quaternion q1 = nodeAnim.Qrot[0], q2 = q1; // init rot as entry 0 for both keys (init may be needed cuz conditional value assignment can upset compiler)
                Vector3 p1 = nodeAnim.Position[0], p2 = p1; // " pos
                Vector3 s1 = nodeAnim.Scale[0], s2 = s1; // " scale
                double tq1 = nodeAnim.QrotTime[0], tq2 = tq1; // " rot-time
                double tp1 = nodeAnim.PositionTime[0], tp2 = tp1; // " pos-time
                double ts1 = nodeAnim.ScaleTime[0], ts2 = ts1; // " scale-time

                // GET ROTATION KEYFRAMES
                var endTIndex = nodeAnim.QrotTime.Count - 1; // final time's index (starting with qrot cuz we do it first - we'll cahnge this variable for pos and scale)
                var endIndex = nodeAnim.Qrot.Count - 1; // final rot frame
                var endTime = nodeAnim.QrotTime[endTIndex]; // get final rotation-time
                if (animTime > endTime)
                {
                    // if animTime is past final rotation-time: Set to interpolate between last and first frames (for animation-loop)
                    tq1 = endTime; // key 1 time is last keyframe and time 2 is time taken after to get to frame 0 (see below) 
                    tq2 += durationSecs; // total duration accounting for time to loop from last frame to frame 0 (with DurationInSecondsAdded)
                    q1 = nodeAnim.Qrot[endIndex]; // get final quaternion (count-1),       NOTE: q2 already set above (key frame 0)                                                                      
                }
                else
                {
                    int frame2 = endIndex, frame1; //                  animTime   t =  frame2
                    for (; frame2 > -1; frame2--)
                    {
                        // loop from last index to 0 (until find correct place on timeline):
                        var t = nodeAnim.QrotTime[frame2]; // what's the time at this frame?
                        if (t < animTime) break; // if the current_time > the frame time then we've found the spot we're looking for (break out)                                                    
                    }

                    if (frame2 < endIndex) frame2++; // at this point the frame2 is 1 less than what we're looking for so add 1
                    q2 = nodeAnim.Qrot[frame2];
                    tq2 = nodeAnim.QrotTime[frame2];
                    frame1 = frame2 - 1;
                    if (frame1 < 0)
                    {
                        frame1 = endIndex; // loop frame1 to last frame
                        tq1 = nodeAnim.QrotTime[frame1] - durationSecs; // Using: frame2time - frame1time, so we need time1 to be less _ thus: subtract durationSecs to fix it
                    }
                    else
                        tq1 = nodeAnim.QrotTime[frame1]; // get time1 

                    q1 = nodeAnim.Qrot[frame1];
                }

                // GET POSITION KEY FRAMES
                endTIndex = nodeAnim.PositionTime.Count - 1; // final time's index
                endIndex = nodeAnim.Position.Count - 1; // final pos frame
                endTime = nodeAnim.PositionTime[endTIndex]; // get final position-time
                if (animTime > endTime)
                {
                    // if animTime is past final pos-time: Set to interpolate between last and first frames (for animation-loop)
                    tp1 = endTime; // key 1 time is last keyframe and time 2 is time taken after to get to frame 0 (see below) 
                    tp2 += durationSecs; // total duration accounting for time to loop from last frame to frame 0 (with DurationInSecondsAdded)
                    p1 = nodeAnim.Position[endIndex]; // get final position (count-1),       NOTE: q2 already set above (key frame 0)                                                                      
                }
                else
                {
                    int frame2 = endIndex, frame1;
                    for (; frame2 > -1; frame2--)
                    {
                        // loop from last index to 0 (until find correct place on timeline):
                        var t = nodeAnim.PositionTime[frame2]; // what's the time at this frame?
                        if (t < animTime) break; // if the current_time > the frame time then we've found the spot we're looking for (break out)                                                    
                    }

                    if (frame2 < endIndex) frame2++; // at this point the frame2 is 1 less than what we're looking for so add 1
                    p2 = nodeAnim.Position[frame2];
                    tp2 = nodeAnim.PositionTime[frame2];
                    frame1 = frame2 - 1;
                    if (frame1 < 0)
                    {
                        frame1 = endIndex; // loop frame1 to last frame
                        tp1 = nodeAnim.PositionTime[frame1] - durationSecs; // Using: frame2time - frame1time, so we need time1 to be less _ thus: subtract durationSecs to fix it
                    }
                    else
                        tp1 = nodeAnim.PositionTime[frame1]; // get time1 

                    p1 = nodeAnim.Position[frame1];
                }

                // GET SCALE KEYFRAMES 
                endTIndex = nodeAnim.ScaleTime.Count - 1; // final time's index
                endIndex = nodeAnim.Scale.Count - 1; // final scale frame
                endTime = nodeAnim.ScaleTime[endTIndex]; // get final scale-time
                if (animTime > endTime)
                {
                    // if animTime is past final scale-time: Set to interpolate between last and first frames (for animation-loop)
                    ts1 = endTime; // key 1 time is last keyframe and time 2 is time taken after to get to frame 0 (see below) 
                    ts2 += durationSecs; // total duration accounting for time to loop from last frame to frame 0 (with DurationInSecondsAdded)
                    s1 = nodeAnim.Scale[endIndex]; // get final scale (count-1),       NOTE: q2 already set above (key frame 0)                                                                      
                }
                else
                {
                    int frame2 = endIndex, frame1;
                    for (; frame2 > -1; frame2--)
                    {
                        // loop from last index to 0 (until find correct place on timeline):
                        var t = nodeAnim.ScaleTime[frame2]; // what's the time at this frame?
                        if (animTime > t) break; // if the current_time > the frame time then we've found the spot we're looking for (break out)                                                    
                    }

                    if (frame2 < endIndex) frame2++; // at this point the frame2 is 1 less than what we're looking for so add 1
                    s2 = nodeAnim.Scale[frame2];
                    ts2 = nodeAnim.ScaleTime[frame2];
                    frame1 = frame2 - 1;
                    if (frame1 < 0)
                    {
                        frame1 = endIndex; // loop frame1 to last frame
                        ts1 = nodeAnim.ScaleTime[frame1] - durationSecs; // Using: frame2time - frame1time, so we need time1 to be less _ thus: subtract durationSecs to fix it
                    }
                    else
                        ts1 = nodeAnim.ScaleTime[frame1]; // get time1 

                    s1 = nodeAnim.Scale[frame1];
                }

                var tqi = (float)GetInterpolateTimePercent(tq1, tq2, animTime); // get the time% (0-1)
                var q = Quaternion.Slerp(q1, q2, tqi); // blend the rotation between keys using the time percent

                var tpi = (float)GetInterpolateTimePercent(tp1, tp2, animTime); // "
                var p = Vector3.Lerp(p1, p2, tpi);

                var tsi = (float)GetInterpolateTimePercent(ts1, ts2, animTime); // "
                var s = Vector3.Lerp(s1, s2, tsi);

                var ms = Matrix.CreateScale(s);
                var mr = Matrix.CreateFromQuaternion(q);
                var mt = Matrix.CreateTranslation(p);

                var m = ms * mr * mt; // S,R,T
                return m;
            }

            private static double GetInterpolateTimePercent(double s, double e, double val)
            {
                if (val < s || val > e) throw new Exception("SkinModel.cs RigAnimation GetInterpolateTimePercent :  Value " + val + " passed to the method must be within the start and end time. ");
                if (s == e) throw new Exception("SkinModel.cs RigAnimation GetInterpolateTimePercent :  e - s :  " + e + "-" + s + "=0  - Divide by zero error.");
                return (val - s) / (e - s);
            }
        }

        /// <summary> Nodes contain animation frames. Initial trans are copied from assimp - then interpolated frame sets are built. (keeping original S,R,T if want to later edit) </summary>
        public class AnimNodes
        {
            public ModelNode NodeRef;
            public string NodeName = "";

            // in model tick time
            public readonly List<double> PositionTime = new();
            public readonly List<double> ScaleTime = new();
            public readonly List<double> QrotTime = new();
            public readonly List<Vector3> Position = new();
            public readonly List<Vector3> Scale = new();
            public readonly List<Quaternion> Qrot = new();
        } 
    } 

    public struct VertexNormMapSkin : IVertexType
    {
        public Vector3 Pos, Norm;
#if USING_COLORED_VERTICES
        public Vector4 color;
#endif
        public Vector2 Uv;
        public Vector3 Tangent;
        public Vector3 BiTangent;
        public Vector4 BlendIndices, BlendWeights;

        public static VertexDeclaration VertexDeclaration = new(
            new VertexElement(BYT.Ini(3), VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
#if USING_COLORED_VERTICES
              new VertexElement(BYT.Ini(4), VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
#endif
            new VertexElement(BYT.Off(3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(BYT.Off(2), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(BYT.Off(3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
            new VertexElement(BYT.Off(3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
            new VertexElement(BYT.Off(4), VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0),
            new VertexElement(BYT.Off(4), VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }
    }

    // B O F F (adjusts byte offset for each entry in a vertex declaration)
    public struct BYT
    {
        public static int byt;

        public static int Ini(int bSize)
        {
            bSize *= 4;
            byt = 0;
            byt += bSize;
            return 0;
        }

        public static int Off(int bSize)
        {
            bSize *= 4;
            byt += bSize;
            return byt - bSize;
        }
    }
}