# FBXLoader
FBX Loader Animator using Assimp and Monogame supporting normal-maps, 3-lights, fog, rescale (for tutorial)
This code was made for AlienScribble Youtube tutorial series on how to use AssimpNet and how to program a FBX skinned mesh
loader and how to make an animated skin model system. Much of the code is based on Wil Motil's work and the shader stuff
is based on SkinnedEffect by Monogame Team. With this code it is also possible to set up animation blending. 

## Importing from Mixamo
https://www.mixamo.com/#/?page=1&query=walk

Download as ASCII FBX.
Edit the file and adjust the ```RelativeFilename``` to the correct path.

```json
...
	Video: 84600720, "Video::file9", "Clip" {
		Type: "Clip"
		Properties70:  {
			P: "Path", "KString", "XRefUrl", "", "/home/app/mixamo-mini/tmp/skins_6b0edb4d-d077-4da9-9541-65e116a80330.fbm/maria_diffuse.png"
			P: "RelPath", "KString", "XRefUrl", "", "textures/maria_diffuse.png"
		}
		UseMipMap: 0
		Filename: "/home/app/mixamo-mini/tmp/skins_6b0edb4d-d077-4da9-9541-65e116a80330.fbm/maria_diffuse.png"
		RelativeFilename: "textures/maria_diffuse.png"
		Content: ,
... 
```

When in MGCB Editor, Set the:
- ```Processor``` to ```Model - MonoGame```
- ```Importer``` to ```FbxImporter - MonoGame```
- ```Build Action``` to ```Copy```
- ```Default Effect``` to ```Skinned Effect```
