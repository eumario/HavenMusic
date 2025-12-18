using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Spectrum : ColorRect
{
	[Export] public int VuCount = 30;
	[Export] public float FreqMax = 11050.0f;
	[Export] public int MinDb = 60;
	[Export] public float AnimationSpeed = 0.1f;
	[Export] public float HeightScale = 8.0f;

	private AudioEffectSpectrumAnalyzerInstance _spectrum;
	private List<float> _minValues = [];
	private List<float> _maxValues = [];

	public bool Paused { get; set; } = false;

	public override void _Ready()
	{
		_spectrum = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(0, 0);
		_minValues.Capacity = VuCount;
		_minValues.AddRange(Enumerable.Repeat(0.0f, VuCount));
		_maxValues.Capacity = VuCount;
		_maxValues.AddRange(Enumerable.Repeat(0.0f, VuCount));
	}

	public override void _Process(double delta)
	{
		if (Paused) return;
		var prevHz = 0.0f;
		var data = new List<float>();
		
		foreach (var i in Enumerable.Range(1, VuCount + 1))
		{
			var hz = i * FreqMax / VuCount;
			var f = _spectrum.GetMagnitudeForFrequencyRange(prevHz, hz);
			var energy = Mathf.Clamp((MinDb + Mathf.LinearToDb(f.Length())) / MinDb, 0.0f, 1.0f);
			data.Add(energy * HeightScale);
			prevHz = hz;
		}

		foreach (var i in Enumerable.Range(0, VuCount))
		{
			if (data[i] > _maxValues[i])
				_maxValues[i] = data[i];
			else
				_maxValues[i] = Mathf.Lerp(_maxValues[i], data[i], AnimationSpeed);
			if (data[i] <= 0.0f)
			{
				_minValues[i] = Mathf.Lerp(_minValues[i], 0.0f, AnimationSpeed);
			}
		}

		var fft = new List<float>();
		foreach(var i in Enumerable.Range(0, VuCount))
			fft.Add(Mathf.Lerp(_minValues[i], _maxValues[i], AnimationSpeed));

		((ShaderMaterial)Material).SetShaderParameter("freq_data", fft.ToArray());
	}
}
