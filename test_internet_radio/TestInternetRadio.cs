using Godot;
using System;
using Godot.Collections;

[SceneTree(root: "SceneTree")]
public partial class TestInternetRadio : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StopButton.Disabled = true;
		PlayButton.Pressed += () =>
		{
			if (RadioUrlLineEdit.Text == "")
			{
				GD.Print("Radio URL line is empty");
				return;
			}

			var uri = new Uri(RadioUrlLineEdit.Text);
			var asff = new AudioStreamFFmpeg();
			asff.UseIcy = true;
			asff.Connect("stream_metadata_changed", Callable.From<Dictionary>((metadata) =>
			{
				GD.Print("Got Server metadata!");
				GD.Print($"Stream Metadata Changed: {Json.Stringify(metadata, "\t")}");
			}));
			asff.Connect("stream_title_changed", Callable.From<String>((title) =>
			{
				GD.Print("Stream Title Changed!");
				GD.Print($"Stream Title Changed: {title}");
			}));
			var res = asff.Open(uri.AbsoluteUri);
			if (res != Error.Ok)
			{
				GD.Print("Failed to open Radio URL");
				return;
			}
			GD.Print($"Error: {res}");

			AudioPlayer.Stream = asff;
			AudioPlayer.Play();
			PlayButton.Disabled = true;
			StopButton.Disabled = false;
		};

		StopButton.Pressed += () =>
		{
			AudioPlayer.Stop();
			PlayButton.Disabled = false;
			StopButton.Disabled = true;
		};
		AudioPlayer.Stream = null;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
