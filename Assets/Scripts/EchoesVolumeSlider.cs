using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class EchoesVolumeSlider : MonoBehaviour
{
    [SerializeField] private EchoesVolumeChannel channel;

    public void Configure(EchoesVolumeChannel value)
    {
        channel = value;
    }

    private void Start()
    {
        Slider slider = GetComponent<Slider>();
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(
            channel == EchoesVolumeChannel.Music ? "Echoes.MusicVolume" : "Echoes.SfxVolume",
            channel == EchoesVolumeChannel.Music ? .28f : .65f));
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void SetVolume(float value)
    {
        if (channel == EchoesVolumeChannel.Music) EchoesAudioManager.SetMusicVolume(value);
        else EchoesAudioManager.SetSfxVolume(value);
    }
}
