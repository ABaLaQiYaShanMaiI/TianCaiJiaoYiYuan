extends Node
## 移植版快捷操作（自动加载，不修改 C# 逻辑，只复用游戏已有的按钮）
##   F1  显示快捷键帮助
##   F5  快速保存（手动槽 1；若开过存档面板则用当前面板）
##   F9  快速读档（手动槽 1）
##   F11 全屏/窗口切换
##   F3  显示/隐藏性能浮层（FPS / 帧时）
## 说明：输入框获得焦点时自动让行，避免打字误触。

var _help_layer: CanvasLayer
var _help_label: Label
var _help_timer: Timer
var _hud: CanvasLayer
var _hud_label: Label
var _busy := false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey):
		return
	var k := event as InputEventKey
	if not k.pressed or k.echo:
		return
	# 正在输入框里打字时，不抢键
	var focus := get_viewport().gui_get_focus_owner()
	if focus is LineEdit or focus is TextEdit:
		return
	match k.keycode:
		KEY_F1:
			show_help()
			get_viewport().set_input_as_handled()
		KEY_F5:
			quick_save()
			get_viewport().set_input_as_handled()
		KEY_F9:
			quick_load()
			get_viewport().set_input_as_handled()
		KEY_F11:
			toggle_fullscreen()
			get_viewport().set_input_as_handled()
		KEY_F3:
			toggle_hud()
			get_viewport().set_input_as_handled()

# ---------------- 基础工具 ----------------
func _game_root() -> Node:
	# 正常运行时 current_scene 就是游戏根；用名字兜底（机器人/测试场景包裹时也能用）
	var r := get_tree().root.find_child("RetroGame", true, false)
	if r != null:
		return r
	return get_tree().current_scene

func _find(name: String) -> Node:
	var r := _game_root()
	if r == null:
		return null
	var n := r.find_child(name, true, false)
	if n != null:
		return n
	return get_tree().root.find_child(name, true, false)

func _press(name: String) -> bool:
	var b := _find(name) as Button
	if b == null:
		return false
	b.emit_signal("pressed")
	return true

func _press_text(prefix: String) -> bool:
	var r := _game_root()
	if r == null:
		return false
	for n in r.find_children("*", "Button", true, false):
		if str(n.text).begins_with(prefix):
			n.emit_signal("pressed")
			return true
	return false

func _wait(seconds: float) -> void:
	await get_tree().create_timer(seconds).timeout

func _notify_back() -> void:
	var r := _game_root()
	if r != null:
		r.notification(1007)

## 反复按“返回”，先关掉所有弹窗，直到暂停菜单出现
func _open_pause_menu() -> bool:
	for i in 4:
		if _find("PauseSave") != null:
			return true
		_notify_back()
		await _wait(0.2)
	return _find("PauseSave") != null

# ---------------- 快速保存 ----------------
func quick_save() -> void:
	if _busy:
		return
	_busy = true
	toast("快速保存…")
	# 先关弹窗并打开暂停菜单（等价 Esc / 返回键）
	if not await _open_pause_menu():
		toast("快速保存：当前界面无法保存（请先进入游戏）")
		_busy = false
		return
	if not _press("PauseSave"):
		toast("快速保存：未找到「保存游戏」")
		_busy = false
		return
	await _wait(0.45)
	if not _press("Slotmanual-1"):
		toast("快速保存：未找到手动槽 1")
		_busy = false
		return
	await _wait(0.3)
	_press_text("确认")   # 覆盖确认（空槽时不存在）
	toast("快速保存：已写入 手动槽 1")
	_busy = false

# ---------------- 快速读档 ----------------
func quick_load() -> void:
	if _busy:
		return
	_busy = true
	toast("快速读档…")
	# 标题页直接点「读取存档」；游戏中先开暂停菜单再点「读取存档」
	var opened := false
	if _find("FrontLoad") != null:
		opened = _press("FrontLoad")
	elif await _open_pause_menu():
		opened = _press("PauseLoad")
	if not opened:
		toast("快速读档：当前界面无法读档")
		_busy = false
		return
	await _wait(0.5)
	if not _press("Slotmanual-1"):
		toast("快速读档：未找到手动槽 1（或该槽为空）")
		_busy = false
		return
	await _wait(0.3)
	_press_text("确认")
	toast("快速读档：已读取 手动槽 1")
	_busy = false

# ---------------- 全屏 ----------------
func toggle_fullscreen() -> void:
	var mode := DisplayServer.window_get_mode()
	if mode == DisplayServer.WINDOW_MODE_FULLSCREEN:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		toast("窗口模式")
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		toast("全屏模式（F11 切回）")

# ---------------- 帮助 / 提示 ----------------
func toast(text: String, seconds := 2.0) -> void:
	_ensure_help_layer()
	_help_label.text = text
	_help_label.visible = true
	if _help_timer != null:
		_help_timer.stop()
	_help_timer = Timer.new()
	_help_timer.one_shot = true
	_help_timer.wait_time = seconds
	_help_timer.timeout.connect(func(): _help_label.visible = false)
	add_child(_help_timer)
	_help_timer.start()

func show_help() -> void:
	toast("F5 快速保存   F9 快速读档   F11 全屏   F3 性能浮层   Esc 暂停菜单", 4.0)

func _ensure_help_layer() -> void:
	if _help_layer != null and is_instance_valid(_help_layer):
		return
	_help_layer = CanvasLayer.new()
	_help_layer.layer = 128
	add_child(_help_layer)
	_help_label = Label.new()
	_help_label.position = Vector2(12, 12)
	_help_label.add_theme_font_size_override("font_size", 14)
	_help_label.add_theme_color_override("font_color", Color(1, 1, 1))
	_help_label.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	_help_label.add_theme_constant_override("outline_size", 4)
	_help_label.visible = false
	_help_layer.add_child(_help_label)

# ---------------- 性能浮层 ----------------
func toggle_hud() -> void:
	if _hud != null and is_instance_valid(_hud):
		_hud.queue_free()
		_hud = null
		return
	_hud = CanvasLayer.new()
	_hud.layer = 127
	add_child(_hud)
	_hud_label = Label.new()
	_hud_label.position = Vector2(12, 34)
	_hud_label.add_theme_font_size_override("font_size", 13)
	_hud_label.add_theme_color_override("font_color", Color(0.7, 1, 0.7))
	_hud_label.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	_hud_label.add_theme_constant_override("outline_size", 4)
	_hud.add_child(_hud_label)

func _process(delta: float) -> void:
	if _hud_label == null or not is_instance_valid(_hud_label):
		return
	var fps := 1.0 / maxf(delta, 0.0001)
	var objs := int(Performance.get_monitor(Performance.OBJECT_COUNT))
	var draw := int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME))
	_hud_label.text = "FPS %.0f | 帧时 %.1f ms | 对象 %d | drawcall %d" % [fps, delta * 1000.0, objs, draw]
