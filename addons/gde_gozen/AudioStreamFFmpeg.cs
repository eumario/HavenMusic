using Godot;
using System;
using Godot.Collections;
using Godot.NativeInterop;

public partial class AudioStreamFFmpeg : AudioStream
{
    private Variant _backer;

    public bool UseIcy
    {
        get => GetUseIcy();
        set => SetUseIcy(value);
    }
    

    public AudioStreamFFmpeg()
    {
        _backer = ClassDB.Instantiate("AudioStreamFFmpeg");
    }

    public AudioStreamFFmpeg(Variant hndl)
    {
        _backer = hndl;
    }

    public Error Open(string path, int streamIndex = -1) => _backer.AsGodotObject().Call("open", path, streamIndex).As<Error>();

    public override AudioStreamPlayback _InstantiatePlayback() => _backer.AsGodotObject().Call("__instantiate_playback").As<AudioStreamPlayback>();

    public override double _GetLength() => _backer.AsGodotObject().Call("get_length").AsDouble();
    
    public bool GetUseIcy() => _backer.AsGodotObject().Call("get_use_icy").AsBool();
    public void SetUseIcy(bool value) => _backer.AsGodotObject().Call("set_use_icy", value);

}
