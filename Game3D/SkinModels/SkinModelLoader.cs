using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assimp;
using Assimp.Configs;
using Game3D.SkinModels.SkinModelHelpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PrimitiveType = Assimp.PrimitiveType;

// ASSIMP INSTRUCTIONS:
// AssimpNET is (cross platform) .NET wrapper for Open Asset Import Library 
// Add the AssimpNET nuget to your solution:
// - in the solution explorer, right click on the project
// - select manage nuget packages
// - click browse
// - type in assimpNET and install it to the solution and project via the checkbox on the right.

// THIS IS BASED ON WORK BY:  WIL MOTIL  (a slightly older modified version)
// https://github.com/willmotil/MonoGameUtilityClasses

namespace Game3D.SkinModels;

// C L A S S   L O A D E R   ( for SkinModel )
// Uses assimpNET 4.1+ nuget,  to load a rigged and or animated model
internal class SkinModelLoader
{
    private readonly GraphicsDevice _graphicsDevice;
    private Scene _scene;
    private string _filePathName;
    private static ContentManager _content;

    // LoadingLevelPreset: 
    // 0 = TargetRealTimeMaximumQuality, 1 = TargetRealTimeQuality, 2 = TargetRealTimeFast, 3 = custom (does it's best to squash meshes down - good for some older models)
    // ReverseVertexWinding:
    // Reverses the models winding - typically this will change the model vertices to counter clockwise winding (CCW).
    // AddLoopingDuration:
    // Artificially adds a small amount of looping duration to the end of a animation.This helps to fix animations that aren't properly looped.
    // Turn on AddAdditionalLoopingTime to use this.
    // Configuration stuff: 
    // https://github.com/assimp/assimp-net/blob/master/AssimpNet/Configs/PropertyConfig.cs
    private static int _loadingLevelPreset = 3;

    private float _addedLoopingDuration;

    private readonly List<PropertyConfig> _configurations = new()
    {
        new NoSkeletonMeshesConfig(true), // true to disable dummy-skeleton mesh
        new FBXImportCamerasConfig(false), // true would import cameras
        new SortByPrimitiveTypeConfig(PrimitiveType.Point | PrimitiveType.Line), // primitive types we should remove
        new VertexBoneWeightLimitConfig(4), // max weights per vertex (4 is very common - our shader will use 4)
        new NormalSmoothingAngleConfig(66.0f), // if no normals, generate (threshold 66 degrees) 
        new FBXStrictModeConfig(false), // true only for fbx-strict-mode
    };

    public SkinModelLoader(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;
        _graphicsDevice = graphicsDevice;
    }

    public void SetDefaultOptions(float addToLoopDuration, string setADebugTexture)
    {
        _addedLoopingDuration = addToLoopDuration;
        _content.Load<Texture2D>(setADebugTexture);
    }

    public SkinModel Load(string filePathOrFileName, int loadingLevelPreset, SkinFx skinFx, float rescale = 1f)
    {
        _loadingLevelPreset = loadingLevelPreset;
        return Load(filePathOrFileName, skinFx, rescale);
    }

    private static string FindFile(string filePathOrFileName)
    {
        Path.GetFileNameWithoutExtension(filePathOrFileName);
        var s = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Content"), filePathOrFileName); // rem: set FBX to "copy" and remove any file processing properties
        if (File.Exists(s) == false)
        {
            Console.WriteLine("(not found) Checked for: " + s);
            s = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Assets"), filePathOrFileName); // If file is placed in a project assets folder (with copy-to property set)            
        }

        if (File.Exists(s) == false)
        {
            Console.WriteLine("(not found) Instead tried to find: " + s);
            s = Path.Combine(Environment.CurrentDirectory, filePathOrFileName); // maybe in the exe directory
        }

        if (File.Exists(s) == false)
        {
            Console.WriteLine("(not found) Instead tried to find: " + s);
            s = filePathOrFileName; // maybe the exact complete path is specified
        }

        Console.WriteLine("Final file/path checked for: " + s);

        Debug.Assert(File.Exists(s), "Could not find the file to load: " + s);
        return s;
    }

    private SkinModel Load(string filePathOrFileName, SkinFx skinFx, float rescale = 1f)
    {
        _filePathName = filePathOrFileName;
        var fullFilePath = FindFile(filePathOrFileName);

        // SETUP ASSIMP IMPORTER AND CONFIGURATIONS
        var importer = new AssimpContext();
        foreach (var p in _configurations) 
            importer.SetConfig(p);
        
        importer.Scale = rescale;

        // LOAD FILE INTO "SCENE" (an assimp imported model is in a thing called Scene)
        try
        {
            switch (_loadingLevelPreset)
            {
                case 0:
                    _scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeMaximumQuality);
                    break;
                case 1:
                    _scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeQuality);
                    break;
                case 2:
                    _scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeFast);
                    break;
                default:
                    _scene = importer.ImportFile(fullFilePath,
                        PostProcessSteps.FlipUVs // currently need
                        | PostProcessSteps.JoinIdenticalVertices // optimizes indexed
                        | PostProcessSteps.Triangulate // precaution
                        | PostProcessSteps.FindInvalidData // sometimes normals export wrong (remove & replace:)
                        | PostProcessSteps.GenerateSmoothNormals // smooths normals after identical verts removed (or bad normals)
                        | PostProcessSteps.ImproveCacheLocality // possible better cache optimization                                        
                        | PostProcessSteps.FixInFacingNormals // doesn't work well with planes - turn off if some faces go dark                                       
                        | PostProcessSteps.CalculateTangentSpace // use if you'll probably be using normal mapping 
                        | PostProcessSteps.GenerateUVCoords // useful for non-uv-map export primitives                                                
                        | PostProcessSteps.ValidateDataStructure
                        | PostProcessSteps.FindInstances
                        | PostProcessSteps.GlobalScale // use with AI_CONFIG_GLOBAL_SCALE_FACTOR_KEY (if need)                                                
                        | PostProcessSteps.FlipWindingOrder // (CCW to CW) Depends on your rasterizing setup (need clockwise to fix inside-out problem?)                                                 

                        //| PostProcessSteps.RemoveRedundantMaterials // use if not using material names to ID special properties                                                
                        //| PostProcessSteps.FindDegenerates      // maybe (if using with AI_CONFIG_PP_SBP_REMOVE to remove points/lines)
                        //| PostProcessSteps.SortByPrimitiveType  // maybe not needed (sort points, lines, etc)
                        //| PostProcessSteps.OptimizeMeshes       // not suggested for animated stuff
                        //| PostProcessSteps.OptimizeGraph        // not suggested for animated stuff                                        
                        //| PostProcessSteps.TransformUVCoords    // maybe useful for keyed uv transforms                                                
                    );
                    break;
            }
        }
        catch (AssimpException ex)
        {
            throw new Exception("Problem reading file: " + fullFilePath + " (" + ex.Message + ")", ex);
        }

        return CreateModel(fullFilePath, skinFx);
    }

    private SkinModel CreateModel(string fileNom, SkinFx skinFx)
    {
        var model = new SkinModel(_graphicsDevice, skinFx); // create model
        model.RelativeFilename = _filePathName;
        CreateRootNode(model, _scene); // prep to build model's tree (need root node)
        CreateMeshesAndBones(model, _scene); // create the model's meshes and bones
        SetupMaterialsAndTextures(model, _scene); // setup the materials and textures of each mesh

        // recursively search and add the nodes for our model from "scene", this includes adding to the flat bone & node lists
        CreateTreeTransforms(model, model.RootNodeOfTree, _scene.RootNode);
        PrepareAnimationsData(model, _scene); // get the animations in the file into each node's animations frame list           
        CopyVertexIndexData(model, _scene); // get the vertices from the meshes  
        return model;
    }

    /// <summary> Create a root node </summary>
    private static void CreateRootNode(SkinModel model, Scene scene)
    {
        model.RootNodeOfTree = new SkinModel.ModelNode();
        model.RootNodeOfTree.Name = scene.RootNode.Name; // set the root node
        // set the root node transforms
        model.RootNodeOfTree.LocalMtx = scene.RootNode.Transform.ToMgTransposed(); // ToMg converts to monogame compatible version
        model.RootNodeOfTree.CombinedMtx = model.RootNodeOfTree.LocalMtx;
    }

    /// <summary> We create model mesh instances for each mesh in scene.meshes. This is just set up here - it doesn't load any data. </summary>
    private static void CreateMeshesAndBones(SkinModel model, Scene scene)
    {
        model.Meshes = new SkinModel.SkinMesh[scene.Meshes.Count];

        // create the meshes (from "scene")
        for (var meshIndex = 0; meshIndex < scene.Meshes.Count; meshIndex++)
        {
            var assimpMesh = scene.Meshes[meshIndex];
            var sMesh = new SkinModel.SkinMesh();
            sMesh.Name = assimpMesh.Name;
            sMesh.MeshNumber = meshIndex;                
            sMesh.TexName = "Default";
            sMesh.MaterialIndex = assimpMesh.MaterialIndex;
            sMesh.MaterialName = scene.Materials[sMesh.MaterialIndex].Name;

            var assimpMeshBones = assimpMesh.Bones;
            sMesh.HasBones = assimpMesh.HasBones; 
            sMesh.ShaderMatrices = new Matrix[assimpMeshBones.Count + 1];
            sMesh.ShaderMatrices[0] = Matrix.Identity; // default=identity; if no bone/node animation, this will keep it static for the duration
            sMesh.MeshBones = new SkinModel.ModelBone[assimpMesh.BoneCount + 1];

            // DUMMY BONE: Can't yet link this to the node - that must wait - it's not yet created & only exists in the model (not in the assimp bone list)
            sMesh.MeshBones[0] = new SkinModel.ModelBone(); // make dummy ModelBone                
            var flatBone = sMesh.MeshBones[0];
            flatBone.OffsetMtx = Matrix.Identity;
            flatBone.Name = assimpMesh.Name; // "DummyBone0";
            flatBone.MeshIndex = meshIndex; // index of the mesh this bone belongs to
            flatBone.BoneIndex = 0; // (note that since we're making a dummy bone at index 0, we'll add 1 to the others)                

            // CREATE/ADD BONES (from assimp data):
            for (var abi = 0; abi < assimpMeshBones.Count; abi++)
            {
                var assimpBone = assimpMeshBones[abi];                    
                var boneIndex = abi + 1; // add 1 because we made a dummy bone
                sMesh.ShaderMatrices[boneIndex] = Matrix.Identity; // init shader matrices
                sMesh.MeshBones[boneIndex] = new SkinModel.ModelBone();
                flatBone = sMesh.MeshBones[boneIndex];
                flatBone.Name = assimpBone.Name;
                flatBone.OffsetMtx = assimpBone.OffsetMatrix.ToMgTransposed();
                flatBone.MeshIndex = meshIndex;
                flatBone.BoneIndex = boneIndex;
                flatBone.NumWeightedVerts = assimpBone.VertexWeightCount; 
                sMesh.MeshBones[boneIndex] = flatBone;
            }

            model.Meshes[meshIndex] = sMesh;
        }
    }

    /// <summary> Loads textures and sets material values to each model mesh. </summary>
    private static void SetupMaterialsAndTextures(SkinModel model, Scene scene)
    {
        for (var i = 0; i < scene.Meshes.Count; i++)
        {
            var sMesh = model.Meshes[i];
            var materialIndex = sMesh.MaterialIndex;
            var assimpMaterial = scene.Materials[materialIndex]; 

            sMesh.Ambient = assimpMaterial.ColorAmbient.ToMonoGame();
            sMesh.Diffuse = assimpMaterial.ColorDiffuse.ToMonoGame();
            sMesh.Specular = assimpMaterial.ColorSpecular.ToMonoGame(); 
            sMesh.Emissive = assimpMaterial.ColorEmissive.ToMonoGame();                 
            sMesh.Opacity = assimpMaterial.Opacity; 
            sMesh.Reflectivity = assimpMaterial.Reflectivity;
            sMesh.Shininess = assimpMaterial.Shininess;
            sMesh.ShineStrength = assimpMaterial.ShininessStrength;
            sMesh.BumpScale = assimpMaterial.BumpScaling;
            sMesh.IsTwoSided = assimpMaterial.IsTwoSided;

            //sMesh.colorTransparent   = assimpMaterial.ColorTransparent.ToMg();
            //sMesh.reflective         = assimpMaterial.ColorReflective.ToMg(); 
            //sMesh.transparency       = assimpMaterial.TransparencyFactor;
            //sMesh.hasShaders         = assimpMaterial.HasShaders;
            //sMesh.shadingMode        = assimpMaterial.ShadingMode.ToString();
            //sMesh.blendMode          = assimpMaterial.BlendMode.ToString();
            //sMesh.isPbrMaterial      = assimpMaterial.IsPBRMaterial;
            //sMesh.isWireFrameEnabled = assimpMaterial.IsWireFrameEnabled;

            var assimpMaterialTextures = assimpMaterial.GetAllMaterialTextures();

            for (var t = 0; t < assimpMaterialTextures.Length; t++)
            {
                //var textureIndex = assimpMaterialTextures[t].TextureIndex;
                //var textureOperation = assimpMaterialTextures[t].Operation;
                //var textureType = assimpMaterialTextures[t].TextureType;
                var relativeFilename = assimpMaterialTextures[t].FilePath;
                var xnbFileName = GetXnbFileName(relativeFilename);                    

                // assumes the model is in its specific directory, under "Contents" AND its textures are below it
                var modelDirectory = Path.GetDirectoryName(model.RelativeFilename);
                if (modelDirectory != null)
                    xnbFileName = Path.Combine(modelDirectory, xnbFileName);

                var xnbFullFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _content.RootDirectory, xnbFileName);

                if (!File.Exists(xnbFullFilePath))
                    throw new FileNotFoundException(null, xnbFullFilePath);

                //xnbFileName = GetFilenameWithoutExtension(xnbFileName);
                //LoadTextures(textureType, model, mi, xnbFileName);
            }
        }
    }

    /// <summary> Recursively get scene nodes stuff into our model nodes
    /// - get node name, init local matrix, assign non-bone mesh transforms to corresponding mesh
    /// - match up by name: meshes-index/bone-index with this bone (which meshes and mesh-bone-list-index(for unique mesh-relative offsets) this bone deals with)
    /// - create the children /// </summary>
    private void CreateTreeTransforms(SkinModel model, SkinModel.ModelNode modelNode, Node curAssimpNode)
    {
        modelNode.Name = curAssimpNode.Name; // get node name
        modelNode.LocalMtx = curAssimpNode.Transform.ToMgTransposed(); // set initial local transform
        modelNode.CombinedMtx = curAssimpNode.Transform.ToMgTransposed();

        // IF IS A MESH NODE: (instead of bone node)
        if (curAssimpNode.HasMeshes)
        {
            modelNode.IsMeshNode = true;
            foreach (var sMesh in curAssimpNode.MeshIndices.Select(meshIndex => model.Meshes[meshIndex]))
            {
                sMesh.NodeWithAnimTrans = modelNode; // tell the mesh to get it's transform from the current node in the tree
            }
        }

        // For each assimpNode in the tree, we create a uniqueMeshBones list which holds information about which meshes are affected (and thus need index of bone for each mesh)
        // We can find the bone to use within each mesh's bone-list by finding a matching name. We store the applicable mesh#'s and bone#'s 
        // to be able to affect more than 1 mesh with a bone later when recursing the tree for animation updates. 
        for (var mi = 0; mi < _scene.Meshes.Count; mi++)
        {
            if (!TryGetBoneForMesh(model.Meshes[mi], modelNode.Name, out var bone, out var boneIndexInMesh)) 
                continue;
            // find the bone that goes with this node name
            // MARK AS BONE: 
            modelNode.HasRealBone = true; // yes, we found it in this mesh's bone-list
            modelNode.IsBoneOnRoute = true; // "                    
            bone.MeshIndex = mi; // record index of every mesh the bone should affect
            bone.BoneIndex = boneIndexInMesh; // record index of the bone within each affected mesh's bone-list
            modelNode.UniqueMeshBones.Add(bone); // add the bone into our flat-list of bones (could be only 1 mesh, could be multiple)
        }

        // CHILDREN: 
        for (var i = 0; i < curAssimpNode.Children.Count; i++)
        {
            var child = curAssimpNode.Children[i];
            var childNode = new SkinModel.ModelNode(); // made each child node                
            childNode.Parent = modelNode; // set parent before passing
            childNode.Name = curAssimpNode.Children[i].Name; // name the child
            if (childNode.Parent.IsBoneOnRoute) childNode.IsBoneOnRoute = true; // part of actual tree
            modelNode.Children.Add(childNode); // add each child to this node's child list
            CreateTreeTransforms(model, modelNode.Children[i], child); // recursively create transforms for each child
        }
    }

    /// <summary> Gets the assimp animations into our model </summary>
    // http://sir-kimmi.de/assimp/lib_html/_animation_overview.html
    // http://sir-kimmi.de/assimp/lib_html/structai_animation.html
    // http://sir-kimmi.de/assimp/lib_html/structai_anim_mesh.html            

    private void PrepareAnimationsData(SkinModel model, Scene scene)
    {
        // Copy animation to ours
        for (var i = 0; i < scene.Animations.Count; i++)
        {
            var originalAnimation = scene.Animations[i];

            // Initially, copy data
            var modelAnimation = new SkinModel.RigAnimation();
            modelAnimation.DurationInSeconds = originalAnimation.DurationInTicks / originalAnimation.TicksPerSecond;
            modelAnimation.DurationInSecondsAdded = _addedLoopingDuration; // May have added duration for animation-loop-fix                

            // Need an animation-node-list for each animation 
            modelAnimation.AnimatedNodes = new List<SkinModel.AnimNodes>(); // lists of S,R,T keyframes for nodes
            
            for (var j = 0; j < originalAnimation.NodeAnimationChannels.Count; j++)
            {
                var assimpNodeAnimationLists = originalAnimation.NodeAnimationChannels[j]; // keyframes for this channel
                var nodeAnim = new SkinModel.AnimNodes();
                nodeAnim.NodeName = assimpNodeAnimationLists.NodeName;

                var refToNode = GetRefToNode(assimpNodeAnimationLists.NodeName, model.RootNodeOfTree);
                if (refToNode == null) Console.WriteLine("NODE SHOULD NOT BE NULL: " + assimpNodeAnimationLists.NodeName);
                nodeAnim.NodeRef = refToNode;

                // get the rotation, scale, and position keys: 
                foreach (var keyList in assimpNodeAnimationLists.RotationKeys)
                {
                    var quaternion = keyList.Value;
                    nodeAnim.QuaternionRotationTime.Add(keyList.Time / originalAnimation.TicksPerSecond);
                    nodeAnim.Quaternions.Add(quaternion.ToMonoGame());
                }

                foreach (var keyList in assimpNodeAnimationLists.PositionKeys)
                {
                    var position = keyList.Value.ToMonoGame();
                    nodeAnim.PositionTime.Add(keyList.Time / originalAnimation.TicksPerSecond); 
                    nodeAnim.Position.Add(position); 
                }

                foreach (var keyList in assimpNodeAnimationLists.ScalingKeys)
                {
                    var scale = keyList.Value.ToMonoGame(); 
                    nodeAnim.ScaleTime.Add(keyList.Time / originalAnimation.TicksPerSecond); 
                    nodeAnim.Scale.Add(scale); 
                }

                // Place this populated node into this model animation:
                modelAnimation.AnimatedNodes.Add(nodeAnim);
            }

            model.Animations.Add(modelAnimation);
        } 
    }

    /// <summary> Copy data from scene to our meshes. </summary> // http://sir-kimmi.de/assimp/lib_html/structai_mesh.html#aa2807c7ba172115203ed16047ad65f9e
    private void CopyVertexIndexData(SkinModel model, Scene scene)
    {
        for (var i = 0; i < scene.Meshes.Count; i++)
        {
            var mesh = scene.Meshes[i];
            var indices = GetIndices(mesh);
            var vertices = GetVertices(mesh);
            UpdateNormals(mesh, ref vertices);
            UpdateUvs(mesh, ref vertices);
            var (tan1, tan2) = RegenerateTangents(indices, vertices);
            OrthoNormalize(ref vertices, ref tan1, ref tan2);

            var (min, max, centroid) = GetBoundingBox(vertices);
            model.Meshes[i].Mid = centroid / vertices.Length;
            model.Meshes[i].Min = min;
            model.Meshes[i].Max = max;

            // BLEND WEIGHTS AND BLEND INDICES
            for (var k = 0; k < mesh.Vertices.Count; k++)
            {
                vertices[k].BlendIndices = Vector4.Zero;
                vertices[k].BlendWeights = Vector4.Zero;
            }

            // Restructure vertex data to conform to a shader.
            // Iterate mesh bone offsets - set the bone Id's and weights to the vertices.
            // This also entails correlating the mesh local bone index names to the flat bone list.
            var tempVerts = mesh.HasBones 
                ? RestructureVertexMeshWithBonesToConformToShader(model, mesh, i) 
                : RestructureVertexMeshWithoutBonesToConformToShader(mesh);

            AdjustBoneInfluence(tempVerts, vertices);

            model.Meshes[i].Vertices = vertices;
            model.Meshes[i].Indices = indices;
        }
    }

    private void AdjustBoneInfluence(TempVertWeightIndices[] tempVerts, VertexNormMapSkin[] vertices)
    {
        // Need up to 4 bone-influences per vertex - empties are 0, weight 0.
        // We can add non-zero entries (0,1,2,3) based on how many were stored.
        // (Note: The bone weight data aligns with offset matrices bone names)                
        for (var i = 0; i < tempVerts.Length; i++)
        {
            if (tempVerts[i] == null) 
                continue;

            var ve = tempVerts[i]; // get vertex entry
            //NOTE: maxbones = 4 
            var arrayIndex = ve.VertIndices.ToArray();
            var arrayBoneId = ve.VertFlatBoneId.ToArray();
            var arrayWeight = ve.VertBoneWeights.ToArray();
            if (arrayBoneId.Length > 3)
            {
                vertices[arrayIndex[3]].BlendIndices.W = arrayBoneId[3]; // we can copy entry 4
                vertices[arrayIndex[3]].BlendWeights.W = arrayWeight[3];
            }

            if (arrayBoneId.Length > 2)
            {
                vertices[arrayIndex[2]].BlendIndices.Z = arrayBoneId[2]; // " entry 3
                vertices[arrayIndex[2]].BlendWeights.Z = arrayWeight[2];
            }

            if (arrayBoneId.Length > 1)
            {
                vertices[arrayIndex[1]].BlendIndices.Y = arrayBoneId[1]; // " entry 2
                vertices[arrayIndex[1]].BlendWeights.Y = arrayWeight[1];
            }

            if (arrayBoneId.Length > 0)
            {
                vertices[arrayIndex[0]].BlendIndices.X = arrayBoneId[0]; // " entry 1
                vertices[arrayIndex[0]].BlendWeights.X = arrayWeight[0];
            }
        }
    }

    private static TempVertWeightIndices[] RestructureVertexMeshWithBonesToConformToShader(SkinModel model, Mesh mesh, int meshIndex)
    {
        var verts = new TempVertWeightIndices[mesh.Vertices.Count];
        model.Meshes[meshIndex].HasBones = mesh.HasBones;
        var bones = mesh.Bones;

        for (var i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            var boneName = bones[i].Name;
            Console.WriteLine(boneName);
            var modelBoneIndex = i + 1; // add 1 cuz we inserted a dummy at 0

            // loop thru this bones vertex listings with the weights for it:
            for (var weightIndex = 0; weightIndex < bone.VertexWeightCount; weightIndex++)
            {
                var vertInd = bone.VertexWeights[weightIndex].VertexID; // which vertex the bone-weight should be assigned to
                var weight = bone.VertexWeights[weightIndex].Weight; // get the weight
                verts[vertInd] ??= new TempVertWeightIndices();
                verts[vertInd].VertIndices.Add(vertInd); // store vert index
                verts[vertInd].VertFlatBoneId.Add(modelBoneIndex); // store current index of bone
                verts[vertInd].VertBoneWeights.Add(weight); // store weight
            }
        }

        return verts;
    }

    private static TempVertWeightIndices[] RestructureVertexMeshWithoutBonesToConformToShader(Mesh mesh)
    {
        var verts = new TempVertWeightIndices[mesh.Vertices.Count];

        // If there is no bone data we will set it to bone zero. (basically a precaution - no bone data, no bones)
        // In this case, verts need to have a weight and be set to bone 0 (identity).
        // (allows us to draw a boneless mesh [as if entire mesh were attached to a single identity world bone])
        // If there is an actual world mesh node, we can combine the animated transform and set it to that bone as well.
        // So this will work for boneless node mesh transforms which assimp doesn't mark as a actual mesh transform when it is.
        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = new TempVertWeightIndices();
            var ve = verts[i];
            if (ve.VertIndices.Count != 0) continue;

            // there is no bone data for this vertex, then we should set it to bone zero.
            verts[i].VertIndices.Add(i);
            verts[i].VertFlatBoneId.Add(0);
            verts[i].VertBoneWeights.Add(1.0f);
        }

        return verts;
    }

    /// <summary>
    /// Loop through all verts and do a Gram-Schmidt orthonormalize, then make sure they're all unit length.
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="tan1"></param>
    /// <param name="tan2"></param>
    private static void OrthoNormalize(ref VertexNormMapSkin[] vertices, ref Vector3[] tan1 , ref Vector3[] tan2)
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var n = vertices[i].Norm;
            Debug.Assert(n.IsFinite(), "Bad normal!");
            Debug.Assert(n.Length() >= 0.9999f, "Bad normal!");

            var t = tan1[i];
            if (t.LengthSquared() < float.Epsilon)
            {
                // TODO: Ideally we could spit out a warning to the content logging here!

                // We couldn't find a good tangent for this vertex.                        
                // Rather than set them to zero which could produce errors in other parts of 
                // the pipeline, we just take a guess at something that may look ok.

                t = Vector3.Cross(n, Vector3.UnitX);
                if (t.LengthSquared() < float.Epsilon) t = Vector3.Cross(n, Vector3.UnitY);

                vertices[i].Tangent = Vector3.Normalize(t);
                vertices[i].BiTangent = Vector3.Cross(n, vertices[i].Tangent);
                continue;
            }

            // Gram-Schmidt orthogonalize
            // TODO: This could be zero - could cause NaNs on normalize... how to fix this?
            var tangent = t - n * Vector3.Dot(n, t);
            tangent = Vector3.Normalize(tangent);
            Debug.Assert(tangent.IsFinite(), "Bad tangent!");
            vertices[i].Tangent = tangent;

            // Calculate handedness
            var w = Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0F ? -1.0F : 1.0F;
            Debug.Assert(LoaderExtensions.IsFinite(w), "Bad handedness!");

            // Calculate the bi-tangent
            var biTangent = Vector3.Cross(n, tangent) * w;
            Debug.Assert(biTangent.IsFinite(), "Bad bi-tangent!");
            vertices[i].BiTangent = biTangent;
        }        
    }

    private static (Vector3[], Vector3[]) RegenerateTangents(int[] indices, VertexNormMapSkin[] vertices)
    {
        var tan1 = new Vector3[vertices.Length];
        var tan2 = new Vector3[vertices.Length];

        for (var index = 0; index < indices.Length; index += 3)
        {
            var i1 = indices[index + 0];
            var i2 = indices[index + 1];
            var i3 = indices[index + 2];

            var w1 = vertices[i1].Uv;
            var w2 = vertices[i2].Uv;
            var w3 = vertices[i3].Uv;

            var s1 = w2.X - w1.X;
            var s2 = w3.X - w1.X;
            var t1 = w2.Y - w1.Y;
            var t2 = w3.Y - w1.Y;

            var denom = s1 * t2 - s2 * t1;
            if (Math.Abs(denom) < float.Epsilon)
            {
                // The triangle UVs are zero sized one dimension. So we cannot calculate the 
                // vertex tangents for this one triangle, but maybe it can with other triangles.
                continue;
            }

            var r = 1.0f / denom;
            Debug.Assert(LoaderExtensions.IsFinite(r), "Bad r!");

            var v1 = vertices[i1].Pos;
            var v2 = vertices[i2].Pos;
            var v3 = vertices[i3].Pos;

            var x1 = v2.X - v1.X;
            var x2 = v3.X - v1.X;
            var y1 = v2.Y - v1.Y;
            var y2 = v3.Y - v1.Y;
            var z1 = v2.Z - v1.Z;
            var z2 = v3.Z - v1.Z;

            var sdir = new Vector3
            {
                X = (t2 * x1 - t1 * x2) * r,
                Y = (t2 * y1 - t1 * y2) * r,
                Z = (t2 * z1 - t1 * z2) * r
            };

            var tdir = new Vector3
            {
                X = (s1 * x2 - s2 * x1) * r,
                Y = (s1 * y2 - s2 * y1) * r,
                Z = (s1 * z2 - s2 * z1) * r
            };

            tan1[i1] += sdir;
            Debug.Assert(tan1[i1].IsFinite(), "Bad tan1[i1]!");
            tan1[i2] += sdir;
            Debug.Assert(tan1[i2].IsFinite(), "Bad tan1[i2]!");
            tan1[i3] += sdir;
            Debug.Assert(tan1[i3].IsFinite(), "Bad tan1[i3]!");

            tan2[i1] += tdir;
            Debug.Assert(tan2[i1].IsFinite(), "Bad tan2[i1]!");
            tan2[i2] += tdir;
            Debug.Assert(tan2[i2].IsFinite(), "Bad tan2[i2]!");
            tan2[i3] += tdir;
            Debug.Assert(tan2[i3].IsFinite(), "Bad tan2[i3]!");
        }

        return (tan1, tan2);
    }

    private (Vector3, Vector3, Vector3) GetBoundingBox(VertexNormMapSkin[] vertices)
    {
        var min = Vector3.Zero;
        var max = Vector3.Zero;
        var centroid = Vector3.Zero;
        foreach (var vertex in vertices)
        {
            min.X = MathF.Min(vertex.Pos.X, min.X);
            min.Y = MathF.Min(vertex.Pos.Y, min.Y);
            min.Z = MathF.Min(vertex.Pos.Z, min.Z);
            
            max.X = MathF.Max(vertex.Pos.X, max.X);
            max.Y = MathF.Max(vertex.Pos.Y, max.Y);
            max.Z = MathF.Max(vertex.Pos.Z, max.Z);
            
            centroid += vertex.Pos;
        }

        return (min, max, centroid);
    }

    private static int[] GetIndices(Mesh mesh)
    {
        var indices = new int[mesh.Faces.Count * 3]; // need 3 indices per face
        var numIndices = 0;
        for (var k = 0; k < mesh.Faces.Count; k++)
        {
            // loop faces
            var f = mesh.Faces[k]; // get face
            var indCount = f.IndexCount;
            if (indCount != 3) Console.WriteLine("\n UNEXPECTED INDEX COUNT \n"); // may need to ensure load settings force triangulation
            for (var j = 0; j < indCount; j++)
            {
                // loop indices of face
                var ind = f.Indices[j]; // get each index
                indices[numIndices] = ind; // store each index into big array of indices
                numIndices++; // increment total number of indices
            }
        }

        return indices;
    }

    private static VertexNormMapSkin[] GetVertices(Mesh mesh)
    {
        var numVerts = mesh.Vertices.Count;
        var vertexNormMapSkins = new VertexNormMapSkin[numVerts]; // allocate memory for vertex array
        for (var k = 0; k < numVerts; k++)
        {
            // loop vertices in mesh
            var f = mesh.Vertices[k]; // get vertex
            vertexNormMapSkins[k].Pos = new Vector3(f.X, f.Y, f.Z); // copy vertex position
        }

        return vertexNormMapSkins;
    }

    private static void UpdateNormals(Mesh mesh, ref VertexNormMapSkin[] vertices)
    {
        for (var k = 0; k < mesh.Normals.Count; k++)
        {
            // loop normals
            var f = mesh.Normals[k]; // get normal    
            vertices[k].Norm = new Vector3(f.X, f.Y, f.Z); // copy normal
        }        
    }

    private static void UpdateUvs(Mesh mesh, ref VertexNormMapSkin[] vertices)
    {
        var uvChannels = mesh.TextureCoordinateChannels;
        for (var k = 0; k < uvChannels.Length; k++)
        {
            // Loop UV channels
            var vertexUvCoords = uvChannels[k];
            var uvCount = 0;
            for (var j = 0; j < vertexUvCoords.Count; j++)
            {
                // Loop texture coords
                var uv = vertexUvCoords[j];
                vertices[uvCount].Uv = new Vector2(uv.X, uv.Y); // get the vertex's uv coordinate
                uvCount++;
            }
        }        
    }

    private static string GetXnbFileName(string s)
    {
        return GetFilenameWithoutExtension(s) + ".xnb";
    }

    private static string GetFilenameWithoutExtension(string filename)
    {
        var extension = Path.GetExtension(filename);
        return filename[..^extension.Length];        
    }

    /// <summary> Gets the named bone in the model mesh </summary>
    private static bool TryGetBoneForMesh(SkinModel.SkinMesh sMesh, string name, out SkinModel.ModelBone bone, out int boneIndexInMesh)
    {
        for (var j = 0; j < sMesh.MeshBones.Length; j++)
        {
            if (sMesh.MeshBones[j].Name != name) 
                continue;
            
            boneIndexInMesh = j;
            bone = sMesh.MeshBones[j];                    
            return true;
        }
        
        bone = null;
        boneIndexInMesh = 0;
        return false;
    }

    /// <summary> Searches the model for the name of the node. If found, it returns the model node - else returns null. </summary>
    private static SkinModel.ModelNode GetRefToNode(string name, SkinModel.ModelNode node)
    {
        if (node.Name == name) 
            return node;

        return node.Children.Count <= 0 
            ? null 
            : node.Children.Select(t => GetRefToNode(name, t)).FirstOrDefault(result => result != null);
    }

    private class TempVertWeightIndices
    {
        public readonly List<float> VertFlatBoneId = new();
        public readonly List<int> VertIndices = new();
        public readonly List<float> VertBoneWeights = new();
    }
}