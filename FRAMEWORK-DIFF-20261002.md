# DownBreak 框架 vs 本项目框架 差异报告

- 生成时间: 2026-10-02 18:57:28
- 源侧(那边): `C:\UnityProject\DownBreak\Assets\MieMieFrameTools`
- 本侧(这边): `Assets\MieMieFrameTools`

## 一 总览

| 指标 | 数量 |
| --- | --- |
| 源侧文件总数 | 1073 |
| 本侧文件总数 | 2946 |
| 两边同名文件 | 303 |
| 其中字节完全相同 | 265 |
| 其中内容不同 | 38 |
| 源侧独有路径 | 770 |
| 本侧独有路径 | 2643 |
| 源侧独有 -> 真缺失(本侧无同名文件) | 454 |
| 源侧独有 -> 改名或挪位(本侧有同名文件) | 316 |

结论: 两边框架同源但已分叉; 源侧独有内容里真正需要关注的自有代码/资源只有 43 个文件。

## 二 真缺失 - 自有内容(建议逐项确认)

| 相对路径 | 字节 |
| --- | --- |
| ARequired\GameSave\GameSave.proto | 995 |
| Editor\Protobuf\protobuf_settings.json | 112 |
| Editor\SaveForEditor\Protobuf\Generator\EditorPrefsHelper.cs | 1286 |
| Editor\SaveForEditor\Protobuf\Generator\ProtocGenerator.cs | 5997 |
| Editor\SaveForEditor\Protobuf\protobuf_settings.json | 465 |
| Editor\SaveForEditor\Protobuf\ProtobufDownloadWindow.cs | 2618 |
| Editor\SaveForEditor\Protobuf\ProtobufGeneratorWindow.cs | 12375 |
| Editor\SaveForEditor\Protobuf\ProtobufMenu.cs | 649 |
| Editor\SaveForEditor\Protobuf\ProtobufSettingsStore.cs | 3882 |
| Editor\SaveForEditor\Protobuf\UI\ProtobufGeneratorWindow.uss | 4390 |
| Editor\SaveForEditor\Protobuf\UI\ProtobufGeneratorWindow.uxml | 3833 |
| Editor\SaveForEditor\Protoc\protoc.exe | 12842177 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Docs\MmAsset-改进清单.md | 34845 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\buidinpack_AbConfig.json | 81242 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\canplaceandbreakitem_AbConfig.json | 34832 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\config_AbConfig.json | 74033 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\consumable_AbConfig.json | 43576 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\equiment_AbConfig.json | 28980 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\icon_AbConfig.json | 17171 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\materials_AbConfig.json | 12657 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\player_AbConfig.json | 46515 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\ui_AbConfig.json | 19037 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\weapon_AbConfig.json | 15211 |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated\world_AbConfig.json | 6483 |
| Scripts\Frame\C_Pool\IPoolable.cs | 381 |
| Scripts\Frame\C_Pool\PoolMember.cs | 326 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\DOTweenSequence.cs | 19786 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\DOTweenSequencePreset.cs | 3695 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Extensions\TransformExtensions.cs | 4819 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_ButtonPunch_Scale.asset | 1892 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_FadeIn.asset | 1213 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_FadeOut.asset | 1213 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_PopupHide_ScaleFade.asset | 1893 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_PopupShow_ScaleFade.asset | 1894 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_Progress_Fill.asset | 1219 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_SlideIn_FromBottom.asset | 1228 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\Presets\UI_UPCrossFade.asset | 1880 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\SequenceAnimationResetApplier.cs | 3876 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\DoTweeExtension\DoTweenAnimExtension\SequenceAnimationTweenBuilder.cs | 22920 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\DoTweenAnim\DOTweenSequenceEditor.cs | 21587 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\DoTweenAnim\DOTweenTypeEditorUtil.cs | 7389 |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\DoTweenAnim\SequenceAnimationDrawer.cs | 20644 |
| Scripts\Tools\UniTask\AsyncTaskManager.cs | 10398 |

## 三 真缺失 - 第三方插件(通常无需迁移)

| 相对路径 | 字节 |
| --- | --- |
| Plugins\Demigiant\DemiLib\Core\DemiLib.dll | 14848 |
| Plugins\Demigiant\DemiLib\Core\DemiLib.xml | 10312 |
| Plugins\Demigiant\DemiLib\Core\Editor\DemiEditor.dll | 195072 |
| Plugins\Demigiant\DemiLib\Core\Editor\DemiEditor.xml | 153309 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquare.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquareAlpha10.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquareAlpha15.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquareAlpha25.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquareAlpha50.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blackSquareAlpha80.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\blueSquare.png | 106 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\circle.png | 455 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\circle_dashedBorderEmpty.png | 673 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\greenSquare.png | 106 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\grid_bright.png | 222 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\grid_dark.png | 216 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alert.png | 540 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignB.png | 131 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignBC.png | 158 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignBL.png | 152 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignBR.png | 150 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignCC.png | 152 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignCL.png | 158 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignCR.png | 153 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignHC.png | 147 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignL.png | 136 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignR.png | 145 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignT.png | 129 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignTC.png | 164 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignTL.png | 153 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignTR.png | 155 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_alignVC.png | 135 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_camera.png | 206 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_camera_border.png | 347 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_cog.png | 197 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_cog_border.png | 376 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_comment.png | 176 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_comment_border.png | 426 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_delete.png | 145 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_demigiant.png | 605 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_distributeHAlignT.png | 174 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_distributeVAlignL.png | 197 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_doing.png | 838 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_end.png | 475 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_flipV.png | 167 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_foldout_closed.png | 158 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_foldout_open.png | 170 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_heart.png | 210 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_heart_border.png | 307 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_light.png | 190 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_light_border.png | 292 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_lock.png | 300 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_lock_open.png | 293 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_nodeArrow.png | 168 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_ok.png | 715 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_optionsDropdown.png | 135 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_play.png | 256 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_play_border.png | 386 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_skull.png | 227 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_skull_border.png | 332 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_star.png | 203 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_star_border.png | 323 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_todo.png | 674 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_ui.png | 223 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_ui_border.png | 259 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_visibility.png | 298 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\ico_visibility_off.png | 367 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\orangeSquare.png | 104 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_atlas.png | 218 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_audio.png | 338 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_bundle.png | 522 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_cog.png | 406 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_cross.png | 210 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_demigiant.png | 388 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_folder.png | 150 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_fonts.png | 465 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_heart.png | 334 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_materials.png | 355 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_models.png | 380 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_particles.png | 493 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_play.png | 217 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_prefab.png | 343 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_scripts.png | 208 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_shaders.png | 379 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_skull.png | 368 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_star.png | 289 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_terrains.png | 309 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\project\ico_textures.png | 278 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\purpleSquare.png | 106 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\redSquare.png | 106 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorder.png | 122 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderAlpha15.png | 116 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurved.png | 196 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurved_darkBorders.png | 216 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurved_darkBordersAlpha.png | 200 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurved02.png | 281 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurved02_darkBorders.png | 336 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurvedAlpha.png | 197 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurvedEmpty.png | 180 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurvedEmpty02.png | 217 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderCurvedEmptyThick.png | 233 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderEmpty.png | 114 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderEmpty01.png | 114 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderEmpty02.png | 121 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderEmpty03.png | 122 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderThickEmpty.png | 121 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareBorderThickerEmpty.png | 122 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareCorners03.png | 133 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\squareCornersEmpty02.png | 131 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\tileBars_empty.png | 236 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\tileBars_slanted.png | 285 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\tileBars_slanted_alpha.png | 266 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\tileCheckerboard.png | 397 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\transparentSquare.png | 97 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteDot.png | 148 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteDot_darkBorder.png | 219 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteDot_whiteBorderAlpha.png | 176 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquare.png | 103 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquare_fadeOut_bt.png | 131 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareAlpha10.png | 107 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareAlpha15.png | 109 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareAlpha25.png | 107 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareAlpha50.png | 107 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareAlpha80.png | 110 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareCurved.png | 144 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\whiteSquareCurved02.png | 190 |
| Plugins\Demigiant\DemiLib\Core\Editor\Imgs\yellowSquare.png | 106 |
| Plugins\Demigiant\DOTween\DOTween.dll | 175616 |
| Plugins\Demigiant\DOTween\DOTween.XML | 232971 |
| Plugins\Demigiant\DOTween\Editor\DOTweenEditor.dll | 70656 |
| Plugins\Demigiant\DOTween\Editor\DOTweenEditor.XML | 7956 |
| Plugins\Demigiant\DOTween\Editor\Imgs\DOTweenIcon.png | 1565 |
| Plugins\Demigiant\DOTween\Editor\Imgs\DOTweenMiniIcon.png | 319 |
| Plugins\Demigiant\DOTween\Editor\Imgs\Footer.png | 4409 |
| Plugins\Demigiant\DOTween\Editor\Imgs\Footer_dark.png | 4429 |
| Plugins\Demigiant\DOTween\Editor\Imgs\Header.jpg | 22787 |
| Plugins\Demigiant\DOTween\Modules\DOTween.Modules.asmdef | 376 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleAudio.cs | 8943 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleEPOOutline.cs | 6855 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModulePhysics.cs | 13989 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModulePhysics2D.cs | 12125 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleSprite.cs | 4166 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleUI.cs | 43245 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleUnityVersion.cs | 18321 |
| Plugins\Demigiant\DOTween\Modules\DOTweenModuleUtils.cs | 6719 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenAnimation_Advanced.unity | 144315 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenAnimation_AdvancedSettings.lighting | 1814 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenAnimation_Basics.unity | 46286 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenAnimation_BasicsSettings.lighting | 1812 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenPath.unity | 39190 |
| Plugins\Demigiant\DOTweenPro Examples\DOTweenPathSettings.lighting | 1800 |
| Plugins\Demigiant\DOTweenPro Examples\Examples Assets\dotweenpro_logo.png | 17098 |
| Plugins\Demigiant\DOTweenPro\DOTweenAnimation.cs | 39038 |
| Plugins\Demigiant\DOTweenPro\DOTweenDeAudio.cs | 260 |
| Plugins\Demigiant\DOTweenPro\DOTweenDeUnityExtended.cs | 260 |
| Plugins\Demigiant\DOTweenPro\DOTweenPro.dll | 16384 |
| Plugins\Demigiant\DOTweenPro\DOTweenPro.XML | 3556 |
| Plugins\Demigiant\DOTweenPro\DOTweenProShortcuts.cs | 4084 |
| Plugins\Demigiant\DOTweenPro\DOTweenTextMeshPro.cs | 61852 |
| Plugins\Demigiant\DOTweenPro\DOTweenTk2d.cs | 15428 |
| Plugins\Demigiant\DOTweenPro\Editor\DOTweenAnimationInspector.cs | 40525 |
| Plugins\Demigiant\DOTweenPro\Editor\DOTweenPreviewManager.cs | 11954 |
| Plugins\Demigiant\DOTweenPro\Editor\DOTweenProEditor.dll | 35840 |
| Plugins\Demigiant\DOTweenPro\Editor\DOTweenProEditor.XML | 542 |
| Plugins\Demigiant\readme_DOTweenPro.txt | 2439 |
| Plugins\Protobuf\Google.Protobuf.dll | 498328 |

## 四 改名 / 挪位(不算缺失, 本侧已有同名文件)

| 源侧路径 | 本侧实际位置 |
| --- | --- |
| ARequired\FrameForPrefabs\EfPlyaAS.prefab | ADefaultRes\FramePrefabs\EfPlyaAS.prefab |
| ARequired\FrameForPrefabs\EfPlyaAS.prefab.meta | ADefaultRes\FramePrefabs\EfPlyaAS.prefab.meta |
| ARequired\FrameForPrefabs\FrameRoot.prefab | ADefaultRes\FramePrefabs\FrameRoot.prefab |
| ARequired\FrameForPrefabs\FrameRoot.prefab.meta | ADefaultRes\FramePrefabs\FrameRoot.prefab.meta |
| ARequired\Front\3500+symbols.txt | ADefaultRes\Arts\FrontArt\3500+symbols.txt |
| ARequired\Front\3500+symbols.txt.meta | ADefaultRes\Arts\FrontArt\3500+symbols.txt.meta |
| ARequired\Front\7000+symbols.txt | ADefaultRes\Arts\FrontArt\7000+symbols.txt |
| ARequired\Front\7000+symbols.txt.meta | ADefaultRes\Arts\FrontArt\7000+symbols.txt.meta |
| ARequired\Front\NotoSansSC SDF.asset | ADefaultRes\Arts\FrontArt\NotoSansSC SDF.asset |
| ARequired\Front\NotoSansSC SDF.asset.meta | ADefaultRes\Arts\FrontArt\NotoSansSC SDF.asset.meta |
| ARequired\Front\ZLabsRoundPix_16px_M_CN SDF.asset | ADefaultRes\Arts\FrontArt\ZLabsRoundPix_16px_M_CN SDF.asset |
| ARequired\Front\ZLabsRoundPix_16px_M_CN SDF.asset.meta | ADefaultRes\Arts\FrontArt\ZLabsRoundPix_16px_M_CN SDF.asset.meta |
| ARequired\Front\ZLabsRoundPix_16px_M_CN.ttf | ADefaultRes\Arts\FrontArt\ZLabsRoundPix_16px_M_CN.ttf |
| ARequired\Front\ZLabsRoundPix_16px_M_CN.ttf.meta | ADefaultRes\Arts\FrontArt\ZLabsRoundPix_16px_M_CN.ttf.meta |
| ARequired\GameSave\Generated.meta | Scripts\Frame\C_Data\Luban\Generated.meta |
| Editor\SaveForEditor\Protobuf\README.md | Scripts\Frame\B_Assets\Yooasset\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\AlipayFileSystem\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\KuaiShouFileSystem\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\OppoFileSystem\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\TaptapFileSystem\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\VivoFileSystem\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniEvent\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniMachine\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniUtility\README.md<br>Scripts\Frame\B_Assets\Yooasset\Samples~\UniTask Sample\README.md |
| Editor\SaveForEditor\Protobuf\README.md.meta | Scripts\Frame\B_Assets\Yooasset\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\AlipayFileSystem\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\KuaiShouFileSystem\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\OppoFileSystem\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\TaptapFileSystem\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime\VivoFileSystem\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniEvent\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniMachine\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniUtility\README.md.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\UniTask Sample\README.md.meta |
| Plugins\Demigiant\DOTween\Modules.meta | Plugins\Sirenix\Odin Inspector\Modules.meta |
| Plugins\Demigiant\DOTween\readme.txt | Plugins\Sirenix\Readme.txt |
| Plugins\Demigiant\DOTween\readme.txt.meta | Plugins\Sirenix\Readme.txt.meta |
| Plugins\Demigiant\DOTweenPro\readme.txt | Plugins\Sirenix\Readme.txt |
| Plugins\Demigiant\DOTweenPro\readme.txt.meta | Plugins\Sirenix\Readme.txt.meta |
| Plugins\Hierarchy Designer\Editor.meta | Editor.meta<br>Editor\GraphViewFrame\Editor.meta<br>Plugins\Demigiant\DemiLib\Core\Editor.meta<br>Plugins\Demigiant\DOTween\Editor.meta<br>Plugins\Demigiant\DOTweenPro\Editor.meta<br>Plugins\Sirenix\Odin Inspector\Assets\Editor.meta<br>Plugins\Sirenix\Odin Inspector\Config\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Extension Sample\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Test Sample\Editor.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Editor.meta |
| Plugins\Protobuf\System.Runtime.CompilerServices.Unsafe.dll | Plugins\MessagePack\System.Runtime.CompilerServices.Unsafe.dll |
| Plugins\Protobuf\System.Runtime.CompilerServices.Unsafe.dll.meta | Plugins\MessagePack\System.Runtime.CompilerServices.Unsafe.dll.meta |
| Scripts\Frame\A_FrameBase\ModuleHub.cs | Scripts\Frame\A_Frame\Frame\ModuleHub.cs |
| Scripts\Frame\A_FrameBase\ModuleHub.cs.meta | Scripts\Frame\A_Frame\Frame\ModuleHub.cs.meta |
| Scripts\Frame\A_FrameBase\MonoManager.cs | Scripts\Frame\A_Frame\Unity\MonoManager.cs |
| Scripts\Frame\A_FrameBase\MonoManager.cs.meta | Scripts\Frame\A_Frame\Unity\MonoManager.cs.meta |
| Scripts\Frame\A_FrameBase\Single\Singleton.cs | Scripts\Frame\A_Frame\Frame\Singleton.cs |
| Scripts\Frame\A_FrameBase\Single\Singleton.cs.meta | Scripts\Frame\A_Frame\Frame\Singleton.cs.meta |
| Scripts\Frame\A_FrameBase\Single\SingletonMono.cs | Scripts\Frame\A_Frame\Unity\SingletonMono.cs |
| Scripts\Frame\A_FrameBase\Single\SingletonMono.cs.meta | Scripts\Frame\A_Frame\Unity\SingletonMono.cs.meta |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.cs | Scripts\Frame\B_Assets\Addressable\AddressableMgr.cs |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.cs.meta | Scripts\Frame\B_Assets\Addressable\AddressableMgr.cs.meta |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Lifecycle.cs | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Lifecycle.cs |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Lifecycle.cs.meta | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Lifecycle.cs.meta |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Load.Async.cs | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Load.Async.cs |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Load.Async.cs.meta | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Load.Async.cs.meta |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Load.Sync.cs | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Load.Sync.cs |
| Scripts\Frame\B_Assets\AddressableMethod\AddressableMgr.Load.Sync.cs.meta | Scripts\Frame\B_Assets\Addressable\AddressableMgr.Load.Sync.cs.meta |
| Scripts\Frame\B_Assets\AddressableMethod\AddressConfig.cs | Scripts\Frame\B_Assets\Addressable\AddressConfig.cs |
| Scripts\Frame\B_Assets\AddressableMethod\AddressConfig.cs.meta | Scripts\Frame\B_Assets\Addressable\AddressConfig.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset.meta | Scripts\Frame\B_Assets\MmAsset.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Docs.meta | Scripts\Frame\B_Assets\MmAsset\Docs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Docs\MmAsset-使用手册.md | Scripts\Frame\B_Assets\MmAsset\Docs\MmAsset-使用手册.md |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Docs\MmAsset-使用手册.md.meta | Scripts\Frame\B_Assets\MmAsset\Docs\MmAsset-使用手册.md.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Generated.meta | Scripts\Frame\C_Data\Luban\Generated.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Resources.meta | Plugins\Sirenix\Odin Inspector\Config\Resources.meta<br>Scripts\Frame\B_Assets\MmAsset\Resources.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\Resources.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Resources\buidinpack_builtin.json | Scripts\Frame\B_Assets\MmAsset\Resources\buidinpack_builtin.json |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Resources\buidinpack_builtin.json.meta | Scripts\Frame\B_Assets\MmAsset\Resources\buidinpack_builtin.json.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Resources\BundleSettings.asset | Scripts\Frame\B_Assets\MmAsset\Resources\BundleSettings.asset |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Resources\BundleSettings.asset.meta | Scripts\Frame\B_Assets\MmAsset\Resources\BundleSettings.asset.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime.meta | Scripts\Frame\A_Frame\Business\Samples\Player\Data\Runtime.meta<br>Scripts\Frame\B_Assets\MmAsset\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Extension Sample\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Mini Game\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\GameScript\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniEvent\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniMachine\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Space Shooter\ThirdParty\UniFramework\UniUtility\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Test Sample\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\UniTask Sample\UniTask\Runtime.meta<br>Scripts\Frame\B_Assets\Yooasset\YooSample\Runtime.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Runtime.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Runtime\Widgets\FloatingTextSystem\Runtime.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Runtime\Widgets\ItemWheel\Runtime.meta<br>Scripts\Frame\E_Input\InputInteract\Runtime.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\AssetPipelineTypes.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Boot\AssetPipelineTypes.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\AssetPipelineTypes.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Boot\AssetPipelineTypes.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Boot.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Boot.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Boot\MmAssetBootManager.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Boot\MmAssetBootManager.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Boot\MmAssetBootManager.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Boot\MmAssetBootManager.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BuiltIn.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\BuiltIn.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BuiltIn\AssetsDeCompressManager.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\BuiltIn\AssetsDeCompressManager.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BuiltIn\AssetsDeCompressManager.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\BuiltIn\AssetsDeCompressManager.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BuiltIn\IBuiltInAssets.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\BuiltIn\IBuiltInAssets.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BuiltIn\IBuiltInAssets.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\BuiltIn\IBuiltInAssets.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleModuleDelivery.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleModuleDelivery.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleModuleDelivery.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleModuleDelivery.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleModuleEnum.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleModuleEnum.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleModuleEnum.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleModuleEnum.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleSettings.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleSettings.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\BundleSettings.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Config\BundleSettings.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\AssetsDownLoader.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\AssetsDownLoader.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\AssetsDownLoader.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\AssetsDownLoader.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\DownloadThread.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\DownloadThread.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\DownloadThread.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\DownloadThread.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsManager.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsManager.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsManager.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsManager.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsManifest.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsManifest.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsManifest.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsManifest.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsModule.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsModule.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\HotAssetsModule.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\HotAssetsModule.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\IHotAssets.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\IHotAssets.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\HotUpdate\IHotAssets.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\HotUpdate\IHotAssets.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\AssetBundleManager.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\AssetBundleManager.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\AssetBundleManager.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\AssetBundleManager.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\BundleConfig.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\BundleConfig.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\BundleConfig.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\BundleConfig.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\CacheObject.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\CacheObject.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\CacheObject.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\CacheObject.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\ClassObjectPool.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\ClassObjectPool.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\ClassObjectPool.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\ClassObjectPool.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\IResourcesInterface.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\IResourcesInterface.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\IResourcesInterface.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\IResourcesInterface.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\ResourceManager.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\ResourceManager.cs<br>Scripts\Frame\B_Assets\Yooasset\Runtime\ResourceManager\ResourceManager.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Loading\ResourceManager.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Loading\ResourceManager.cs.meta<br>Scripts\Frame\B_Assets\Yooasset\Runtime\ResourceManager\ResourceManager.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\MmAssetFrame.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Core\MmAssetFrame.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\MmAssetFrame.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Core\MmAssetFrame.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\MmAssetMgr.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Core\MmAssetMgr.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\MmAssetMgr.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Core\MmAssetMgr.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Security.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\AES.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\AES.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\AES.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\AES.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\CRC32.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\CRC32.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\CRC32.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\CRC32.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\MD5.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\MD5.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Security\MD5.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Security\MD5.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Utilities.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Utilities.meta<br>Scripts\Frame\B_Assets\Yooasset\Editor\Utilities.meta<br>Scripts\Frame\B_Assets\Yooasset\Runtime\Utility\Utilities.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Utilities\FileHelper.cs | Scripts\Frame\B_Assets\MmAsset\Runtime\Utilities\FileHelper.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Runtime\Utilities\FileHelper.cs.meta | Scripts\Frame\B_Assets\MmAsset\Runtime\Utilities\FileHelper.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples.meta | Scripts\Frame\A_Frame\Business\Samples.meta<br>Scripts\Frame\B_Assets\MmAsset\Samples.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载.meta | Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载\1.unity | Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\1.unity |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载\1.unity.meta | Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\1.unity.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载\Scripts.meta | Scripts.meta<br>Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\Scripts.meta<br>Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\Scripts.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载\Scripts\Load.cs | Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\Scripts\Load.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\1.随包资源加载\Scripts\Load.cs.meta | Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\Scripts\Load.cs.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载.meta | Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载\2.unity | Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\2.unity |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载\2.unity.meta | Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\2.unity.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载\Scripts.meta | Scripts.meta<br>Scripts\Frame\B_Assets\MmAsset\Samples\1.随包资源加载\Scripts.meta<br>Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\Scripts.meta |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载\Scripts\HotLoad.cs | Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\Scripts\HotLoad.cs |
| Scripts\Frame\B_Assets\MmAssetsMethod\MmAsset\Samples\2.热更资源加载\Scripts\HotLoad.cs.meta | Scripts\Frame\B_Assets\MmAsset\Samples\2.热更资源加载\Scripts\HotLoad.cs.meta |
| Scripts\Frame\C_Pool\GameObjPool.cs | Scripts\Tools\CodingTools\Pool\Pool\GameObjPool.cs |
| Scripts\Frame\C_Pool\GameObjPool.cs.meta | Scripts\Tools\CodingTools\Pool\Pool\GameObjPool.cs.meta |
| Scripts\Frame\C_Pool\GameObjPoolReporter.cs | Scripts\Tools\CodingTools\Pool\Diagnostics\GameObjPoolReporter.cs |
| Scripts\Frame\C_Pool\GameObjPoolReporter.cs.meta | Scripts\Tools\CodingTools\Pool\Diagnostics\GameObjPoolReporter.cs.meta |
| Scripts\Frame\C_Pool\ObjectPool.cs | Scripts\Tools\CodingTools\Pool\Pool\ObjectPool.cs |
| Scripts\Frame\C_Pool\ObjectPool.cs.meta | Scripts\Tools\CodingTools\Pool\Pool\ObjectPool.cs.meta |
| Scripts\Frame\C_Pool\PoolExtensions.cs | Scripts\Tools\CodingTools\Pool\Core\PoolExtensions.cs |
| Scripts\Frame\C_Pool\PoolExtensions.cs.meta | Scripts\Tools\CodingTools\Pool\Core\PoolExtensions.cs.meta |
| Scripts\Frame\C_Pool\PoolManager.cs | Scripts\Tools\CodingTools\Pool\Core\PoolManager.cs |
| Scripts\Frame\C_Pool\PoolManager.cs.meta | Scripts\Tools\CodingTools\Pool\Core\PoolManager.cs.meta |
| Scripts\Frame\D_EventCenter\Mono.meta | Scripts\Tools\CodingTools\EventCenter\Mono.meta |
| Scripts\Frame\D_EventCenter\Mono\MmGameEvents.cs | Scripts\Tools\CodingTools\EventCenter\Mono\MmGameEvents.cs |
| Scripts\Frame\D_EventCenter\Mono\MmGameEvents.cs.meta | Scripts\Tools\CodingTools\EventCenter\Mono\MmGameEvents.cs.meta |
| Scripts\Frame\D_EventCenter\Mono\MmGlobalEventBus.cs | Scripts\Tools\CodingTools\EventCenter\Mono\MmGlobalEventBus.cs |
| Scripts\Frame\D_EventCenter\Mono\MmGlobalEventBus.cs.meta | Scripts\Tools\CodingTools\EventCenter\Mono\MmGlobalEventBus.cs.meta |
| Scripts\Frame\D_EventCenter\Mono\MmLocalEventBusMono.cs | Scripts\Tools\CodingTools\EventCenter\Mono\MmLocalEventBusMono.cs |
| Scripts\Frame\D_EventCenter\Mono\MmLocalEventBusMono.cs.meta | Scripts\Tools\CodingTools\EventCenter\Mono\MmLocalEventBusMono.cs.meta |
| Scripts\Frame\G_InputSystem\InputBuffer.meta | Scripts\Frame\E_Input\InputBuffer.meta |
| Scripts\Frame\G_InputSystem\InputBuffer\InputBuffer2D.cs | Scripts\Frame\E_Input\InputBuffer\InputBuffer2D.cs |
| Scripts\Frame\G_InputSystem\InputBuffer\InputBuffer2D.cs.meta | Scripts\Frame\E_Input\InputBuffer\InputBuffer2D.cs.meta |
| Scripts\Frame\G_InputSystem\InputBuffer\InputBuffer3D.cs | Scripts\Frame\E_Input\InputBuffer\InputBuffer3D.cs |
| Scripts\Frame\G_InputSystem\InputBuffer\InputBuffer3D.cs.meta | Scripts\Frame\E_Input\InputBuffer\InputBuffer3D.cs.meta |
| Scripts\Frame\G_InputSystem\InputManager.meta | Scripts\Frame\E_Input\InputManager.meta |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.ChangeKey.cs | Scripts\Frame\E_Input\InputManager\InputManager.ChangeKey.cs |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.ChangeKey.cs.meta | Scripts\Frame\E_Input\InputManager\InputManager.ChangeKey.cs.meta |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.cs | Scripts\Frame\E_Input\InputManager\InputManager.cs |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.cs.meta | Scripts\Frame\E_Input\InputManager\InputManager.cs.meta |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.PlayerInput.cs | Scripts\Frame\E_Input\InputManager\InputManager.PlayerInput.cs |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.PlayerInput.cs.meta | Scripts\Frame\E_Input\InputManager\InputManager.PlayerInput.cs.meta |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.UIMap.cs | Scripts\Frame\E_Input\InputManager\InputManager.UIMap.cs |
| Scripts\Frame\G_InputSystem\InputManager\InputManager.UIMap.cs.meta | Scripts\Frame\E_Input\InputManager\InputManager.UIMap.cs.meta |
| Scripts\Frame\G_InputSystem\InputSystemAction.cs | Scripts\Frame\E_Input\InputSystemAction.cs |
| Scripts\Frame\G_InputSystem\InputSystemAction.cs.meta | Scripts\Frame\E_Input\InputSystemAction.cs.meta |
| Scripts\Frame\G_InputSystem\InputSystemAction.inputactions | Scripts\Frame\E_Input\InputSystemAction.inputactions |
| Scripts\Frame\G_InputSystem\InputSystemAction.inputactions.meta | Scripts\Frame\E_Input\InputSystemAction.inputactions.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork.meta | Scripts\Frame\D_UI\MmUIFrameWork.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core.meta | Plugins\Demigiant\DemiLib\Core.meta<br>Scripts\Frame\B_Assets\MmAsset\Runtime\Core.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Core.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Core\Core.meta<br>Scripts\Frame\E_Input\InputInteract\Core.meta<br>Scripts\Frame\J_Save\Core.meta<br>Scripts\Tools\CodingTools\EventCenter\Core.meta<br>Scripts\Tools\CodingTools\Pool\Core.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIBindConfig.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIBindConfig.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIBindConfig.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIBindConfig.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIDataBase.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIDataBase.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIDataBase.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIDataBase.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIWindowBase.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIWindowBase.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Base\UIWindowBase.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Base\UIWindowBase.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core.meta | Plugins\Demigiant\DemiLib\Core.meta<br>Scripts\Frame\B_Assets\MmAsset\Runtime\Core.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Core.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Core\Core.meta<br>Scripts\Frame\E_Input\InputInteract\Core.meta<br>Scripts\Frame\J_Save\Core.meta<br>Scripts\Tools\CodingTools\EventCenter\Core.meta<br>Scripts\Tools\CodingTools\Pool\Core.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\Loader.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\Loader.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\Loader\UILoad.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\Loader\UILoad.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\Loader\UILoad.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\Loader\UILoad.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\UIHub.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\UIHub.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\UIHub.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\UIHub.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\UIStack.cs | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\UIStack.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Core\Core\UIStack.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Core\Core\UIStack.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor.meta | Editor.meta<br>Editor\GraphViewFrame\Editor.meta<br>Plugins\Demigiant\DemiLib\Core\Editor.meta<br>Plugins\Demigiant\DOTween\Editor.meta<br>Plugins\Demigiant\DOTweenPro\Editor.meta<br>Plugins\Sirenix\Odin Inspector\Assets\Editor.meta<br>Plugins\Sirenix\Odin Inspector\Config\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Extension Sample\Editor.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Test Sample\Editor.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Editor.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\DoTweenAnim.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\DoTweenAnim.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextAtlasBaker.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextAtlasBaker.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextAtlasBaker.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextAtlasBaker.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextEditorWindow.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextEditorWindow.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextEditorWindow.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextEditorWindow.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextSetup.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextSetup.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\FloatingText\FloatingTextSetup.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\FloatingText\FloatingTextSetup.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\ItemWheel.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\ItemWheel.meta<br>Scripts\Frame\D_UI\MmUIFrameWork\Runtime\Widgets\ItemWheel.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\ItemWheel\ItemWheelControllerEditor.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\ItemWheel\ItemWheelControllerEditor.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\ItemWheel\ItemWheelControllerEditor.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\ItemWheel\ItemWheelControllerEditor.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\ItemWheel\ItemWheelEditorWindow.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\ItemWheel\ItemWheelEditorWindow.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\ItemWheel\ItemWheelEditorWindow.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\ItemWheel\ItemWheelEditorWindow.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\MieMieUIFrameWork.UI.Editor.asmdef | Scripts\Frame\D_UI\MmUIFrameWork\Editor\MieMieUIFrameWork.UI.Editor.asmdef |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\MieMieUIFrameWork.UI.Editor.asmdef.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\MieMieUIFrameWork.UI.Editor.asmdef.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\PackagePaths.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\PackagePaths.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\PackagePaths.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\PackagePaths.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIGenPath.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIGenPath.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIGenPath\UIGenPathSettings.json | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIGenPath\UIGenPathSettings.json |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIGenPath\UIGenPathSettings.json.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIGenPath\UIGenPathSettings.json.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITemp.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITemp.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITemp.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITemp.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITool.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITool.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITool.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\GenerateUITool.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIGenPathSettings.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIGenPathSettings.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIGenPathSettings.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIGenPathSettings.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIHierarchyBindDrawer.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIHierarchyBindDrawer.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIHierarchyBindDrawer.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIHierarchyBindDrawer.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIToolLocator.cs | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIToolLocator.cs |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIToolLocator.cs.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIScripts\UIToolLocator.cs.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIToolkits.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIToolkits.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uss | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uss |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uss.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uss.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uxml | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uxml |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uxml.meta | Scripts\Frame\D_UI\MmUIFrameWork\Editor\UIForEditor\UIToolkits\GenerateUITemp.uxml.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\MieMieUIFrameWork.UI.asmdef | Scripts\Frame\D_UI\MmUIFrameWork\MieMieUIFrameWork.UI.asmdef |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\MieMieUIFrameWork.UI.asmdef.meta | Scripts\Frame\D_UI\MmUIFrameWork\MieMieUIFrameWork.UI.asmdef.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts.meta | ADefaultRes\Arts.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt.meta | ADefaultRes\Arts\CrosshairArt.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\Crosshair_Sprite.png | ADefaultRes\Arts\CrosshairArt\Crosshair_Sprite.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\Crosshair_Sprite.png.meta | ADefaultRes\Arts\CrosshairArt\Crosshair_Sprite.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\CrosshairNoPoint.png | ADefaultRes\Arts\CrosshairArt\CrosshairNoPoint.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\CrosshairNoPoint.png.meta | ADefaultRes\Arts\CrosshairArt\CrosshairNoPoint.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\PointCrosshair.png | ADefaultRes\Arts\CrosshairArt\PointCrosshair.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\PointCrosshair.png.meta | ADefaultRes\Arts\CrosshairArt\PointCrosshair.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\XCrosshair.png | ADefaultRes\Arts\CrosshairArt\XCrosshair.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\CrosshairArt\XCrosshair.png.meta | ADefaultRes\Arts\CrosshairArt\XCrosshair.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_Capsule.png | ADefaultRes\Arts\StandImageArt\Stand_Capsule.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_Capsule.png.meta | ADefaultRes\Arts\StandImageArt\Stand_Capsule.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_Circle.png | ADefaultRes\Arts\StandImageArt\Stand_Circle.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_Circle.png.meta | ADefaultRes\Arts\StandImageArt\Stand_Circle.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_GridCell_Wire.png | ADefaultRes\Arts\StandImageArt\Stand_GridCell_Wire.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_GridCell_Wire.png.meta | ADefaultRes\Arts\StandImageArt\Stand_GridCell_Wire.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_HandleKnob.png | ADefaultRes\Arts\StandImageArt\Stand_HandleKnob.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_HandleKnob.png.meta | ADefaultRes\Arts\StandImageArt\Stand_HandleKnob.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_RoundPanel.png | ADefaultRes\Arts\StandImageArt\Stand_RoundPanel.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_RoundPanel.png.meta | ADefaultRes\Arts\StandImageArt\Stand_RoundPanel.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_RoundPanel_Wire.png | ADefaultRes\Arts\StandImageArt\Stand_RoundPanel_Wire.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_RoundPanel_Wire.png.meta | ADefaultRes\Arts\StandImageArt\Stand_RoundPanel_Wire.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_SharpPanel.png | ADefaultRes\Arts\StandImageArt\Stand_SharpPanel.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StandArt\Stand_SharpPanel.png.meta | ADefaultRes\Arts\StandImageArt\Stand_SharpPanel.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Heavy.png | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Heavy.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Heavy.png.meta | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Heavy.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Medium.png | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Medium.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Medium.png.meta | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Medium.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Thick.png | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Thick.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Thick.png.meta | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Thick.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Thin.png | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Thin.png |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Arts\StatusBarArt\Stand_Ring_Thin.png.meta | ADefaultRes\Arts\HollowCircleArt\Stand_Ring_Thin.png.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components.meta | ADefaultRes\Components.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Button.meta | ADefaultRes\Components\Button.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Button\StandButton.prefab | ADefaultRes\Components\Button\StandButton.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Button\StandButton.prefab.meta | ADefaultRes\Components\Button\StandButton.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Button\StandButton_Sharp.prefab | ADefaultRes\Components\Button\StandButton_Sharp.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Button\StandButton_Sharp.prefab.meta | ADefaultRes\Components\Button\StandButton_Sharp.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Crosshair.meta | ADefaultRes\Components\Crosshair.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Crosshair\Crosshair_Prefab.prefab | ADefaultRes\Components\Crosshair\Crosshair_Prefab.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Crosshair\Crosshair_Prefab.prefab.meta | ADefaultRes\Components\Crosshair\Crosshair_Prefab.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image.meta | ADefaultRes\Components\Image.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Test Sample\TestRes\Image.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\FillImage.prefab | ADefaultRes\Components\Image\FillImage.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\FillImage.prefab.meta | ADefaultRes\Components\Image\FillImage.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\StandImage.prefab | ADefaultRes\Components\Image\StandImage.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\StandImage.prefab.meta | ADefaultRes\Components\Image\StandImage.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\StandImage_Sharp.prefab | ADefaultRes\Components\Image\StandImage_Sharp.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Image\StandImage_Sharp.prefab.meta | ADefaultRes\Components\Image\StandImage_Sharp.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Panel.meta | ADefaultRes\Components\Panel.meta<br>Scripts\Frame\B_Assets\Yooasset\Samples~\Test Sample\TestRes\Panel.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Panel\StandPanel.prefab | ADefaultRes\Components\Panel\StandPanel.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Panel\StandPanel.prefab.meta | ADefaultRes\Components\Panel\StandPanel.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Slider.meta | ADefaultRes\Components\Slider.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Slider\StandSlider.prefab | ADefaultRes\Components\Slider\StandSlider.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Slider\StandSlider.prefab.meta | ADefaultRes\Components\Slider\StandSlider.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Slider\StandSlider_Sharp.prefab | ADefaultRes\Components\Slider\StandSlider_Sharp.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Slider\StandSlider_Sharp.prefab.meta | ADefaultRes\Components\Slider\StandSlider_Sharp.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\StatusBar.meta | ADefaultRes\Components\StatusBar.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\StatusBar\StatusBar.prefab | ADefaultRes\Components\StatusBar\StatusBar.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\StatusBar\StatusBar.prefab.meta | ADefaultRes\Components\StatusBar\StatusBar.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Tmp.meta | ADefaultRes\Components\Tmp.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Tmp\StandTmpText.prefab | ADefaultRes\Components\Tmp\StandTmpText.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Tmp\StandTmpText.prefab.meta | ADefaultRes\Components\Tmp\StandTmpText.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Tmp\StandTmpText_Sharp.prefab | ADefaultRes\Components\Tmp\StandTmpText_Sharp.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Tmp\StandTmpText_Sharp.prefab.meta | ADefaultRes\Components\Tmp\StandTmpText_Sharp.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Toggle.meta | ADefaultRes\Components\Toggle.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Toggle\StandToggle.prefab | ADefaultRes\Components\Toggle\StandToggle.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Toggle\StandToggle.prefab.meta | ADefaultRes\Components\Toggle\StandToggle.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Toggle\StandToggle_Sharp.prefab | ADefaultRes\Components\Toggle\StandToggle_Sharp.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Components\Toggle\StandToggle_Sharp.prefab.meta | ADefaultRes\Components\Toggle\StandToggle_Sharp.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Frame.meta | Scripts\Frame.meta<br>Scripts\Frame\A_Frame\Frame.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Frame\UIRoot.prefab | ADefaultRes\FramePrefabs\UIPrefabs\UIRoot.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Frame\UIRoot.prefab.meta | ADefaultRes\FramePrefabs\UIPrefabs\UIRoot.prefab.meta |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Frame\UITemple.prefab | ADefaultRes\FramePrefabs\UIPrefabs\UITemple.prefab |
| Scripts\Frame\H_UIFramework\MmUIFrameWork\StandUIPrefabs\Frame\UITemple.prefab.meta | ADefaultRes\FramePrefabs\UIPrefabs\UITemple.prefab.meta |
| Scripts\Tools\AnimationExtension.meta | Scripts\Tools\Display\AnimationExtension.meta |
| Scripts\Tools\AnimationExtension\3D\AniamtorExtension.cs | Scripts\Tools\Display\AnimationExtension\Animator\AniamtorExtension.cs |
| Scripts\Tools\AnimationExtension\3D\AniamtorExtension.cs.meta | Scripts\Tools\Display\AnimationExtension\Animator\AniamtorExtension.cs.meta |
| Scripts\Tools\AnimationExtension\3D\AnimationEventStateBehaviour.cs | Scripts\Tools\Display\AnimationExtension\Animator\AnimationEventStateBehaviour.cs |
| Scripts\Tools\AnimationExtension\3D\AnimationEventStateBehaviour.cs.meta | Scripts\Tools\Display\AnimationExtension\Animator\AnimationEventStateBehaviour.cs.meta |
| Scripts\Tools\AnimationExtension\3D\AnimationReceiver.cs | Scripts\Tools\Display\AnimationExtension\Animator\AnimationReceiver.cs |
| Scripts\Tools\AnimationExtension\3D\AnimationReceiver.cs.meta | Scripts\Tools\Display\AnimationExtension\Animator\AnimationReceiver.cs.meta |
| Scripts\Tools\ColorToos\ColorTools.cs | Scripts\Tools\Display\Color\ColorTools.cs |
| Scripts\Tools\ColorToos\ColorTools.cs.meta | Scripts\Tools\Display\Color\ColorTools.cs.meta |
| Scripts\Tools\PerformanceTrace.cs | Scripts\Tools\Performance\TraceTimems\PerformanceTrace.cs |
| Scripts\Tools\PerformanceTrace.cs.meta | Scripts\Tools\Performance\TraceTimems\PerformanceTrace.cs.meta |
| Scripts\Tools\Physic&Math.meta | Scripts\Tools\CodingTools\Physic&Math.meta |
| Scripts\Tools\Physic&Math\MathExtensions.cs | Scripts\Tools\CodingTools\Physic&Math\MathExtensions.cs |
| Scripts\Tools\Physic&Math\MathExtensions.cs.meta | Scripts\Tools\CodingTools\Physic&Math\MathExtensions.cs.meta |
| Scripts\Tools\Physic&Math\PhysicRayCast.cs | Scripts\Tools\CodingTools\Physic&Math\PhysicRayCast.cs |
| Scripts\Tools\Physic&Math\PhysicRayCast.cs.meta | Scripts\Tools\CodingTools\Physic&Math\PhysicRayCast.cs.meta |
| Scripts\Tools\SceneCtrl.meta | Scripts\Tools\CodingTools\SceneCtrl.meta |
| Scripts\Tools\SceneCtrl\SceneCtrl.cs | Scripts\Tools\CodingTools\SceneCtrl\SceneCtrl.cs |
| Scripts\Tools\SceneCtrl\SceneCtrl.cs.meta | Scripts\Tools\CodingTools\SceneCtrl\SceneCtrl.cs.meta |
| Scripts\Tools\UniTask.meta | Scripts\Frame\B_Assets\Yooasset\Samples~\UniTask Sample\UniTask.meta<br>Scripts\Tools\CodingTools\UniTask.meta |
| Scripts\Tools\UniTask\UniTimerManager.cs | Scripts\Tools\CodingTools\UniTask\UniTimerManager.cs |
| Scripts\Tools\UniTask\UniTimerManager.cs.meta | Scripts\Tools\CodingTools\UniTask\UniTimerManager.cs.meta |
| Scripts\Tools\UnityInterEvents.meta | Scripts\Tools\CodingTools\UnityInterEvents.meta |
| Scripts\Tools\UnityInterEvents\SimpleEventExtensions.cs | Scripts\Tools\CodingTools\UnityInterEvents\SimpleEventExtensions.cs |
| Scripts\Tools\UnityInterEvents\SimpleEventExtensions.cs.meta | Scripts\Tools\CodingTools\UnityInterEvents\SimpleEventExtensions.cs.meta |
| Scripts\Tools\UnityInterEvents\SimpleEventListener.cs | Scripts\Tools\CodingTools\UnityInterEvents\SimpleEventListener.cs |
| Scripts\Tools\UnityInterEvents\SimpleEventListener.cs.meta | Scripts\Tools\CodingTools\UnityInterEvents\SimpleEventListener.cs.meta |

## 五 同名但内容不同(方向需人工判断, mtime 已被 reset 打平不可信)

| 相对路径 | 源侧字节 | 本侧字节 |
| --- | --- | --- |
| Editor\AnimationForEditor\FbxAnimationClipRenameExtractWindow.cs | 12184 | 12598 |
| Editor\AsmdefTool\AsmdefToolWindow.cs | 25909 | 26471 |
| Editor\DmvcForEditor\DMVCCodeGeneratorWindow.cs | 16650 | 16733 |
| Editor\DmvcForEditor\DMVCExecutionOrderWindow.cs | 16003 | 16085 |
| Editor\DmvcForEditor\DmvcLibraryChecker.cs | 2397 | 2397 |
| Editor\EventBusForEditor\EventBusEditorWindow.cs | 15869 | 15962 |
| Editor\FolderForEditor\ProjectWindow\FolderBookmarkWindow.cs | 18488 | 18567 |
| Editor\FolderForEditor\TopWindow\CheckFolder.cs | 8021 | 6858 |
| Editor\FsmForEditor\ChinedFSMSqueueWindow.cs | 19002 | 19079 |
| Editor\FsmForEditor\FsmLibraryChecker.cs | 1643 | 1270 |
| Editor\FsmForEditor\TickFSMWindow.cs | 17731 | 17816 |
| Editor\MieMieFrameWork.Editor.asmdef | 585 | 562 |
| Editor\MmAssetForEditor\Configuration\BuildBundleConfigura.cs | 2394 | 2841 |
| Editor\MmAssetForEditor\MmAssetPaths.cs | 1971 | 2225 |
| Editor\MmAssetForEditor\RootMenu\BuildBundleWindow.cs | 3849 | 7205 |
| Editor\MmAssetForEditor\RootWindow\BuildWindow.cs | 2880 | 3028 |
| Editor\MmAssetForEditor\Tools\BundleTools.cs | 4966 | 4961 |
| Editor\MmAssetForEditor\Tools\MmAssetDiagnostics.cs | 9537 | 9474 |
| Editor\MmAssetsTopWindow\MmAssetsTopWindow.cs | 5992 | 2869 |
| Editor\MmAssetsTopWindow\MmEditorPaths.cs | 1877 | 1193 |
| Editor\MmAssetsTopWindow\MmModuleCatalog.json | 26382 | 18215 |
| Editor\MmAssetsTopWindow\MmModuleCatalogData.cs | 854 | 658 |
| Editor\MmAssetsTopWindow\MmModuleCatalogStore.cs | 11274 | 5086 |
| Editor\MmAssetsTopWindow\MmModuleDetailPanel.cs | 6784 | 2446 |
| Editor\PoolWindow\PoolEditorWindow.cs | 18534 | 17261 |
| Editor\ToolsCenter.meta | 172 | 177 |
| Plugins\Demigiant.meta | 293 | 172 |
| Plugins\Demigiant\DemiLib.meta | 260 | 172 |
| Plugins\Demigiant\DemiLib\Core.meta | 107 | 172 |
| Plugins\Demigiant\DemiLib\Core\Editor.meta | 107 | 172 |
| Plugins\Demigiant\DOTween.meta | 260 | 172 |
| Plugins\Demigiant\DOTween\Editor.meta | 107 | 172 |
| Plugins\Demigiant\DOTweenPro.meta | 293 | 172 |
| Plugins\Demigiant\DOTweenPro\Editor.meta | 107 | 172 |
| Scripts\Frame\F_Audio.meta | 172 | 180 |
| Scripts\Frame\F_Audio\AudioManager.cs | 9990 | 10230 |
| Scripts\Frame\F_Audio\AudioManager.Sfx.cs | 6728 | 6708 |
| Scripts\MieMieFrameWork.Runtime.asmdef | 566 | 669 |

## 六 补充说明

- 真缺失里另有 245 个 .meta 伴随文件, 未计入上面两张表。
- 源侧无 .git, 没有版本信息, 无法用提交时间判断新旧; 同名文件 265/303 字节完全相同, 说明同源。
- 本侧 Demigiant 目录只剩 .dll.mdb 调试符号(被 .gitignore 忽略), 6 个 .dll 已随 10.2 回退移除; 本侧代码 0 处引用 DG.Tweening, 不影响编译, 如需 DOTween 可从源侧复制。


