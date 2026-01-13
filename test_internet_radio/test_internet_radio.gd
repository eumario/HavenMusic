extends PanelContainer
@onready var radio_url_line_edit: LineEdit = %RadioUrlLineEdit
@onready var play_button: Button = %PlayButton
@onready var stop_button: Button = %StopButton
@onready var audio_player: AudioStreamPlayer = %AudioPlayer

var icy_meta: Dictionary = {}
var stream_title: String = ""
var tags: Dictionary = {}
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	play_button.disabled = false
	stop_button.disabled = true
	play_button.pressed.connect(_load_stream)
	stop_button.pressed.connect(_stop_stream)

func _load_stream() -> void:
	if radio_url_line_edit.text == "":
		print("Radio URL line is empty")
		return
	
	var asff = AudioStreamFFmpeg.new()
	asff.use_icy = true
	asff.open(radio_url_line_edit.text)
	audio_player.stream = asff
	audio_player.play()
	play_button.disabled = true
	stop_button.disabled = false
	tags = asff.get_tags()
	print("Tags: %s" % JSON.stringify(tags, "\t"))

func _stop_stream() -> void:
	audio_player.stop()
	play_button.disabled = false
	stop_button.disabled = true

func _process(_delta: float) -> void:
	if audio_player.stream == null:
		return
	if not audio_player.playing:
		return
	var asff: AudioStreamFFmpeg = audio_player.stream
	var new_icy = asff.get_icy_headers()
	if icy_meta.hash() != new_icy.hash():
		print("Icy Metadata has changed: ", JSON.stringify(new_icy, "\t"))
		icy_meta = new_icy
	
	var new_title = asff.get_stream_title()
	if new_title != stream_title:
		print("Stream Title Changed: %s" % new_title)
		stream_title = new_title
	
	var new_tags = asff.get_tags()
	if new_tags.hash() != tags.hash():
		print("New ID3 Tags: %s" % JSON.stringify(new_tags, "\t"))
		tags = new_tags
