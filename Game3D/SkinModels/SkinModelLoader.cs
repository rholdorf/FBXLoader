//#define USING_COLORED_VERTICES  // uncomment this in both SkinModel and SkinModelLoader if using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public bool FilePathDebug = true;
    public bool LoadedModelInfo = false; //
    public bool AssimpInfo = false;
    public bool MinimalInfo = true; // 
    public bool ConsoleInfo = false; // from here down is mostly run time console info.
    public bool MatrixInfo = false;
    public bool MeshBoneCreationInfo = false;
    public bool MaterialInfo = true;
    public bool FlatBoneInfo = false;
    public bool NodeTreeInfo = false;
    public bool AnimationInfo = false;
    public bool AnimationKeysInfo = false;
    public string TargetNodeName = "";

    readonly GraphicsDevice _graphicsDevice;
    public Scene Scene;
    public string FilePathName;
    public string FilePathNameWithoutExtension;
    public string AltDirectory;
    public static ContentManager Content;
    public static bool UseDebugTex;
    public static Texture2D DebugTex;
    public LoadDebugInfo Info;

    // LoadingLevelPreset: 
    // 0 = TargetRealTimeMaximumQuality, 1 = TargetRealTimeQuality, 2 = TargetRealTimeFast, 3 = custom (does it's best to squash meshes down - good for some older models)
    // ReverseVertexWinding:
    // Reverses the models winding - typically this will change the model vertices to counter clockwise winding (CCW).
    // AddLoopingDuration:
    // Artificially adds a small amount of looping duration to the end of a animation.This helps to fix animations that aren't properly looped.
    // Turn on AddAdditionalLoopingTime to use this.
    // Configuration stuff: 
    // https://github.com/assimp/assimp-net/blob/master/AssimpNet/Configs/PropertyConfig.cs

    public static int LoadingLevelPreset = 3;
    public bool ReverseVerticeWinding = false;
    public float AddedLoopingDuration;

    public List<PropertyConfig> Configurations = new()
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
        Content = content;
        _graphicsDevice = graphicsDevice;
        Info = new LoadDebugInfo(this);
    }

    public void SetDefaultOptions(float addToLoopDuration, string setADebugTexture)
    {
        AddedLoopingDuration = addToLoopDuration;
        DebugTex = Content.Load<Texture2D>(setADebugTexture);
        UseDebugTex = setADebugTexture.Length > 0;
    }

    public SkinModel Load(string filePathOrFileName, string altTextureDirectory, SkinFx skinFx, float rescale = 1f)
    {
        AltDirectory = altTextureDirectory;
        return Load(filePathOrFileName, skinFx, rescale);
    }

    public SkinModel Load(string filePathOrFileName, string altTextureDirectory, bool useDebugTexture, SkinFx skinFx, float rescale = 1f)
    {
        UseDebugTex = useDebugTexture;
        AltDirectory = altTextureDirectory;
        return Load(filePathOrFileName, skinFx, rescale);
    }

    public SkinModel Load(string filePathOrFileName, string altTextureDirectory, bool useDebugTexture, int loadingLevelPreset, SkinFx skinFx, float rescale = 1f)
    {
        LoadingLevelPreset = loadingLevelPreset;
        UseDebugTex = useDebugTexture;
        AltDirectory = altTextureDirectory;
        return Load(filePathOrFileName, skinFx, rescale);
    }

    public SkinModel Load(string filePathOrFileName, SkinFx skinFx, float rescale = 1f)
    {
        FilePathName = filePathOrFileName;
        FilePathNameWithoutExtension = Path.GetFileNameWithoutExtension(filePathOrFileName);
        var s = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Content"), filePathOrFileName); // rem: set FBX to "copy" and remove any file processing properties
        if (File.Exists(s) == false)
        {
            if (FilePathDebug)
            {
                Console.WriteLine("(not found) Checked for: " + s);
            }

            s = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Assets"), filePathOrFileName); // If file is placed in a project assets folder (with copy-to property set)            
        }

        if (File.Exists(s) == false)
        {
            if (FilePathDebug)
            {
                Console.WriteLine("(not found) Instead tried to find: " + s);
            }

            s = Path.Combine(Environment.CurrentDirectory, filePathOrFileName); // maybe in the exe directory
        }

        if (File.Exists(s) == false)
        {
            if (FilePathDebug)
            {
                Console.WriteLine("(not found) Instead tried to find: " + s);
            }

            s = filePathOrFileName; // maybe the exact complete path is specified
        }

        if (FilePathDebug)
        {
            Console.WriteLine("Final file/path checked for: " + s);
        }

        Debug.Assert(File.Exists(s), "Could not find the file to load: " + s);
        var fullFilePath = s;

        // SETUP ASSIMP IMPORTER AND CONFIGURATIONS
        var importer = new AssimpContext();
        foreach (var p in Configurations) importer.SetConfig(p);
        importer.Scale = rescale;

        // LOAD FILE INTO "SCENE" (an assimp imported model is in a thing called Scene)
        try
        {
            switch (LoadingLevelPreset)
            {
                case 0:
                    Scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeMaximumQuality);
                    break;
                case 1:
                    Scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeQuality);
                    break;
                case 2:
                    Scene = importer.ImportFile(fullFilePath, PostProcessPreset.TargetRealTimeFast);
                    break;
                default:
                    Scene = importer.ImportFile(fullFilePath,
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
            throw new Exception("Problem reading file: " + fullFilePath + " (" + ex.Message + ")");
        }

        return CreateModel(fullFilePath, skinFx);
    }

    private SkinModel CreateModel(string fileNom, SkinFx skinFx)
    {
        var model = new SkinModel(_graphicsDevice, skinFx); // create model
        model.RelativeFilename = FilePathName;
        
        CreateRootNode(model, Scene); // prep to build model's tree (need root node)

        CreateMeshesAndBones(model, Scene, 0); // create the model's meshes and bones

        SetupMaterialsAndTextures(model, Scene); // setup the materials and textures of each mesh

        // recursively search and add the nodes for our model from "scene", this includes adding to the flat bone & node lists
        CreateTreeTransforms(model, model.RootNodeOfTree, Scene.RootNode, 0);

        PrepareAnimationsData(model, Scene); // get the animations in the file into each node's animations framelist           

        CopyVertexIndexData(model, Scene); // get the vertices from the meshes  

        Info.AssimpSceneDump(Scene); // can dump scene data if need to            
        Info.DisplayInfo(model, fileNom); // more info

        return model;
    }

    /// <summary> Create a root node </summary>
    private void CreateRootNode(SkinModel model, Scene scene)
    {
        model.RootNodeOfTree = new SkinModel.ModelNode();
        model.RootNodeOfTree.Name = scene.RootNode.Name; // set the rootnode
        // set the rootnode transforms
        model.RootNodeOfTree.LocalMtx = scene.RootNode.Transform.ToMgTransposed(); // ToMg converts to monogame compatible version
        model.RootNodeOfTree.CombinedMtx = model.RootNodeOfTree.LocalMtx;

        if (MaterialInfo)
        {
            LoadDebugInfo.CreatingRootInfo(" Creating root node,  scene.RootNode.Name: " + scene.RootNode.Name + "   scene.RootNode.MeshCount: " + scene.RootNode.MeshCount + "   scene.RootNode.ChildCount: " + scene.RootNode.ChildCount);
            if (MatrixInfo) Console.WriteLine(" scene.RootNode.Transform.ToMgTransposed() " + scene.RootNode.Transform.ToMgTransposed());
        }
    }

    /// <summary> We create model mesh instances for each mesh in scene.meshes. This is just set up here - it doesn't load any data. </summary>
    private void CreateMeshesAndBones(SkinModel model, Scene scene, int meshIndex)
    {
        if (MeshBoneCreationInfo) Console.WriteLine("\n\n@@@CreateModelMeshesAndBones \n");

        model.Meshes = new SkinModel.SkinMesh[scene.Meshes.Count]; // allocate skin meshes array

        // create the meshes (from "scene")
        for (var mi = 0; mi < scene.Meshes.Count; mi++)
        {
            var assimpMesh = scene.Meshes[mi];
            var sMesh = new SkinModel.SkinMesh(); // make new SkinMesh
            sMesh.Name = assimpMesh.Name; // name
            sMesh.MeshNumber = mi; // index from scene                
            sMesh.TexName = "Default"; // texture name
            sMesh.MaterialIndex = assimpMesh.MaterialIndex; // index of material used
            sMesh.MaterialName = scene.Materials[sMesh.MaterialIndex].Name; // material name

            var assimpMeshBones = assimpMesh.Bones;
            sMesh.HasBones = assimpMesh.HasBones; // has bones? 
            sMesh.ShaderMatrices = new Matrix[assimpMeshBones.Count + 1]; // allocate enough shader matrices
            sMesh.ShaderMatrices[0] = Matrix.Identity; // default=identity; if no bone/node animation, this'll keep it static for the duration
            sMesh.MeshBones = new SkinModel.ModelBone[assimpMesh.BoneCount + 1]; // allocate bones

            // DUMMY BONE: Can't yet link this to the node - that must wait - it's not yet created & only exists in the model (not in the assimp bone list)
            sMesh.MeshBones[0] = new SkinModel.ModelBone(); // make dummy ModelBone                
            var flatBone = sMesh.MeshBones[0]; // reference the bone
            flatBone.OffsetMtx = Matrix.Identity;
            flatBone.Name = assimpMesh.Name; // "DummyBone0";
            flatBone.MeshIndex = mi; // index of the mesh this bone belongs to
            flatBone.BoneIndex = 0; // (note that since we're making a dummy bone at index 0, we'll add 1 to the others)                

            // CREATE/ADD BONES (from assimp data):
            for (var abi = 0; abi < assimpMeshBones.Count; abi++)
            {
                // loop thru bones
                var assimpBone = assimpMeshBones[abi]; // refer to bone                    

                var boneIndex = abi + 1; // add 1 because we made a dummy bone
                sMesh.ShaderMatrices[boneIndex] = Matrix.Identity; // init shader matrices
                sMesh.MeshBones[boneIndex] = new SkinModel.ModelBone(); // make ModelBone
                flatBone = sMesh.MeshBones[boneIndex]; // refer to the new bone
                flatBone.Name = assimpBone.Name; // name it
                flatBone.OffsetMtx = assimpBone.OffsetMatrix.ToMgTransposed(); // set the offset matrix (compatible version)
                flatBone.MeshIndex = mi; // assign the associated mesh index
                flatBone.BoneIndex = boneIndex; // index of the bone
                flatBone.NumWeightedVerts = assimpBone.VertexWeightCount; // how many vertex weights? 
                sMesh.MeshBones[boneIndex] = flatBone; // put the new bone into the bone list
            }

            model.Meshes[mi] = sMesh; // add the new SkinMesh to the mesh list

            // SHOW DEBUG INFO (if set to)
            if (MeshBoneCreationInfo)
            {
                LoadDebugInfo.ShowMeshBoneCreationInfo(assimpMesh, sMesh, MatrixInfo, mi);
            }
        }
    }

    /// <summary> Loads textures and sets material values to each model mesh. </summary>
    private void SetupMaterialsAndTextures(SkinModel model, Scene scene)
    {
        for (var i = 0; i < scene.Meshes.Count; i++)
        {
            // loop thru scene meshes
            var sMesh = model.Meshes[i]; // ref to model-mesh
            var matIndex = sMesh.MaterialIndex; // we need the material index (for scene)
            var assimpMaterial = scene.Materials[matIndex]; // get the scene's material 

            sMesh.Ambient = assimpMaterial.ColorAmbient.ToMg(); // minimum light color
            sMesh.Diffuse = assimpMaterial.ColorDiffuse.ToMg(); // regular material colorization
            sMesh.Specular = assimpMaterial.ColorSpecular.ToMg(); // specular highlight color 
            sMesh.Emissive = assimpMaterial.ColorEmissive.ToMg(); // amplify a color brightness (not requiring light - similar to ambient really - kind of a glow without light)                 
            sMesh.Opacity = assimpMaterial.Opacity; // how opaque or see-through is it? 
            sMesh.Reflectivity = assimpMaterial.Reflectivity; // strength of reflections
            sMesh.Shininess = assimpMaterial.Shininess; // how much light shines off
            sMesh.ShineStrength = assimpMaterial.ShininessStrength; // probably specular power (can use to narrow & intensifies highlights - ie: more wet or metallic looking)
            sMesh.BumpScale = assimpMaterial.BumpScaling; // amplify or reduce normal-map effect
            sMesh.IsTwoSided = assimpMaterial.IsTwoSided; // can be useful for glass or ice

            //sMesh.colorTransparent   = assimpMaterial.ColorTransparent.ToMg();
            //sMesh.reflective         = assimpMaterial.ColorReflective.ToMg(); 
            //sMesh.transparency       = assimpMaterial.TransparencyFactor;
            //sMesh.hasShaders         = assimpMaterial.HasShaders;
            //sMesh.shadingMode        = assimpMaterial.ShadingMode.ToString();
            //sMesh.blendMode          = assimpMaterial.BlendMode.ToString();
            //sMesh.isPbrMaterial      = assimpMaterial.IsPBRMaterial;
            //sMesh.isWireFrameEnabled = assimpMaterial.IsWireFrameEnabled;

            var assimpMaterialTextures = assimpMaterial.GetAllMaterialTextures(); // get the list of textures

            for (var t = 0; t < assimpMaterialTextures.Length; t++)
            {
                var textureIndex = assimpMaterialTextures[t].TextureIndex;
                var textureOperation = assimpMaterialTextures[t].Operation;
                var textureType = assimpMaterialTextures[t].TextureType;
                var relativeFilename = assimpMaterialTextures[t].FilePath;
                var xnbFileName = GetXnbFileName(relativeFilename);                    

                // assumes the model is in its specific directory, under "Contents" AND its textures are below it
                var modelDirectory = Path.GetDirectoryName(model.RelativeFilename);
                if (modelDirectory != null)
                    xnbFileName = Path.Combine(modelDirectory, xnbFileName);

                var xnbFullFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory, xnbFileName);

                if (!File.Exists(xnbFullFilePath))
                    throw new FileNotFoundException(null, xnbFullFilePath);

                //xnbFileName = GetFilenameWithoutExtension(xnbFileName);
                //LoadTextures(textureType, model, mi, xnbFileName);
            }
        }
    }

    private void LoadTextures(TextureType textureType, SkinModel model, int mi, string xnbFileName)
    {
        var texture = Content.Load<Texture2D>(xnbFileName);
        switch (textureType)
        {
            case TextureType.Diffuse:
                model.Meshes[mi].TexName = xnbFileName;
                model.Meshes[mi].TexDiffuse = texture;
                break;

            case TextureType.Normals:
                model.Meshes[mi].TexNormMapName = xnbFileName;
                model.Meshes[mi].TexNormalMap = texture;
                break;

            case TextureType.Specular:
                model.Meshes[mi].TexSpecularName = xnbFileName;
                model.Meshes[mi].TexSpecular = texture;
                break;

            case TextureType.Height:
                model.Meshes[mi].TexHeightMapName = xnbFileName;
                model.Meshes[mi].TexHeightMap = texture;
                break;

            case TextureType.Reflection:
                model.Meshes[mi].TexReflectionMapName = xnbFileName;
                model.Meshes[mi].TexReflectionMap = texture;
                break;

            case TextureType.None: break;
            case TextureType.Ambient: break;
            case TextureType.Emissive: break;
            case TextureType.Shininess: break;
            case TextureType.Opacity: break;
            case TextureType.Displacement: break;
            case TextureType.Lightmap: break;
            case TextureType.Unknown: break;
            default: throw new ArgumentOutOfRangeException(nameof(textureType), textureType, null);
        }
    }

    /// <summary> Recursively get scene nodes stuff into our model nodes
    /// - get node name, init local matrix, assign non-bone mesh transforms to corresponding mesh
    /// - match up by name: meshes-index/bone-index with this bone (which meshes and mesh-bone-list-index(for unique mesh-relative offsets) this bone deals with)
    /// - create the children /// </summary>
    private void CreateTreeTransforms(SkinModel model, SkinModel.ModelNode modelNode, Node curAssimpNode, int tabLevel)
    {
        modelNode.Name = curAssimpNode.Name; // get node name
        modelNode.LocalMtx = curAssimpNode.Transform.ToMgTransposed(); // set initial local transform
        modelNode.CombinedMtx = curAssimpNode.Transform.ToMgTransposed();

        // IF IS A MESH NODE: (instead of bone node)
        if (curAssimpNode.HasMeshes)
        {
            modelNode.IsMeshNode = true;
            foreach (var meshIndex in curAssimpNode.MeshIndices)
            {
                // loop through the list of mesh indices
                var sMesh = model.Meshes[meshIndex]; // refer to the corresponding skinMesh
                sMesh.NodeWithAnimTrans = modelNode; // tell the mesh to get it's transform from the current node in the tree
            }
        }

        // For each assimpNode in the tree, we create a uniqueMeshBones list which holds information about which meshes are affected (and thus need index of bone for each mesh)
        // We can find the bone to use within each mesh's bone-list by finding a matching name. We store the applicable mesh#'s and bone#'s 
        // to be able to affect more than 1 mesh with a bone later when recursing the tree for animation updates. 
        for (var mi = 0; mi < Scene.Meshes.Count; mi++)
        {
            if (!GetBoneForMesh(model.Meshes[mi], modelNode.Name, out var bone, out var boneIndexInMesh)) 
                continue;
            // find the bone that goes with this node name
            // MARK AS BONE: 
            modelNode.HasRealBone = true; // yes, we found it in this mesh's bone-list
            modelNode.IsBoneOnRoute = true; // "                    
            bone.MeshIndex = mi; // record index of every mesh the bone should affect
            bone.BoneIndex = boneIndexInMesh; // record index of the bone within each affected mesh's bone-list
            modelNode.UniqueMeshBones.Add(bone); // add the bone into our flat-list of bones (could be only 1 mesh, could be multiple)
        }

        if (NodeTreeInfo)
        {
            Info.ShowNodeTreeInfo(tabLevel, curAssimpNode, MatrixInfo, modelNode, model, Scene);
        }

        // CHILDREN: 
        for (var i = 0; i < curAssimpNode.Children.Count; i++)
        {
            var asimpChildNode = curAssimpNode.Children[i];
            var childNode = new SkinModel.ModelNode(); // made each child node                
            childNode.Parent = modelNode; // set parent before passing
            childNode.Name = curAssimpNode.Children[i].Name; // name the child
            if (childNode.Parent.IsBoneOnRoute) childNode.IsBoneOnRoute = true; // part of actual tree
            modelNode.Children.Add(childNode); // add each child to this node's child list
            CreateTreeTransforms(model, modelNode.Children[i], asimpChildNode, tabLevel + 1); // recursively create transforms for each child
        }
    }

    /// <summary> Gets the assimp animations into our model </summary>
    // http://sir-kimmi.de/assimp/lib_html/_animation_overview.html
    // http://sir-kimmi.de/assimp/lib_html/structai_animation.html
    // http://sir-kimmi.de/assimp/lib_html/structai_anim_mesh.html            

    private void PrepareAnimationsData(SkinModel model, Scene scene)
    {
        if (AnimationInfo) Console.WriteLine("\n\n@@@AnimationsCreateNodesAndCopy \n");

        // Copy animation to ours
        for (var i = 0; i < scene.Animations.Count; i++)
        {
            var assimAnim = scene.Animations[i];
            if (AnimationInfo) Console.WriteLine("  " + "assimpAnim.Name: " + assimAnim.Name);

            // Initially, copy data
            var modelAnim = new SkinModel.RigAnimation();
            modelAnim.DurationInSeconds = assimAnim.DurationInTicks / assimAnim.TicksPerSecond; // Time for entire animation
            modelAnim.DurationInSecondsAdded = AddedLoopingDuration; // May have added duration for animation-loop-fix                
            //modelAnim.TotalFrames  = (int)(modelAnim.DurationInSeconds * (double)(modelAnim.TicksPerSecond)); // just a default value

            // Need an animation-node-list for each animation 
            modelAnim.AnimatedNodes = new List<SkinModel.AnimNodes>(); // lists of S,R,T keyframes for nodes
            // Loop the node channels
            for (var j = 0; j < assimAnim.NodeAnimationChannels.Count; j++)
            {
                var anodeAnimLists = assimAnim.NodeAnimationChannels[j]; // refer to assimp node animation lists (keyframes) for this channel
                var nodeAnim = new SkinModel.AnimNodes(); // make a new animNode [animation-list (keyframes)]
                nodeAnim.NodeName = anodeAnimLists.NodeName; // copy the animation name
                if (AnimationInfo) Console.WriteLine("  " + " Animated Node Name: " + nodeAnim.NodeName);

                // use name to get the node in our tree to refer to 
                var modelnoderef = GetRefToNode(anodeAnimLists.NodeName, model.RootNodeOfTree);
                if (modelnoderef == null) Console.WriteLine("NODE SHOULD NOT BE NULL: " + anodeAnimLists.NodeName);
                nodeAnim.NodeRef = modelnoderef; // set the bone this animation refers to

                // get the rotation, scale, and position keys: 
                foreach (var keyList in anodeAnimLists.RotationKeys)
                {
                    var oaq = keyList.Value; // get open-assimp quaternion
                    nodeAnim.QuaternionRotationTime.Add(keyList.Time / assimAnim.TicksPerSecond); // add to list: rotation-time: time = keyTime / TicksPerSecond
                    nodeAnim.Quaternions.Add(oaq.ToMg()); // add to list: rotation (monogame compatible quaternion)
                }

                foreach (var keyList in anodeAnimLists.PositionKeys)
                {
                    var oap = keyList.Value.ToMg(); // get open-assimp position
                    nodeAnim.PositionTime.Add(keyList.Time / assimAnim.TicksPerSecond); // add to list: position-time: time = keyTime / TicksPerSecond
                    nodeAnim.Position.Add(oap); // add to list: position
                }

                foreach (var keyList in anodeAnimLists.ScalingKeys)
                {
                    var oas = keyList.Value.ToMg(); // get open-assimp scale
                    nodeAnim.ScaleTime.Add(keyList.Time / assimAnim.TicksPerSecond); // add to list: scale-time: time = keyTime / TicksPerSecond
                    nodeAnim.Scale.Add(oas); // add to list: scale 
                }

                // Place this populated node into this model animation:
                modelAnim.AnimatedNodes.Add(nodeAnim);
            }

            // Place the animation into the model.
            model.Animations.Add(modelAnim);
        } // loop scene animations
    } // PrepareAnimationsData

    /// <summary> Copy data from scene to our meshes. </summary> // http://sir-kimmi.de/assimp/lib_html/structai_mesh.html#aa2807c7ba172115203ed16047ad65f9e
    private void CopyVertexIndexData(SkinModel model, Scene scene)
    {
        if (ConsoleInfo) Console.WriteLine("\n\n@@@CopyVerticeIndiceData \n");

        // LOOP SCENE MESHES for VERTEX DATA
        for (var mi = 0; mi < scene.Meshes.Count; mi++)
        {
            var mesh = scene.Meshes[mi];
            if (ConsoleInfo)
            {
                LoadDebugInfo.ShowMeshInfo(mesh, mi);
                for (var i = 0; i < mesh.UVComponentCount.Length; i++)
                {
                    var val = mesh.UVComponentCount[i];
                    Console.Write("\n" + " mesh.UVComponentCount[" + i + "] : " + val);
                }

                Console.Write("\n\n" + " Copying Indices...");
            }

            // INDICES
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

            // VERTICES
            if (ConsoleInfo) Console.Write("\n" + " Copying Vertices...");
            var numVerts = mesh.Vertices.Count;
            var v = new VertexNormMapSkin[numVerts]; // allocate memory for vertex array
            for (var k = 0; k < numVerts; k++)
            {
                // loop vertices in mesh
                var f = mesh.Vertices[k]; // get vertex
                v[k].Pos = new Vector3(f.X, f.Y, f.Z); // copy vertex position
            }

            // NORMALS
            if (ConsoleInfo) Console.Write("\n" + " Copying Normals...");
            for (var k = 0; k < mesh.Normals.Count; k++)
            {
                // loop normals
                var f = mesh.Normals[k]; // get normal    
                v[k].Norm = new Vector3(f.X, f.Y, f.Z); // copy normal
            }

            // VERTEX COLORS

            // A mesh may contain 0 to AI_MAX_NUMBER_OF_COLOR_SETS vertex colors per vertex. NULL if not present. Each array is mNumVertices in size if present. 
            // http://sir-kimmi.de/assimp/lib_html/structai_mesh.html#aa2807c7ba172115203ed16047ad65f9e                


            // UV's
            if (ConsoleInfo) Console.Write("\n" + " Copying Uv TexCoords...");
            var uvchannels = mesh.TextureCoordinateChannels;
            for (var k = 0; k < uvchannels.Length; k++)
            {
                // Loop UV channels
                var vertexUvCoords = uvchannels[k];
                var uvCount = 0;
                for (var j = 0; j < vertexUvCoords.Count; j++)
                {
                    // Loop texture coords
                    var uv = vertexUvCoords[j];
                    v[uvCount].Uv = new Vector2(uv.X, uv.Y); // get the vertex's uv coordinate
                    uvCount++;
                }
            }

            // We already set the Assimp to generate tangents & bitangents if needed (which needs normals &  uv's which we also told it to ensure), so this should work:  
            // TANGENTS
            //if (ConsoleInfo) Console.Write("\n" + " Copying Tangents...");
            //for (int k = 0; k < mesh.Tangents.Count; k++) { 
            //    var f = mesh.Tangents[k];
            //    v[k].tangent = new Vector3(f.X, f.Y, f.Z);                  // copy tangent                    
            //}                
            //// BI-TANGENTS
            //if (ConsoleInfo) Console.Write("\n" + " Copying BiTangents...");
            //for (int k = 0; k < mesh.BiTangents.Count; k++) {
            //    var f = mesh.BiTangents[k];
            //    v[k].biTangent = new Vector3(f.X, f.Y, f.Z);               // copy bi-tangent                    
            //}

            // REGENERATE TANGENTS AND BITANGENTS - LOADED ONES CAUSING NAN------------------------------------------------------------------
            var tan1 = new Vector3[numVerts];
            var tan2 = new Vector3[numVerts];

            for (var index = 0; index < numIndices; index += 3)
            {
                var i1 = indices[index + 0];
                var i2 = indices[index + 1];
                var i3 = indices[index + 2];

                var w1 = v[i1].Uv;
                var w2 = v[i2].Uv;
                var w3 = v[i3].Uv;

                var s1 = w2.X - w1.X;
                var s2 = w3.X - w1.X;
                var t1 = w2.Y - w1.Y;
                var t2 = w3.Y - w1.Y;

                var denom = s1 * t2 - s2 * t1;
                if (Math.Abs(denom) < float.Epsilon)
                {
                    // The triangle UVs are zero sized one dimension. So we cannot calculate the 
                    // vertex tangents for this one trangle, but maybe it can with other trangles.
                    continue;
                }

                var r = 1.0f / denom;
                Debug.Assert(LoaderExtensions.IsFinite(r), "Bad r!");

                var v1 = v[i1].Pos;
                var v2 = v[i2].Pos;
                var v3 = v[i3].Pos;

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
                    Z = (t2 * z1 - t1 * z2) * r,
                };

                var tdir = new Vector3
                {
                    X = (s1 * x2 - s2 * x1) * r,
                    Y = (s1 * y2 - s2 * y1) * r,
                    Z = (s1 * z2 - s2 * z1) * r,
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

            // At this point we have all the vectors accumulated, but we need to average
            // them all out. So we loop through all the final verts and do a Gram-Schmidt
            // orthonormalize, then make sure they're all unit length.
            for (var i = 0; i < numVerts; i++)
            {
                var n = v[i].Norm;
                Debug.Assert(n.IsFinite(), "Bad normal!");
                Debug.Assert(n.Length() >= 0.9999f, "Bad normal!");

                var t = tan1[i];
                if (t.LengthSquared() < float.Epsilon)
                {
                    // TODO: Ideally we could spit out a warning to the content logging here!

                    // We couldn't find a good tanget for this vertex.                        
                    // Rather than set them to zero which could produce errors in other parts of 
                    // the pipeline, we just take a guess at something that may look ok.

                    t = Vector3.Cross(n, Vector3.UnitX);
                    if (t.LengthSquared() < float.Epsilon) t = Vector3.Cross(n, Vector3.UnitY);

                    v[i].Tangent = Vector3.Normalize(t);
                    v[i].BiTangent = Vector3.Cross(n, v[i].Tangent);
                    continue;
                }

                // Gram-Schmidt orthogonalize
                // TODO: This could be zero - could cause NaNs on normalize... how to fix this?
                var tangent = t - n * Vector3.Dot(n, t);
                tangent = Vector3.Normalize(tangent);
                Debug.Assert(tangent.IsFinite(), "Bad tangent!");
                v[i].Tangent = tangent;

                // Calculate handedness
                var w = Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0F ? -1.0F : 1.0F;
                Debug.Assert(LoaderExtensions.IsFinite(w), "Bad handedness!");

                // Calculate the bitangent
                var bitangent = Vector3.Cross(n, tangent) * w;
                Debug.Assert(bitangent.IsFinite(), "Bad bitangent!");
                v[i].BiTangent = bitangent;
            }
            //-------------------------------------------------------------------------------------------------------------------------------

            // GET BOUNDING BOX
            if (ConsoleInfo) Console.Write("\n" + " Calculating min max centroid...");
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            var centroid = Vector3.Zero;
            foreach (var vert in v)
            {
                if (vert.Pos.X < min.X)
                {
                    min.X = vert.Pos.X;
                }

                if (vert.Pos.Y < min.Y)
                {
                    min.Y = vert.Pos.Y;
                }

                if (vert.Pos.Z < min.Z)
                {
                    min.Z = vert.Pos.Z;
                }

                if (vert.Pos.X > max.X)
                {
                    max.X = vert.Pos.X;
                }

                if (vert.Pos.Y > max.Y)
                {
                    max.Y = vert.Pos.Y;
                }

                if (vert.Pos.Z > max.Z)
                {
                    max.Z = vert.Pos.Z;
                }

                centroid += vert.Pos;
            }

            model.Meshes[mi].Mid = centroid / v.Length;
            model.Meshes[mi].Min = min;
            model.Meshes[mi].Max = max;

            // BLEND WEIGHTS AND BLEND INDICES
            if (ConsoleInfo) Console.Write("\n" + " Copying and adjusting blend weights and indexs...");
            for (var k = 0; k < mesh.Vertices.Count; k++)
            {
                v[k].BlendIndices = Vector4.Zero;
                v[k].BlendWeights = Vector4.Zero;
            }

            // Restructure vertex data to conform to a shader.
            // Iterate mesh bone offsets - set the bone Id's and weights to the vertices.
            // This also entails correlating the mesh local bone index names to the flat bone list.
            var verts = new TempVertWeightIndices[mesh.Vertices.Count];
            if (mesh.HasBones)
            {
                model.Meshes[mi].HasBones = mesh.HasBones;
                var assimpBones = mesh.Bones; // refer to current bone set in this mesh
                if (ConsoleInfo) Console.WriteLine("   assimpMeshBones.Count: " + assimpBones.Count);
                // LOOP: ASSIMP MESH BONES
                for (var ambi = 0; ambi < assimpBones.Count; ambi++)
                {
                    // loop the bones of this assimp mesh
                    var assimBone = assimpBones[ambi];
                    var assimBoneName = assimpBones[ambi].Name;
                    var modelBoneIndex = ambi + 1; // add 1 cuz we inserted a dummy at 0

                    // Debug Info (if needed) - could get the entire list of weights
                    if (ConsoleInfo)
                    {
                        var str = "     mesh[" + mi + "].Name: " + mesh.Name + "  bone[" + ambi + "].Name: " + assimBoneName.PadRight(12) + "  assimpMeshBoneIndex: " + ambi.ToString().PadRight(4) + "  WeightCount: " + assimBone.VertexWeightCount;
                        if (assimBone.VertexWeightCount > 0) str += "  ex VertexWeights[0].VertexID: " + assimBone.VertexWeights[0].VertexID;
                        Console.WriteLine(str);
                    }

                    // loop thru this bones vertex listings with the weights for it:
                    for (var weightIndex = 0; weightIndex < assimBone.VertexWeightCount; weightIndex++)
                    {
                        var vertInd = assimBone.VertexWeights[weightIndex].VertexID; // which vertex the bone-weight should be assigned to
                        var weight = assimBone.VertexWeights[weightIndex].Weight; // get the weight
                        if (verts[vertInd] == null) verts[vertInd] = new TempVertWeightIndices(); // add new temp weight thing to store our info                             
                        verts[vertInd].VertIndices.Add(vertInd); // store vert index
                        verts[vertInd].VertFlatBoneId.Add(modelBoneIndex); // store corrent index of bone
                        verts[vertInd].VertBoneWeights.Add(weight); // store weight
                        verts[vertInd].NumBonesForThisVert++; // store how many bones affect this vertex
                    }
                }
            }
            else
            {
                // If there is no bone data we will set it to bone zero. (basically a precaution - no bone data, no bones)
                // In this case, verts need to have a weight and be set to bone 0 (identity).
                // (allows us to draw a boneless mesh [as if entire mesh were attached to a single identity world bone])
                // If there is an actual world mesh node, we can combine the animated transform and set it to that bone as well.
                // So this will work for boneless node mesh transforms which assimp doesn't mark as a actual mesh transform when it is.
                for (var i = 0; i < verts.Length; i++)
                {
                    verts[i] = new TempVertWeightIndices();
                    var ve = verts[i];
                    if (ve.VertIndices.Count == 0)
                    {
                        // there is no bone data for this vertex, then we should set it to bone zero.
                        verts[i].VertIndices.Add(i);
                        verts[i].VertFlatBoneId.Add(0);
                        verts[i].VertBoneWeights.Add(1.0f);
                    }
                }
            }

            // Need up to 4 bone-influences per vertex - empties are 0, weight 0. We can add non-zero entries (0,1,2,3) based on how many were stored.
            // (Note: The bone weight data aligns with offset matrices bone names)                
            for (var i = 0; i < verts.Length; i++)
            {
                // loop the vertices
                if (verts[i] != null)
                {
                    var ve = verts[i]; // get vertex entry
                    //NOTE: maxbones = 4 
                    var arrayIndex = ve.VertIndices.ToArray();
                    var arrayBoneId = ve.VertFlatBoneId.ToArray();
                    var arrayWeight = ve.VertBoneWeights.ToArray();
                    if (arrayBoneId.Length > 3)
                    {
                        v[arrayIndex[3]].BlendIndices.W = arrayBoneId[3]; // we can copy entry 4
                        v[arrayIndex[3]].BlendWeights.W = arrayWeight[3];
                    }

                    if (arrayBoneId.Length > 2)
                    {
                        v[arrayIndex[2]].BlendIndices.Z = arrayBoneId[2]; // " entry 3
                        v[arrayIndex[2]].BlendWeights.Z = arrayWeight[2];
                    }

                    if (arrayBoneId.Length > 1)
                    {
                        v[arrayIndex[1]].BlendIndices.Y = arrayBoneId[1]; // " entry 2
                        v[arrayIndex[1]].BlendWeights.Y = arrayWeight[1];
                    }

                    if (arrayBoneId.Length > 0)
                    {
                        v[arrayIndex[0]].BlendIndices.X = arrayBoneId[0]; // " entry 1
                        v[arrayIndex[0]].BlendWeights.X = arrayWeight[0];
                    }
                }
            }

            model.Meshes[mi].Vertices = v; // refer to vertices and indices we just setup
            model.Meshes[mi].Indices = indices;
            // reverse winding if specified (i2 and i1 are swapped to flip winding direction)
            if (ReverseVerticeWinding)
            {
                for (var k = 0; k < model.Meshes[mi].Indices.Length; k += 3)
                {
                    var i0 = model.Meshes[mi].Indices[k + 0];
                    var i1 = model.Meshes[mi].Indices[k + 1];
                    var i2 = model.Meshes[mi].Indices[k + 2];
                    model.Meshes[mi].Indices[k + 0] = i0;
                    model.Meshes[mi].Indices[k + 1] = i2;
                    model.Meshes[mi].Indices[k + 2] = i1;
                }
            }
        } // end-loop scene meshes vertex data
    }

    /// <summary> Custom get file name </summary>
    public string GetFileName(string s, bool useBothSeperators)
    {
        var tpathsplit = s.Split(new[] { '.' });
        var f = tpathsplit[0];
        if (tpathsplit.Length > 1) f = tpathsplit[tpathsplit.Length - 2];

        if (useBothSeperators)
            tpathsplit = f.Split('/', '\\');
        else
            tpathsplit = f.Split(new[] { '/' });
        s = tpathsplit[tpathsplit.Length - 1];
        return s;
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
    private bool GetBoneForMesh(SkinModel.SkinMesh sMesh, string name, out SkinModel.ModelBone bone, out int boneIndexInMesh)
    {
        var found = false;
        bone = null;
        boneIndexInMesh = 0;
        for (var j = 0; j < sMesh.MeshBones.Length; j++)
        {
            // loop thru the bones of the mesh
            if (sMesh.MeshBones[j].Name == name)
            {
                // found a bone whose name matches                     
                boneIndexInMesh = j; // return the index into the bone-list of the mesh
                bone = sMesh.MeshBones[j]; // return the matching bone                    
                found = true;
            }
        }

        return found;
    }

    /// <summary> Searches the model for the name of the node. If found, it returns the model node - else returns null. </summary>
    private static SkinModel.ModelNode GetRefToNode(string name, SkinModel.ModelNode node)
    {
        SkinModel.ModelNode result = null;
        if (node.Name == name) return node;
        if (result == null && node.Children.Count > 0)
        {
            // must have the result == null && because remember - this is recursive 
            for (var i = 0; i < node.Children.Count; i++)
            {
                result = GetRefToNode(name, node.Children[i]);
                if (result != null)
                {
                    return result; // found it
                }
            }
        }

        return result;
    }

    private class TempVertWeightIndices
    {
        public int NumBonesForThisVert;
        public List<float> VertFlatBoneId = new List<float>();
        public List<int> VertIndices = new List<int>();
        public List<float> VertBoneWeights = new List<float>();
    }
}