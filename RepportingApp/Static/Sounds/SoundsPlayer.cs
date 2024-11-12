namespace RepportingApp.Static.Sounds;

public static class SoundsPlayer
{
    static readonly string RelativePath = Path.Combine("Assets", "SFX", "UiSfx.mp3");
    public static void StartSfxOne()
    {
        using var audioFile = new AudioFileReader(RelativePath);
        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(audioFile);
        outputDevice.Play();
    }

}