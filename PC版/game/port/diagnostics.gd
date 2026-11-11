extends Node
## 移植版诊断日志（自动加载）
## 作用：
##   1) 启动时写环境自检（引擎版本 / 用户目录 / 存档目录可写性 / 命令行 / .NET 环境）
##   2) 每 0.5 秒扫描存档目录，把"新建/覆盖/删除"的存档动作写进日志
##   3) 日志统一落在 user://logs/ 下，方便打包反馈

const SCAN_INTERVAL := 0.5
const ACTIVITY_LOG := "user://logs/save-activity.log"
const ACTIVITY_MAX_BYTES := 2 * 1024 * 1024

var _timer: Timer
var _known := {}          # path -> Vector2i(size, mtime)
var _first_scan_done := false
var _warned_unwritable := false

func _ready() -> void:
	DirAccess.make_dir_recursive_absolute("user://logs")
	_log_header()
	_timer = Timer.new()
	_timer.wait_time = SCAN_INTERVAL
	_timer.autostart = true
	_timer.timeout.connect(_scan)
	add_child(_timer)
	_scan()

func _log_header() -> void:
	var sb := PackedStringArray()
	sb.append("")
	sb.append("==================== 启动 %s ====================" % Time.get_datetime_string_from_system(false, true))
	sb.append("engine      : %s" % Engine.get_version_info().get("string", "?"))
	sb.append("os          : %s / %s" % [OS.get_name(), OS.get_version()])
	sb.append("user://     : %s" % ProjectSettings.globalize_path("user://"))
	sb.append("exe         : %s" % OS.get_executable_path())
	sb.append("cwd         : %s" % OS.get_environment("CD"))
	sb.append("cmdline     : %s" % OS.get_cmdline_args())
	sb.append("DOTNET_ROLL_FORWARD : %s" % OS.get_environment("DOTNET_ROLL_FORWARD"))
	var saves := ProjectSettings.globalize_path("user://saves")
	sb.append("save dir    : %s (%s)" % [saves, "存在" if DirAccess.dir_exists_absolute(saves) else "不存在"])
	# 可写性自检
	var probe := ProjectSettings.globalize_path("user://logs/.write-probe")
	var f := FileAccess.open(probe, FileAccess.WRITE)
	if f == null:
		sb.append("write test  : [失败] 无法写入日志目录，错误=%s" % error_string(FileAccess.get_open_error()))
	else:
		f.store_string("ok")
		f.close()
		DirAccess.remove_absolute(probe)
		sb.append("write test  : 正常")
	# 现有存档概览
	if DirAccess.dir_exists_absolute(saves):
		var da := DirAccess.open(saves)
		if da != null:
			da.list_dir_begin()
			var n := da.get_next()
			var items := PackedStringArray()
			while n != "":
				if not da.current_is_dir() and n.ends_with(".json"):
					items.append(n)
				n = da.get_next()
			da.list_dir_end()
			sb.append("saves       : %s" % ("、".join(items) if items.size() > 0 else "（空）"))
	_append(ACTIVITY_LOG, "\n".join(sb))

func _scan() -> void:
	var saves := ProjectSettings.globalize_path("user://saves")
	if not DirAccess.dir_exists_absolute(saves):
		if not _warned_unwritable:
			_warned_unwritable = true
			_append(ACTIVITY_LOG, "[%s] 存档目录尚不存在（游戏保存后会自动创建）：%s" % [_ts(), saves])
		return
	var now := {}
	_collect(saves, "", now)
	if not _first_scan_done:
		_first_scan_done = true
		_known = now
		return
	# 新增 / 变化
	for path in now.keys():
		var cur: int = now[path]
		if not _known.has(path):
			_append(ACTIVITY_LOG, "[%s] 新建  %s" % [_ts(), path])
		elif _known[path] != cur:
			_append(ACTIVITY_LOG, "[%s] 更新  %s" % [_ts(), path])
	# 删除
	for path in _known.keys():
		if not now.has(path):
			_append(ACTIVITY_LOG, "[%s] 删除  %s" % [_ts(), path])
	_known = now

func _collect(base: String, rel: String, out: Dictionary) -> void:
	var dir_abs := base.path_join(rel) if rel != "" else base
	var da := DirAccess.open(dir_abs)
	if da == null:
		return
	da.list_dir_begin()
	var n := da.get_next()
	while n != "":
		if n == "." or n == "..":
			n = da.get_next()
			continue
		# 跳过临时文件，避免与游戏写盘冲突
		if n.ends_with(".tmp"):
			n = da.get_next()
			continue
		var rel_child := n if rel == "" else rel.path_join(n)
		var abs_child := dir_abs.path_join(n)
		if da.current_is_dir():
			_collect(base, rel_child, out)
		else:
			# 只用 stat 取修改时间，不打开文件内容（否则会和存档写入抢句柄）
			out[rel_child] = FileAccess.get_modified_time(abs_child)
		n = da.get_next()
	da.list_dir_end()

func _append(path: String, text: String) -> void:
	# 简单轮转
	var abs := ProjectSettings.globalize_path(path)
	var existing := 0
	if FileAccess.file_exists(path):
		existing = FileAccess.get_file_as_bytes(path).size()
	if existing > ACTIVITY_MAX_BYTES:
		var da := DirAccess.open(ProjectSettings.globalize_path("user://logs"))
		if da != null:
			da.rename(ProjectSettings.globalize_path(path).get_file(), "save-activity.1.log")
	var f := FileAccess.open(path, FileAccess.READ_WRITE)
	if f == null:
		f = FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	f.seek_end()
	f.store_line(text)
	f.close()

func _ts() -> String:
	return Time.get_time_string_from_system()
