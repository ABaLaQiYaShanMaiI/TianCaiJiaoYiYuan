extends Node

var game: Node

func _ready() -> void:
	var mode := OS.get_environment("BOT_MODE")
	if mode == "":
		mode = "save"
	print("BOT: mode=", mode)
	var ps: PackedScene = load("res://ui/scenes/retro-main.scn")
	game = ps.instantiate()
	add_child(game)
	# 等待 C# 控制器异步完成 _Ready / 启动动画 / 首页构建（最多 ~30 秒）
	for i in 1800:
		await get_tree().process_frame
		if game.find_child("FrontNew", true, false) != null:
			break
	print("BOT: scene up, sessionStarted=", game.get("_sessionStarted"), " FrontNew=", game.find_child("FrontNew", true, false) != null)

	if mode == "save":
		await _save_flow()
	elif mode == "continue":
		var ok := await _start_session_from_front()
		print("BOT: continue result sessionStarted=", ok)
		_dump_dialog()
		_dir_list()
	elif mode == "quit":
		await _new_game()
		await _frames(60)
		print("BOT: 触发关窗通知 1006（等价点 X）")
		game.notification(1006)
		await _frames(150)
		print("BOT: 关窗流程结束 busy=", game.get("_persistenceBusy"), " quitRequested=", game.get("_quitRequested"))
		_dir_list()
	elif mode == "overwrite":
		await _overwrite_flow()
	elif mode == "resave":
		await _press("FrontLoad")
		await _frames(90)
		print("BOT: slots = ", _find("Slotmanual-1") != null)
		await _press("Slotmanual-1")
		await _frames(240)
		print("BOT: loaded, sessionStarted=", game.get("_sessionStarted"), " dialog=", game.get("_dialog") != null)
		for attempt in 6:
			if game.get("_dialog") == null:
				break
			game.notification(1007)
			await _frames(20)
		if not await _open_pause():
			print("BOT: [FAIL] 暂停菜单未打开")
			return
		await _press("PauseSave")
		await _frames(90)
		print("BOT: save dialog slot2 = ", _find("Slotmanual-2") != null)
		await _press("Slotmanual-2")
		await _frames(40)
		if _find("Slotmanual-2") != null and game.get("_dialog") != null:
			await _press_text("确认")
		await _frames(240)
		print("BOT: resave done busy=", game.get("_persistenceBusy"))
		_dump_dialog()
		_dir_list()
	elif mode == "perf":
		await _new_game()
		print("BOT: perf 采样开始（静置 25 秒）")
		await _frames(60 * 25)
		print("BOT: perf 采样结束")
		_dir_list()
	elif mode == "keys":
		await _new_game()
		await _frames(60)
		var qk = get_node_or_null("/root/PortQuickKeys")
		print("QK: autoload=", qk != null)
		game.notification(1007)
		await _frames(40)
		print("QK: pause open=", _find("PauseSave") != null)
		qk.quick_save()
		await _frames(240)
		print("QK: after quick_save")
		_dir_list()
		game.notification(1007)
		await _frames(40)
		qk.quick_load()
		await _frames(300)
		print("QK: after quick_load, session=", game.get("_sessionStarted"), " dialog=", game.get("_dialog") != null)
	elif mode == "loadpause":
		await _loadpause_flow()
	else:
		await _load_flow()

	print("BOT: done")
	await get_tree().process_frame
	get_tree().quit()

func _frames(n: int) -> void:
	for i in n:
		await get_tree().process_frame
		if i % 30 == 29:
			print("BOT: ..frame ", i + 1)

func _find(name: String) -> Node:
	var n := game.find_child(name, true, false)
	if n == null:
		n = get_tree().root.find_child(name, true, false)
	return n

func _press(name: String) -> bool:
	var b := _find(name) as Button
	if b == null:
		print("BOT: [MISS] button ", name)
		return false
	print("BOT: press ", name)
	b.emit_signal("pressed")
	return true

func _set_text(name: String, value: String) -> void:
	var e := _find(name) as LineEdit
	if e == null:
		print("BOT: [MISS] edit ", name)
		return
	e.text = value
	e.emit_signal("text_changed", value)
	print("BOT: set ", name, " = ", value)

func _new_game() -> void:
	await _press("FrontNew")
	await _frames(15)
	_set_text("CampaignName", "测试公司")
	_set_text("CampaignSeed", "123456789")
	_set_text("HeroName", "测试主角")
	await _frames(5)
	for i in 6:
		var sb := _find("HeroAbility%d" % i) as SpinBox
		if sb != null:
			sb.value = 300.0
	await _frames(5)
	await _press("CampaignStart")
	await _frames(180)

func _save_flow() -> void:
	await _press("FrontNew")
	await _frames(15)
	_set_text("CampaignName", "测试公司")
	_set_text("CampaignSeed", "123456789")
	_set_text("HeroName", "测试主角")
	await _frames(5)
	# 顺手用超上限数值，验证解锁补丁
	for i in 6:
		var sb := _find("HeroAbility%d" % i) as SpinBox
		if sb != null:
			sb.value = 300.0
	await _frames(5)
	await _press("CampaignStart")
	await _frames(180)
	print("BOT: after CampaignStart, sessionStarted=", game.get("_sessionStarted"), " dialog=", game.get("_dialog") != null)

	# 关闭开局剧情等弹窗，直到没有 dialog
	for attempt in 6:
		if game.get("_dialog") == null:
			break
		var dlg = game.get("_dialog")
		print("BOT: close dialog #", attempt, " -> ", dlg)
		game.notification(1007)
		await _frames(20)

	game.notification(1007)
	await _frames(30)
	print("BOT: pause buttons present: ", _find("PauseSave") != null)
	await _press("PauseSave")
	await _frames(90)
	var slots := []
	for key in ["Slotmanual-1", "Slotmanual-2", "Slotmanual-3"]:
		if _find(key) != null:
			slots.append(key)
	print("BOT: save dialog slots = ", slots)
	if slots.is_empty():
		print("BOT: [FAIL] 存档面板没打开")
		_dump_dialog()
		return
	await _press("Slotmanual-1")
	await _frames(180)
	print("BOT: save finished, persistenceBusy=", game.get("_persistenceBusy"))
	_dir_list()

func _press_text(text: String) -> bool:
	var dlg = game.get("_dialog")
	if dlg == null:
		print("BOT: [MISS] no dialog for text ", text)
		return false
	for c in dlg.find_children("*", "Button", true, false):
		if c.text.contains(text):
			print("BOT: press dialog button '", c.text, "'")
			c.emit_signal("pressed")
			return true
	print("BOT: [MISS] dialog button ", text)
	return false

func _start_session_from_front() -> bool:
	var cont := _find("FrontContinue") as Button
	if cont == null:
		print("BOT: [MISS] FrontContinue")
		return false
	print("BOT: press FrontContinue")
	cont.emit_signal("pressed")
	await _frames(180)
	print("BOT: after continue, sessionStarted=", game.get("_sessionStarted"), " dialog=", game.get("_dialog") != null)
	for attempt in 6:
		if game.get("_dialog") == null:
			break
		game.notification(1007)
		await _frames(20)
	return game.get("_sessionStarted")

func _open_pause() -> bool:
	game.notification(1007)
	await _frames(40)
	return _find("PauseSave") != null

func _overwrite_flow() -> void:
	await _start_session_from_front()
	if not await _open_pause():
		print("BOT: [FAIL] 暂停菜单未打开")
		return
	await _press("PauseSave")
	await _frames(90)
	print("BOT: slots = ", _find("Slotmanual-1") != null)
	await _press("Slotmanual-1")
	await _frames(30)
	var ok := await _press_text("确认")
	print("BOT: confirm pressed = ", ok)
	await _frames(180)
	print("BOT: overwrite save done, busy=", game.get("_persistenceBusy"))
	_dir_list()

func _loadpause_flow() -> void:
	await _start_session_from_front()
	if not await _open_pause():
		print("BOT: [FAIL] 暂停菜单未打开")
		return
	await _press("PauseLoad")
	await _frames(90)
	print("BOT: load slots = ", _find("Slotmanual-1") != null)
	await _press("Slotmanual-1")
	await _frames(30)
	var ok := await _press_text("确认")
	print("BOT: confirm pressed = ", ok)
	await _frames(240)
	print("BOT: in-session load done, busy=", game.get("_persistenceBusy"), " sessionStarted=", game.get("_sessionStarted"))

func _dump_dialog() -> void:
	var dlg = game.get("_dialog")
	if dlg == null:
		print("BOT: no dialog")
		return
	for c in dlg.find_children("*", "Button", true, false):
		print("BOT: dialog button -> ", c.name, " / ", c.text)

func _dir_list() -> void:
	var base := ProjectSettings.globalize_path("user://")
	print("BOT: user:// = ", base)
	var saves := base.path_join("saves")
	print("BOT: saves exists = ", DirAccess.dir_exists_absolute(saves))
	var da := DirAccess.open(saves)
	if da == null:
		print("BOT: [WARN] 打不开存档目录: ", saves)
		return
	for f in da.get_files():
		print("BOT:   file ", f, " (", FileAccess.get_file_as_bytes(saves.path_join(f)).size(), " bytes)")
	for d in da.get_directories():
		print("BOT:   dir  ", d)

func _load_flow() -> void:
	await _press("FrontLoad")
	await _frames(90)
	var slots := []
	for key in ["Slotmanual-1", "Slotmanual-2", "Slotmanual-3"]:
		if _find(key) != null:
			slots.append(key)
	print("BOT: load dialog slots = ", slots)
	if slots.is_empty():
		print("BOT: [FAIL] 读档面板没打开")
		return
	await _press("Slotmanual-1")
	await _frames(240)
	print("BOT: load finished, sessionStarted=", game.get("_sessionStarted"), " busy=", game.get("_persistenceBusy"))
