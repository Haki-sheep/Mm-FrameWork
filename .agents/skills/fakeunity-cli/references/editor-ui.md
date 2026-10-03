# 在线 UGUI 观察与目标点击

仅用于运行中 Editor 的 UGUI。资产布局创作读取 [ugui-and-tmp](ugui-and-tmp.md)，
一般 Editor/截图/测试流程读取 [editor-online-roslyn](editor-online-roslyn.md)。不引入 Player 网络组件。

先 status 确认 ugui_automation；查询入口为 editor ui-snapshot/find/raycast/metrics，结果回执为 ui-result。
使用 --values-file JSON、--json 和明确项目。按名称/路径缩小扫描，检查限制、截断、来源和身份；
不把相同名称当唯一目标，不用旧会话的 instance ID。

snapshot 返回 RectTransform 几何、Canvas/CanvasGroup、Selectable 状态及目标身份；
active、alpha、几何和射线都不单独证明最终像素可见。最终画面问题要配合 GameView 合成截图。
DontDestroyOnLoad 对象使用当前 session + instance ID + scene/path 组合，不用普通 Scene 枚举结果推断不存在。

## 坐标与命中

point 为 Unity 屏幕像素或 normalized，原点在左下角；明确 coordinate_space。
点击必须带当前 screen_size，旧尺寸、非有限值、越界会拒绝。不要把图片左上坐标或 Simulator safe-area 坐标直接混用。
metrics 返回 raw Screen/Device/GameView、CanvasScaler、Canvas camera rect 和可选目标父链，不应用项目适配策略。
项目的 safe-area 选择、mixed/stale 判定和布局预期由项目 Provider/Docs 负责，不复制算法进 Core。

Overlay 使用相机为空的 UGUI 路径；Camera Canvas 必须有有效 raycaster/camera。
多个活动 EventSystem、非目标 display、RenderTexture camera 等未支持组合不能靠强制点击兜底。
raycast 使用 EventSystem 原始排序；点击要求 top hit 是目标或其合法子 Graphic，且真正的 click/down handler 对应目标。
禁用、不可交互、透明、遮挡、无命中或歧义时拒绝，不 fallback 到请求对象，不直接调用 Button.onClick。

## 点击与证据

editor ui-click 只允许稳定、非暂停 Play Mode，并经过 preservation guard。
先 --dry-run 验证零事件预览，再按授权 --yes；返回的 eligible 不是永久资格，派发阶段会重新核对模式、EventSystem 与目标。
事件级模拟依次发 pointerDown/up/click，不等于操作系统物理输入或真机触控。

prepared/completed/failed receipt 记录 click_dispatch_attempted、click_dispatched、callback_errors 与 outcome。
回调异常可能已经改变业务状态，因此是 outcome_unknown，不能因为外层失败而再点一次。
只查询原 request/operation ID，依据项目 View、selection revision、Verified Frame 等业务状态决定后续操作。
通用点击的 business_postcondition 未评估，不可据此声称登录/领奖/选择成功；没有通用 byte undo。

drag、long-press、UIToolkit、GIF 与 Runtime 网络不属于这一期合同。只读 UI 与只读 Inspector 可以在 guard 保护期查询；
raw Roslyn 和所有 Template 不因此获得保护期执行资格。
