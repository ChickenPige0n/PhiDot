extends Node2D

@export
var hit_effect:PackedScene

# Called when the node enters the scene tree for the first time.
func gen_he():
	var a:AnimatedSprite2D = hit_effect.instantiate()
	a.global_position = DisplayServer.window_get_size()/2
	add_child(a)
	

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

