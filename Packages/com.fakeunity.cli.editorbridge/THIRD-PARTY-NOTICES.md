# Third-Party Notices

## AIBridge UGUI API Adaptation

- Repository: https://github.com/liyingsong99/AIBridge
- Tag/commit: 1.5.9 / b2bee3e8bf2c25dfc8e99642f53219ea66d831f1
- Sources: Editor/Commands/InputCommand.cs; Runtime/AIBridgeRuntime.UiAutomation.cs.
- Destinations: Editor/EditorUiOperations.cs, Editor/EditorUiMetrics.cs, Editor/EditorUiClick.cs.
- Scope: Unity RectTransform/Canvas/EventSystem discovery, raycast metadata and pointer down/up/click sequence.
- Changes: Editor-only execution through the existing bridge; explicit session/scene/object and screen-size binding,
  no target fallback, exact handler/occlusion checks, bounded snapshots, callback-error evidence and immutable receipts.
- Runtime MonoBehaviour, transports, discovery, authentication model, code execution and binaries are not included.
- The MIT copyright and permission notice below applies to this adaptation.

## AIBridge Prefab API Reference

- Repository: https://github.com/liyingsong99/AIBridge
- Tag/commit: 1.5.9 / b2bee3e8bf2c25dfc8e99642f53219ea66d831f1
- Source: Editor/PrefabEditing/PrefabPatchExecutor.cs.
- Destination: Editor/EditorPrefabOperations.cs.
- Scope: load-once/edit/save/finally-unload Unity Prefab API processing approach.
- Changes: existing FakeUnityCLI component operation vocabulary and typed values, exact asset/hash identity,
  immutable receipts and byte restore, missing-reference validation and synchronous asset callback audit.
- No upstream patch parser, transport, security model, installer or binary is included.
- The MIT copyright and permission notice below also applies to this reference/adaptation.

## AIBridge Inspector API Reference

- Repository: https://github.com/liyingsong99/AIBridge
- Tag: 1.5.9
- Commit: b2bee3e8bf2c25dfc8e99642f53219ea66d831f1
- Sources: Editor/Commands/InspectorCommand_PropertyValues.cs,
  Editor/Commands/InspectorCommand_CurveGradientValues.cs,
  Editor/Commands/InspectorCommand_TargetResolution.cs.
- Destination: Editor/EditorPropertyValues.cs and Editor/EditorObjectTargets.cs.
- Scope: SerializedProperty typed conversion, Gradient API reflection and Prefab target processing approach.
- Changes: independently structured strict JSON values, semantic names, explicit scene/session/object identity,
  no Selection fallback, bounded values, GUID/local-id references and receipt-bound compare-and-restore.
- No upstream queue, permission policy, Runtime transport, installer or binary is included.
- The MIT copyright and permission notice below also applies to this reference/adaptation.

## AIBridge ScreenshotHelper

- Repository: https://github.com/liyingsong99/AIBridge
- Tag: 1.3.2
- Commit: 942cc7e40a09087687f6fe1ff10d4399e6ab2854
- Source: Editor/Utils/ScreenshotHelper.cs
- Destination: Editor/EditorOnlineOperations.cs, CaptureScreenshot and FindGameViewFramebufferField
- Adapted scope: existing GameView RenderTexture discovery and GPU readback/orientation handling.
- Changes: strict window selection, no ScreenCapture fallback, bounded PNG artifacts, exception-safe resource
  restoration, explicit capture scope/freshness, and the existing FakeUnityCLI request/preservation protocol.
- No transport, permission model, runtime service, CLI binary or precompiled dependency was copied.
- This is public 1.3.2 source provenance, not verification of the unavailable historical 1.3.3 ZIP.

MIT License

Copyright (c) 2026 liyingsong

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
