using System;
using System.Linq;
using Assimp;

// THIS IS BASED ON WORK BY:  WIL MOTIL  (a slightly older modified version)
// https://github.com/willmotil/MonoGameUtilityClasses

// TO DO: 
// For Minimal Info (bottom), we may want to later add more texture type info as we add support for more texture types. 
// (The code in here is a bit crazy to look at, if desired, one could always add some white space - Wil's version is a bit cleaner)

namespace Game3D.SkinModels.SkinModelHelpers
{
    internal class LoadDebugInfo
    {
        private readonly SkinModelLoader _ld;

        public LoadDebugInfo(SkinModelLoader inst)
        {
            _ld = inst;
        }

        public void AssimpSceneDump(Scene scene)
        {
            if (!_ld.AssimpInfo) return;
            Console.Write("\n\n_______________________________________________");
            Console.Write("\n ---------------------");
            Console.Write("\n AssimpSceneDump...");
            Console.Write("\n --------------------- \n ");
            Console.Write("\n Model Name: " + _ld.FilePathName);
            Console.Write("\n scene.CameraCount: " + scene.CameraCount);
            Console.Write("\n scene.LightCount: " + scene.LightCount);
            Console.Write("\n scene.MeshCount: " + scene.MeshCount);
            Console.Write("\n scene.MaterialCount: " + scene.MaterialCount);
            Console.Write("\n scene.TextureCount: " + scene.TextureCount + "(embedded data)");
            Console.Write("\n scene.AnimationCount: " + scene.AnimationCount);
            Console.Write("\n scene.RootNode.Name: " + scene.RootNode.Name);
            Console.Write("\n \n ");
            Console.Write("\n ///////////////////////////////////////////////////");
            Console.Write("\n scene.Lights");
            var aiLights = scene.Lights;
            for (var i = 0; i < aiLights.Count; i++)
            {
                var aiLight = aiLights[i];
                Console.Write("\n aiLight " + i + " of " + (aiLights.Count - 1) + "");
                Console.Write("\n aiLight.Name: " + aiLight.Name);
            }

            Console.Write("\n \n ///////////////////////////////////////////////////");
            Console.Write("\n scene.Cameras");
            var aiCameras = scene.Cameras;
            for (var i = 0; i < aiCameras.Count; i++)
            {
                var aiCamera = aiCameras[i];
                Console.Write("\n aiCamera " + i + " of " + (aiCameras.Count - 1) + "");
                Console.Write("\n aiCamera.Name: " + aiCamera.Name);
            }

            Console.Write("\n \n ///////////////////////////////////////////////////");
            Console.Write("\n scene.Meshes");
            var aiMeshes = scene.Meshes;
            for (var i = 0; i < aiMeshes.Count; i++)
            {
                var aiMesh = aiMeshes[i];
                Console.Write("\n \n --------------------------------------------------");
                Console.Write("\n Mesh " + i + " of " + (aiMeshes.Count - 1) + "");
                Console.Write("\n aiMesh.Name: " + aiMesh.Name);
                Console.Write("\n aiMesh.VertexCount: " + aiMesh.VertexCount);
                Console.Write("\n aiMesh.FaceCount: " + aiMesh.FaceCount);
                Console.Write("\n aiMesh.Normals.Count: " + aiMesh.Normals.Count);
                Console.Write("\n aiMesh.MorphMethod: " + aiMesh.MorphMethod);
                Console.Write("\n aiMesh.MaterialIndex: " + aiMesh.MaterialIndex);
                Console.Write("\n aiMesh.MeshAnimationAttachmentCount: " + aiMesh.MeshAnimationAttachmentCount);
                Console.Write("\n aiMesh.Tangents.Count: " + aiMesh.Tangents.Count);
                Console.Write("\n aiMesh.BiTangents.Count: " + aiMesh.BiTangents.Count);
                Console.Write("\n aiMesh.VertexColorChannelCount: " + aiMesh.VertexColorChannelCount);
                Console.Write("\n aiMesh.UVComponentCount.Length: " + aiMesh.UVComponentCount.Length);
                Console.Write("\n aiMesh.TextureCoordinateChannelCount: " + aiMesh.TextureCoordinateChannelCount);
                for (var k = 0; k < aiMesh.TextureCoordinateChannels.Length; k++)
                {
                    if (aiMesh.TextureCoordinateChannels[k].Any()) Console.Write("\n aiMesh.TextureCoordinateChannels[" + k + "].Count(): " + aiMesh.TextureCoordinateChannels[k].Count);
                }

                Console.Write("\n aiMesh.BoneCount: " + aiMesh.BoneCount);
                Console.Write("\n \n Bones store a vertex id and a vertex weight. \n ");
                for (var b = 0; b < aiMesh.Bones.Count; b++)
                {
                    var aiMeshBone = aiMesh.Bones[b];
                    Console.Write("\n  aiMesh Bone " + b + " of " + (aiMesh.Bones.Count - 1) + "  aiMeshBone.Name: " + aiMeshBone.Name + "      aiMeshBone.VertexWeightCount: " + aiMeshBone.VertexWeightCount);
                    if (aiMeshBone.VertexWeightCount > 0) Console.Write("    aiMeshBone.VertexWeights[0]VertexID: " + aiMeshBone.VertexWeights[0].VertexID);
                }

                Console.Write("");
            }

            Console.Write("\n \n ///////////////////////////////////////////////////");
            Console.Write("\n scene.Materials");
            var aiMaterials = scene.Materials;
            for (var i = 0; i < aiMaterials.Count; i++)
            {
                var aiMaterial = aiMaterials[i];
                Console.Write("\n \n --------------------------------------------------");
                Console.Write("\n " + "aiMaterial " + i + " of " + (aiMaterials.Count - 1) + "");
                Console.Write("\n " + "aiMaterial.Name: " + aiMaterial.Name);
                Console.Write("\n " + "ColorAmbient: " + aiMaterial.ColorAmbient + "  ColorDiffuse: " + aiMaterial.ColorDiffuse + "  ColorSpecular: " + aiMaterial.ColorSpecular);
                Console.Write("\n " + "ColorEmissive: " + aiMaterial.ColorEmissive + "  ColorReflective: " + aiMaterial.ColorReflective + "  ColorTransparent: " + aiMaterial.ColorTransparent);
                Console.Write("\n " + "Opacity: " + aiMaterial.Opacity + "  Shininess: " + aiMaterial.Shininess + "  ShininessStrength: " + aiMaterial.ShininessStrength);
                Console.Write("\n " + "Reflectivity: " + aiMaterial.Reflectivity + "  ShadingMode: " + aiMaterial.ShadingMode + "  BlendMode: " + aiMaterial.BlendMode + "  BumpScaling: " + aiMaterial.BumpScaling);
                Console.Write("\n " + "IsTwoSided: " + aiMaterial.IsTwoSided + "  IsWireFrameEnabled: " + aiMaterial.IsWireFrameEnabled);
                Console.Write("\n " + "HasTextureAmbient: " + aiMaterial.HasTextureAmbient + "  HasTextureDiffuse: " + aiMaterial.HasTextureDiffuse + "  HasTextureSpecular: " + aiMaterial.HasTextureSpecular);
                Console.Write("\n " + "HasTextureNormal: " + aiMaterial.HasTextureNormal + "  HasTextureDisplacement: " + aiMaterial.HasTextureDisplacement + "  HasTextureHeight: " + aiMaterial.HasTextureHeight + "  HasTextureLightMap: " + aiMaterial.HasTextureLightMap);
                Console.Write("\n " + "HasTextureEmissive: " + aiMaterial.HasTextureEmissive + "  HasTextureOpacity: " + aiMaterial.HasTextureOpacity + "  HasTextureReflection: " + aiMaterial.HasTextureReflection);
                Console.Write("\n");
                // https://github.com/assimp/assimp/issues/3027
                // If the texture data is embedded, the host application can then load 'embedded' texture data directly from the aiScene.mTextures array.
                var aiMaterialTextures = aiMaterial.GetAllMaterialTextures();
                Console.Write("\n aiMaterialTextures.Count: " + aiMaterialTextures.Length);
                for (var j = 0; j < aiMaterialTextures.Count(); j++)
                {
                    var aiTexture = aiMaterialTextures[j];
                    Console.Write("\n \n   " + "aiMaterialTexture [" + j + "]");
                    Console.Write("\n   " + "aiTexture.Name: " + _ld.GetFileName(aiTexture.FilePath, true));
                    Console.Write("\n   " + "FilePath.: " + aiTexture.FilePath);
                    Console.Write("\n   " + "texture.TextureType: " + aiTexture.TextureType);
                    Console.Write("\n   " + "texture.Operation: " + aiTexture.Operation);
                    Console.Write("\n   " + "texture.BlendFactor: " + aiTexture.BlendFactor);
                    Console.Write("\n   " + "texture.Mapping: " + aiTexture.Mapping);
                    Console.Write("\n   " + "texture.WrapModeU: " + aiTexture.WrapModeU + " , V: " + aiTexture.WrapModeV);
                    Console.Write("\n   " + "texture.UVIndex: " + aiTexture.UVIndex);
                }
            }

            Console.Write("\n \n ///////////////////////////////////////////////////");
            Console.Write("\n scene.Animations \n ");
            var aiAnimations = scene.Animations;
            for (var i = 0; i < aiAnimations.Count; i++)
            {
                var aiAnimation = aiAnimations[i];
                Console.Write("\n --------------------------------------------------");
                Console.Write("\n aiAnimation " + i + " of " + (aiAnimations.Count - 1) + "");
                Console.Write("\n aiAnimation.Name: " + aiAnimation.Name);
                if (aiAnimation.NodeAnimationChannels.Count > 0) Console.Write("\n  " + " Animated Nodes...");
                for (var j = 0; j < aiAnimation.NodeAnimationChannels.Count; j++)
                {
                    var nodeAnimLists = aiAnimation.NodeAnimationChannels[j];
                    Console.Write("\n  " + " aiAnimation.NodeAnimationChannels[" + j + "].NodeName: " + nodeAnimLists.NodeName);
                    Node nodeinfo;
                    if (GetAssimpTreeNode(scene.RootNode, nodeAnimLists.NodeName, out nodeinfo))
                    {
                        if (nodeinfo.MeshIndices.Count > 0)
                        {
                            Console.Write("  " + " HasMeshes: " + nodeinfo.MeshIndices.Count);
                            foreach (var n in nodeinfo.MeshIndices) Console.Write("  " + " : " + scene.Meshes[n].Name);
                        }
                    }
                }
            }

            Console.Write("\n -------------------------------------------------- \n ");
            Console.Write("\n ///////////////////////////////////////////////////");
            Console.Write("\n scene  NodeHierarchy");
            AssimpNodeHierarchyDump(scene.RootNode, 0); // ASSIMP NODE HIERARCHY DUMP            
        }

        // ASSIMP NODE HIERARCHY DUMP
        private void AssimpNodeHierarchyDump(Node node, int spaces)
        {
            var indent = "";
            for (var i = 0; i < spaces; i++) indent += "  ";
            Console.Write("\n" + indent + "  node.Name: " + node.Name + "          HasMeshes: " + node.HasMeshes + "    MeshCount: " + node.MeshCount +
                          "    node.ChildCount: " + node.ChildCount + "    MeshIndices.Count " + node.MeshIndices.Count);
            for (var j = 0; j < node.MeshIndices.Count; j++)
            {
                var meshIndex = node.MeshIndices[j];
                Console.Write("\n" + indent + " *meshIndex: " + meshIndex + "  meshIndex name: " + _ld.Scene.Meshes[meshIndex].Name);
            }

            for (var n = 0; n < node.Children.Count(); n++)
            {
                AssimpNodeHierarchyDump(node.Children[n], spaces + 1);
            } // recursive
        }

        private static bool GetAssimpTreeNode(Node treeNode, string name, out Node node)
        {
            var found = false;
            node = null;
            if (treeNode.Name == name)
            {
                found = true;
                node = treeNode;
            }
            else
            {
                foreach (var n in treeNode.Children)
                {
                    found = GetAssimpTreeNode(n, name, out node);
                }
            }

            return found;
        }

        public void DisplayInfo(SkinModel model, string filePath)
        {
            if (_ld.LoadedModelInfo)
            {
                Console.Write("\n\n\n\n****************************************************");
                Console.WriteLine("\n\n@@@DisplayInfo \n \n");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("Model");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine();
                Console.WriteLine("FileName");
                Console.WriteLine(_ld.GetFileName(filePath, true));
                Console.WriteLine();
                Console.WriteLine("Path:");
                Console.WriteLine(filePath);
                Console.WriteLine();
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("Animations");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("");
                for (var i = 0; i < _ld.Scene.Animations.Count; i++)
                {
                    var anim = _ld.Scene.Animations[i];
                    Console.WriteLine("_____________________________________");
                    Console.WriteLine($"Anim #[{i}] Name: {anim.Name}");
                    Console.WriteLine("_____________________________________");
                    Console.WriteLine($"  Duration: {anim.DurationInTicks} / {anim.TicksPerSecond} sec.   total duration in seconds: {anim.DurationInTicks / anim.TicksPerSecond}");
                    Console.WriteLine($"  Node Animation Channels: {anim.NodeAnimationChannelCount} ");
                    Console.WriteLine($"  Mesh Animation Channels: {anim.MeshAnimationChannelCount} ");
                    Console.WriteLine($"  Mesh Morph     Channels: {anim.MeshMorphAnimationChannelCount} ");
                }

                Console.WriteLine();
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("Node Heirarchy");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("");
                InfoModelNode(model, model.RootNodeOfTree, 0);
                Console.WriteLine("");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("Meshes and Materials");
                Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
                Console.WriteLine("");
                InfoForMeshMaterials(model, _ld.Scene);
                Console.WriteLine("");
            }

            if (_ld.MinimalInfo || _ld.LoadedModelInfo)
            {
                MinimalInfo(model, filePath);
            }
        }

        private void InfoModelNode(SkinModel model, SkinModel.ModelNode n, int tabLevel)
        {
            var ntab = "";
            for (var i = 0; i < tabLevel; i++) ntab += "  ";
            var rtab = "\n" + ntab;
            var msg = "\n";
            msg += rtab + $"{n.Name}  ";
            msg += rtab + $"|_children.Count: {n.Children.Count} ";
            if (n.Parent == null)
                msg += "|_parent: IsRoot ";
            else
                msg += "|_parent: " + n.Parent.Name;
            msg += rtab + $"|_hasARealBone: {n.HasRealBone} ";
            msg += rtab + $"|_isThisAMeshNode: {n.IsMeshNode}";
            if (n.UniqueMeshBones.Count > 0)
            {
                msg += rtab + $"|_uniqueMeshBones.Count: {n.UniqueMeshBones.Count}  ";
                var i = 0;
                foreach (var bone in n.UniqueMeshBones)
                {
                    msg += rtab + $"|_node: {n.Name}  lists  uniqueMeshBone[{i}] ...  meshIndex: {bone.MeshIndex}  meshBoneIndex: {bone.BoneIndex}   " + $"mesh[{bone.MeshIndex}]bone[{bone.BoneIndex}].Name: {model.Meshes[bone.MeshIndex].MeshBones[bone.BoneIndex].Name}  " + $"in  mesh[{bone.MeshIndex}].Name: {model.Meshes[bone.MeshIndex].Name}";
                    var nameToCompare = model.Meshes[bone.MeshIndex].MeshBones[bone.BoneIndex].Name;
                    var j = 0;
                    foreach (var anim in model.Animations)
                    {
                        var k = 0;
                        foreach (var animNode in anim.AnimatedNodes)
                        {
                            if (animNode.NodeName == nameToCompare) msg += rtab + $"|^has corresponding Animation[{j}].Node[{k}].Name: {animNode.NodeName}";
                            k++;
                        }

                        j++;
                    }

                    i++;
                }
            }

            Console.WriteLine(msg);
            for (var i = 0; i < n.Children.Count; i++)
            {
                InfoModelNode(model, n.Children[i], tabLevel + 1);
            }
        }

        private void InfoForMeshMaterials(SkinModel model, Scene scene)
        {
            Console.WriteLine("InfoForMaterials");
            Console.WriteLine("Each mesh has a listing of bones that apply to it; this is just a reference to the bone.");
            Console.WriteLine("Each mesh has a corresponding Offset matrix for that bone.");
            Console.WriteLine("Important.");
            Console.WriteLine("This means that offsets are not common across meshes but bones can be.");
            Console.WriteLine("ie: The same bone node may apply to different meshes but that same bone will have a different applicable offset per mesh.");
            Console.WriteLine("Each mesh also has a corresponding bone weight per mesh.");
            for (var amLoop = 0; amLoop < scene.Meshes.Count; amLoop++)
            {
                // loop through assimp meshes
                var assimpMesh = scene.Meshes[amLoop];
                Console.WriteLine("\n" + "__________________________" +
                                  "\n" + "scene.Meshes[" + amLoop + "] " + assimpMesh.Name +
                                  "\n" + " FaceCount: " + assimpMesh.FaceCount +
                                  "\n" + " VertexCount: " + assimpMesh.VertexCount +
                                  "\n" + " Normals.Count: " + assimpMesh.Normals.Count +
                                  "\n" + " Bones.Count: " + assimpMesh.Bones.Count +
                                  "\n" + " MaterialIndex: " + assimpMesh.MaterialIndex +
                                  "\n" + " MorphMethod: " + assimpMesh.MorphMethod +
                                  "\n" + " HasMeshAnimationAttachments: " + assimpMesh.HasMeshAnimationAttachments);
                Console.WriteLine(" UVComponentCount.Length: " + assimpMesh.UVComponentCount.Length);
                for (var i = 0; i < assimpMesh.UVComponentCount.Length; i++)
                {
                    var val = assimpMesh.UVComponentCount[i];
                    if (val > 0) Console.WriteLine("   mesh.UVComponentCount[" + i + "] : int value: " + val);
                }

                Console.WriteLine(" TextureCoordinateChannels.Length:" + assimpMesh.TextureCoordinateChannels.Length);
                Console.WriteLine(" TextureCoordinateChannelCount:" + assimpMesh.TextureCoordinateChannelCount);
                for (var i = 0; i < assimpMesh.TextureCoordinateChannels.Length; i++)
                {
                    var channel = assimpMesh.TextureCoordinateChannels[i];
                    if (channel.Count > 0) Console.WriteLine("   mesh.TextureCoordinateChannels[" + i + "]  count " + channel.Count);
                    //for (int j = 0; j < channel.Count; j++) { // holds uvs and ? i think //Console.Write(" channel[" + j + "].Count: " + channel.Count); }
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n" + "__________________________");
            if (scene.HasTextures)
            {
                var textureCount = scene.TextureCount;
                var textures = scene.Textures;
                Console.WriteLine("\n  Embedded Textures " + " Count " + textureCount);
                for (var i = 0; i < textures.Count; i++)
                {
                    var name = textures[i];
                    Console.WriteLine("    Embedded Textures[" + i + "] " + name);
                }
            }
            else
            {
                Console.WriteLine("\n    Embedded Textures " + " None ");
            }

            Console.WriteLine("\n" + "__________________________");
            if (scene.HasMaterials)
            {
                Console.WriteLine("\n    Materials scene.MaterialCount " + scene.MaterialCount + "\n");
                for (var i = 0; i < scene.Materials.Count; i++)
                {
                    Console.WriteLine();
                    Console.WriteLine("\n    " + "__________________________");
                    Console.WriteLine("    Material[" + i + "] ");
                    Console.WriteLine("    Material[" + i + "].Name " + scene.Materials[i].Name);
                    var m = scene.Materials[i];
                    if (m.HasName)
                    {
                        Console.Write("     Name: " + m.Name);
                    }

                    var t = m.GetAllMaterialTextures();
                    Console.WriteLine("    GetAllMaterialTextures Length " + t.Length);
                    Console.WriteLine();
                    for (var j = 0; j < t.Length; j++)
                    {
                        var tindex = t[j].TextureIndex;
                        var toperation = t[j].Operation;
                        var ttype = Enum.GetName(t[j].TextureType);
                        var tfilepath = t[j].FilePath;
                        // J matches up to the texture coordinate channel uv count it looks like.
                        Console.WriteLine("    Texture[" + j + "] " + "   Index:" + tindex + "   Type: " + ttype + "   Operation: " + toperation + "   Filepath: " + tfilepath);
                    }

                    Console.WriteLine();
                    // added info
                    Console.WriteLine("    Material[" + i + "] " + "  HasColorAmbient " + m.HasColorAmbient + "  HasColorDiffuse " + m.HasColorDiffuse + "  HasColorSpecular " + m.HasColorSpecular);
                    Console.WriteLine("    Material[" + i + "] " + "  HasColorReflective " + m.HasColorReflective + "  HasColorEmissive " + m.HasColorEmissive + "  HasColorTransparent " + m.HasColorTransparent);
                    Console.WriteLine("    Material[" + i + "] " + "  ColorAmbient:" + m.ColorAmbient + "  ColorDiffuse: " + m.ColorDiffuse + "  ColorSpecular: " + m.ColorSpecular);
                    Console.WriteLine("    Material[" + i + "] " + "  ColorReflective:" + m.ColorReflective + "  ColorEmissive: " + m.ColorEmissive + "  ColorTransparent: " + m.ColorTransparent);
                    Console.WriteLine("    Material[" + i + "] " + "  HasOpacity: " + m.HasOpacity + "  Opacity: " + m.Opacity + "  HasShininess:" + m.HasShininess + "  Shininess:" + m.Shininess + "  HasReflectivity: " + m.HasReflectivity + "  Reflectivity " + scene.Materials[i].Reflectivity);
                    Console.WriteLine("    Material[" + i + "] " + "  HasBlendMode:" + m.HasBlendMode + "  BlendMode:" + m.BlendMode + "  HasShadingMode: " + m.HasShadingMode + "  ShadingMode:" + m.ShadingMode + "  HasBumpScaling: " + m.HasBumpScaling + "  HasTwoSided: " + m.HasTwoSided + "  IsTwoSided: " + m.IsTwoSided);
                    Console.WriteLine("    Material[" + i + "] " + "  HasTextureAmbient " + m.HasTextureAmbient + "  HasTextureDiffuse " + m.HasTextureDiffuse + "  HasTextureSpecular " + m.HasTextureSpecular);
                    Console.WriteLine("    Material[" + i + "] " + "  HasTextureNormal " + m.HasTextureNormal + "  HasTextureHeight " + m.HasTextureHeight + "  HasTextureDisplacement:" + m.HasTextureDisplacement + "  HasTextureLightMap " + m.HasTextureLightMap);
                    Console.WriteLine("    Material[" + i + "] " + "  HasTextureReflection:" + m.HasTextureReflection + "  HasTextureOpacity " + m.HasTextureOpacity + "  HasTextureEmissive:" + m.HasTextureEmissive);
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("\n   No Materials Present. \n");
            }

            Console.WriteLine();
            Console.WriteLine("\n" + "__________________________");
            Console.WriteLine("Bones in meshes");
            for (var mindex = 0; mindex < model.Meshes.Length; mindex++)
            {
                var rmMesh = model.Meshes[mindex];
                Console.WriteLine();
                Console.WriteLine("\n" + "__________________________");
                Console.WriteLine("Bones in mesh[" + mindex + "]   " + rmMesh.Name);
                Console.WriteLine();
                if (rmMesh.HasBones)
                {
                    var meshBones = rmMesh.MeshBones;
                    Console.WriteLine(" meshBones.Length: " + meshBones.Length);
                    for (var meshBoneIndex = 0; meshBoneIndex < meshBones.Length; meshBoneIndex++)
                    {
                        var boneInMesh = meshBones[meshBoneIndex]; // ahhhh
                        var boneInMeshName = meshBones[meshBoneIndex].Name;
                        var str = "   mesh[" + mindex + "].Name: " + rmMesh.Name + "   bone[" + meshBoneIndex + "].Name: " + boneInMeshName + "   assimpMeshBoneIndex: " + meshBoneIndex + "   WeightCount: " + boneInMesh.NumWeightedVerts; //str += "\n" + "   OffsetMatrix " + boneInMesh.OffsetMatrix;
                        Console.WriteLine(str);
                    }
                }

                Console.WriteLine();
            }
        }

        private void MinimalInfo(SkinModel model, string filePath)
        {
            Console.WriteLine($"Model [{_ld.GetFileName(filePath, true)}]");
            Console.WriteLine($"    sceneRootNodeOfTree's Node Name: {model.RootNodeOfTree.Name}");
            Console.WriteLine($"    number of animation: {model.Animations.Count}");
            Console.WriteLine($"    number of meshes: {model.Meshes.Length}");
            for (var mmLoop = 0; mmLoop < model.Meshes.Length; mmLoop++)
            {
                var rmMesh = model.Meshes[mmLoop];
                Console.WriteLine($"    mesh #{mmLoop}/{model.Meshes.Length-1} {rmMesh.Name} bones: {model.Meshes[mmLoop].MeshBones.Length} materialIndex: {rmMesh.MaterialIndex} materialIndexName: {rmMesh.MaterialName}");
                if (rmMesh.TexDiffuse != null) 
                    Console.WriteLine($"        texture: {rmMesh.TexName}");
                if (rmMesh.TexNormalMap != null) 
                    Console.WriteLine($"        textureNormalMap: {rmMesh.TexNormMapName}");
            }
        }

        public static void CreatingRootInfo(string str1)
        {
            Console.WriteLine("\n\n@@@CreateRootNode \n");
            Console.WriteLine("\n\n Prep to build a models tree. Set Up the Models RootNode");
            Console.WriteLine(str1);
        }

        public static void ShowMeshBoneCreationInfo(Mesh assimpMesh, SkinModel.SkinMesh sMesh, bool matrixInfo, int mi)
        {
            // If an imported model uses multiple materials, the import splits up the mesh. Use this value as index into the scene's material list. 
            // http://sir-kimmi.de/assimp/lib_html/structai_mesh.html#aa2807c7ba172115203ed16047ad65f9e                   
            Console.Write("\n\n Name " + assimpMesh.Name + " scene.Mesh[" + mi + "] ");
            Console.Write("\n" + " assimpMesh.VertexCount: " + assimpMesh.VertexCount + "  rmMesh.MaterialIndexName: " + sMesh.MaterialName
                          + "   Material index: " + sMesh.MaterialIndex + " (material associated to this mesh)  " + " Bones.Count: " + assimpMesh.Bones.Count);
            Console.Write("\n" + " Note bone 0 doesn't exist in the original assimp bone data structure to facilitate a bone 0 for mesh node transforms so " +
                          "that aibone[0] is converted to modelBone[1]");
            for (var i = 0; i < sMesh.MeshBones.Length; i++)
            {
                var bone = sMesh.MeshBones[i];
                Console.Write("\n Bone [" + i + "] Name " + bone.Name + "  meshIndex: " + bone.MeshIndex + " meshBoneIndex: "
                              + bone.BoneIndex + " numberOfAssociatedWeightedVertices: " + bone.NumWeightedVerts);
                if (matrixInfo) Console.Write("\n  Offset: " + bone.OffsetMtx);
            }
        }

        public void ShowNodeTreeInfo(int tabLevel, Node curAssimpNode, bool matrixInfo, SkinModel.ModelNode modelNode, SkinModel model, Scene scene)
        {
            var tab = new string(' ', tabLevel * 2);
            var tabSpaces = tab + "    ";

            Console.WriteLine("\n\n@@@CreateModelNodeTreeTransformsRecursively \n \n ");
            Console.Write("\n " + tab + "  ModelNode Name: " + modelNode.Name + "  curAssimpNode.Name: " + curAssimpNode.Name);
            if (curAssimpNode.MeshIndices.Count > 0)
            {
                Console.Write("\n " + tab + "  |_This node has mesh references.  aiMeshCount: " + curAssimpNode.MeshCount + " Listed MeshIndices: ");
                for (var i = 0; i < curAssimpNode.MeshIndices.Count; i++) Console.Write(" , " + curAssimpNode.MeshIndices[i]);
                for (var i = 0; i < curAssimpNode.MeshIndices.Count; i++)
                {
                    var modelMesh = model.Meshes[curAssimpNode.MeshIndices[i]];
                    Console.Write("\n " + tab + " " + " |_Is a mesh ... Mesh nodeRefContainingAnimatedTransform Set to node: "
                                  + modelMesh.NodeWithAnimTrans.Name + "  mesh: " + modelMesh.Name);
                }
            }

            if (matrixInfo) Console.WriteLine("\n " + tabSpaces + "|_curAssimpNode.Transform: " + curAssimpNode.Transform.SrtInfoToString(tabSpaces));
            for (var mIndex = 0; mIndex < scene.Meshes.Count; mIndex++)
            {
                if (!GetBoneForMesh(model.Meshes[mIndex], modelNode.Name, out _, out var boneIndexInMesh)) continue;
                Console.Write("\n " + tab + "  |_The node will be marked as having a real bone node along the bone route.");
                if (modelNode.IsMeshNode) Console.Write("\n " + tab + "  |_The node is also a mesh node so this is maybe a node targeting a mesh transform with animations.");
                Console.Write("\n " + tab + "  |_Adding uniqueBone for Node: " + modelNode.Name + " of Mesh[" + mIndex + " of " + scene.Meshes.Count + "].Name: " + scene.Meshes[mIndex].Name);
                Console.Write("\n " + tab + "  |_It's a Bone  in mesh #" + mIndex + "  aiBoneIndexInMesh: " + (boneIndexInMesh - 1) + " adjusted BoneIndexInMesh: " + boneIndexInMesh);
            }
        }

        private static bool GetBoneForMesh(SkinModel.SkinMesh sMesh, string name, out SkinModel.ModelBone bone, out int boneIndexInMesh)
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
                    found = true;
                    bone = sMesh.MeshBones[j]; // return the matching bone
                    boneIndexInMesh = j; // return the index into the bone-list of the mesh
                }
            }

            return found;
        }

        public static void ShowMeshInfo(Mesh mesh, int mi)
        {
            Console.WriteLine(
                "\n" + "__________________________" +
                "\n" + "scene.Meshes[" + mi + "] " + mesh.Name +
                "\n" + " FaceCount: " + mesh.FaceCount +
                "\n" + " VertexCount: " + mesh.VertexCount +
                "\n" + " Normals.Count: " + mesh.Normals.Count +
                "\n" + " Bones.Count: " + mesh.Bones.Count +
                "\n" + " HasMeshAnimationAttachments: " + mesh.HasMeshAnimationAttachments + "\n  (note mesh animations maybe linked to a node animation off the main bone transform chain.)" +
                "\n" + " MorphMethod: " + mesh.MorphMethod +
                "\n" + " MaterialIndex: " + mesh.MaterialIndex +
                "\n" + " VertexColorChannels.Count: " + mesh.VertexColorChannels[mi].Count +
                "\n" + " Tangents.Count: " + mesh.Tangents.Count +
                "\n" + " BiTangents.Count: " + mesh.BiTangents.Count +
                "\n" + " UVComponentCount.Length: " + mesh.UVComponentCount.Length
            );
        }
    }
}