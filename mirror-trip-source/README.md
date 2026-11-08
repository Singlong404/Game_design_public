# Mirror Trip — public source snapshot

**AA_建筑设计沈工 · Rong Shen (Singlong404)**  
Game Off 2026 · Theme: **SHIFT** · Unity **6000.6.5f1**

[Play the official Web demo](https://singlong404.github.io/Game_design_public/mirror-trip/) · [Author](https://github.com/Singlong404)

## Scope / 公开范围

This is the readable source for the current Mirror Trip game: all 69 C# files outside the Editor folder (including two conditionally compiled development helpers), all four custom shaders, the release build entry point, assembly definitions, matching Unity `.meta` files, package manifest/lock and editor version. The 69 files were verified byte-for-byte against the isolated project used for the released build on 2026-11-08. See `SOURCE_MANIFEST.json` for hashes of the published files.

本目录公开正式版的游戏代码，供参赛源码查阅。它不是完整可直接运行的 Unity 工程：场景、预制体、贴图、字体、音频、动画数据与生产美术文件未包含在此次源码快照中。完整体验请打开上方正式试玩链接。

Scenes, prefabs, textures, fonts, audio, animation data and production artwork are intentionally excluded. **This source snapshot alone cannot reproduce the playable build.** Caches, backups, credentials, signing keys and private archives are also excluded. Historical scene-repair utilities and capture probes are not required by the released player and are omitted.

## Code map

| System | Source under `Assets/Project/Scripts/` |
| --- | --- |
| World switching and session state | `Core/WorldSession.cs` |
| Player movement and fall handling | `Player/WorldPlayerMotor.cs` |
| Card shooting and switches | `Card/`, `Puzzle/` |
| Movable, paired and storage mirrors | `Mirror/` |
| Progression, hazards and ending | `Flow/` |
| Dialogue and interactions | `Interaction/` |
| English / Chinese localization | `UI/GameLocalization.cs` |
| UI, journal and level hints | `UI/` |
| Cameras and switch previews | `Camera/` |
| Character animation and effects | `Presentation/` |
| Audio playback | `Audio/LevelAudioDirector.cs` |

Custom shaders are under `Assets/Project/Art/Materials/` and `Assets/Project/Art/Production/NarrativeFinale/`.

## Engine and build reference

Use Unity 6000.6.5f1. Package versions are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`; dependencies include the Input System, Unity UI, 2D packages and URP.

With the complete original project and its scene/assets available, the entry points in `Assets/Project/Scripts/Editor/ReleaseBuild.cs` are `MirrorTrip.Editor.ReleaseBuild.Web` and `MirrorTrip.Editor.ReleaseBuild.Windows`. Set `MIRROR_RELEASE_OUTPUT` to an output directory; set `MIRROR_WINDOWS_MONO=1` for the Windows Mono build. The build scene is `Assets/Project/Scenes/Levels/Level_01.unity` (not included here). The release targets a 1920 × 1080 presentation. No Unity Editor or player compilation was rerun for this publication; copied sources were hash-checked and the public upload was verified separately.

## AI disclosure

AI assistance played a major role in programming, artwork/animation, writing/dialogue, design brainstorming and testing/debugging. The creator directed the project, built and revised the level layout, selected the final content and reviewed the results. Storefront and jam disclosures should reflect that involvement.

## Rights / 署名与使用

This is a **source-available publication, not an open-source license grant**. Copyright (c) 2026 Rong Shen, for original contributions to the extent applicable. All rights reserved, except rights already granted by applicable law or GitHub's Terms of Service. Public visibility permits inspection through GitHub; it does not grant a separate license to redistribute or reuse the game, its code or its artwork. Contact the author for additional permissions. Third-party components retain their own licenses.

中文署名：**AA_建筑设计沈工**。英文署名：**Rong Shen (Singlong404)**。本次仅公开源码，不额外授权商业再利用；第三方素材、软件包的原许可不受影响。比赛的最终资格认定以主办方规则为准。
