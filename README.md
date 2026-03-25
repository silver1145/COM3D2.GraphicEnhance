# COM3D2.GraphicEnhance

A graphics enhancement plugin, which provides:

- **`SkinEnhance`**: add DQS skinning support, and allow dynamic switching between `LBS` and `DQS`.
- **`ShapekeyEnhance`**: accelerate shapekey calculation.
- **`TextureExtend`**: support texture color space and mipmap options, and extend supported texture formats in `tex`.
- **`TextureCache`**: cache texture to reduce memory usage, and add Copy-on-write (copy from gpu when read or write texture pixels) support.

## Install

Download from [Release](https://github.com/silver1145/COM3D2.GraphicEnhance/releases) and Extract `plugins` to `BepinEx/`.

**Note**:

1. Remove [mate_tex_cache](https://github.com/silver1145/scripts-com3d2?tab=readme-ov-file#mate_tex_cache) and [mipmap_extend](https://github.com/silver1145/scripts-com3d2?tab=readme-ov-file#mipmap_extend) from `scripts/` if they are installed.
2. Remove `COM3D2.SKAccelerator` if installed, You can find it in:

- `Sybaris/COM3D2.SKAccelerator.Managed.dll`
- `Sybaris/COM3D2.SKAccelerator.Patcher.dll`
- `Sybaris/UnityInjector/COM3D2.SKAccUtil.Plugin.dll`

## Details

### SkinEnhance

**Option**:

| Name                 | Default | Description                          |
| -------------------- | ------- | ------------------------------------ |
| SkinMethod           | LBS     | LBS/DQS                              |
| SwitchHotkeyModifier | None    | Hotkey modifier to switch SkinMethod |
| SwitchHotkeyKey      | None    | Hotkey to switch SkinMethod          |

`SkinEnhance` introduces DQS skinning, also known in Blender as Preserve Volume skinning. Unity uses `LBS` skinning by default, and this plugin allows switching between `LBS` and `DQS` dynamically at runtime.

Because COM3D2 model assets are authored around `LBS`, enabling `DQS` may introduce artifacts in the hip region, and further work is needed to fully address them.

Another advantage of `DQS` is that it supports arbitrary bind poses more naturally. With `LBS`, meshes created with different bind poses can show obvious inaccuracy when transformed into some pose, especially around joints.

<details>
<summary>Comparison of DQS and LBS</summary>

| Gif                                                                                                                                    | Difference                                                                                                                     |
| -------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| <img src="https://raw.githubusercontent.com/ConstantineRudenko/DQ-skinning-for-Unity/master/Screenshots/before-after.gif" width="400"> | <img src="https://raw.githubusercontent.com/ConstantineRudenko/DQ-skinning-for-Unity/master/Screenshots/diff.png" width="400"> |

</details>

### ShapekeyEnhance

**Option**:

| Name                      | Default | Description                                                      |
| ------------------------- | ------- | ---------------------------------------------------------------- |
| _GlobalEnable             | true    | Global switch                                                    |
| ShapekeyMethod            | Delta   | Delta/Full                                                       |
| DeltaModeFullSyncInterval | 120     | Delta mode full sync interval (seconds) [0 to disbale full sync] |
| DeltaModeBlendPosNormFix  | false   | Delta mode normal fix for some plugin                            |

`ShapekeyEnhance` provides a more efficient multithreaded shapekey blending method and introduces a delta mode that can significantly reduce computation time.

Note that some plugins, such as `COM3D2.ShapeAnimator.Plugin`, may incorrectly call `TMorph.FixBlendValues` on face. This can cause normal problems when delta mode is enabled. If that happens, enable the `DeltaModeBlendPosNormFix` option to correct the issue.

### TextureExtend

**Option**:

| Name          | Default | Description   |
| ------------- | ------- | ------------- |
| _GlobalEnable | true    | Global switch |

`TextureExtend` replaces [mipmap_extend](https://github.com/silver1145/scripts-com3d2?tab=readme-ov-file#mipmap_extend) and expands how `tex` files are imported and configured.

- Automatically enables mipmap for `tex` with `mipmap` keyword in filename.
- Automatically enables linear color space for `tex` with `linear` keyword in filename.

It alse introduces an extended `tex` format by appending another tex data to the end of the file. This extended format allows importing textures in a wider range of texture formats while remaining forward compatible. Even without this plugin, the extend `tex` file can still be loaded normally. The main purpose of this feature is to support better compression options for large textures, such as `BC7`, which can reduce VRAM usage and keep texture quality.

### TextureCache

**Option**:

| Name            | Default | Description                        |
| --------------- | ------- | ---------------------------------- |
| _GlobalEnable   | true    | Global switch                      |
| AlwaysLoadCheck | false   | Always to check tex hash when load |

`TextureCache` replaces [mate_tex_cache](https://github.com/silver1145/scripts-com3d2?tab=readme-ov-file#mate_tex_cache) with a redesigned caching workflow focused on better performance and lower overhead. It
uses a copy-on-write style workflow. Texture will upload to GPU first, if texture pixels need to be read or modified later, the texture data is copied back from the GPU first.

If `COM3D2.MaidLoader` is installed, the refresh function of MaidLoader will mark all caches as expired. When loading expired caches, it will be decided whether to reload based on the file hash. You can also enable `AlwaysLoadCheck` to always verify the file hash when loading.

### Others

**Option**:

| Name           | Default                  | Description             |
| -------------- | ------------------------ | ----------------------- |
| ThreadPoolSize | CPU logical core num - 1 | Global Thread Pool Size |
