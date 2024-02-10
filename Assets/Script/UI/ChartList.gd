extends Control

@export
var item_scene:PackedScene
@export
var scroll_label:Label
var item_list:Array[Control] = []
var scroll_val = 0.0

var aaa = 0
@export
var timer:Timer
# Called when the node enters the scene tree for the first time.
func _ready():
	aaa = 0
	timer.start()
		
func timeout():
	var a:Control = item_scene.instantiate()
	add_child(a)
	item_list.append(a)
	aaa+=1
	if aaa>=20:
		timer.stop()

func x_pos_func(y_pos:float) -> float:
	return -.1 + (1-pow((y_pos-.45)*4,2))/5

var scroll_vec = 0.0
var bounce_vec = 0.0

var cur_at_index = 0
var selected
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	
	scroll_val += scroll_vec*0.02
	
	if scroll_val>17*5:scroll_val -= (scroll_val - 17*5)*0.1
	
	var max_val = -item_list.size()*17 + 5*17
	if scroll_val<=max_val : scroll_val += (max_val-scroll_val)*0.1
	
	scroll_label.text = String.num(scroll_val,4)
	scroll_vec *= 0.9
	
	for a in item_list:
		if a.position.x > 100:
			if selected != null:
				selected.modulate = Color(1,1,1,1.0)
			selected = a
			selected.modulate = Color(1,1,1,0.5)

	calc_pos()


func calc_pos():
	var i = 0
	for a in item_list:
		var y = (i*70) + (scroll_val*70/17)
		a.position = Vector2(size.x*x_pos_func(y/size.y),y)
		i += 1

var keyFlag = false
func _input(event):
	if event is InputEventMouse:
		event = event as InputEventMouse
		if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) && event is InputEventMouseMotion:
			event = event as InputEventMouseMotion
			scroll_vec += event.relative.y

	if Input.is_key_pressed(KEY_A) && !keyFlag:
		keyFlag = !keyFlag
		_ready()
	if !Input.is_key_pressed(KEY_A) && keyFlag:
		keyFlag = !keyFlag
		
