using FMOD;

namespace Nautilus.FMod.Interfaces;

public interface IFModSoundBuilder
{
    public IFModSoundBuilder SetMode(MODE mode);
    public IFModSoundBuilder SetMode3D(float minDistance, float maxDistance);
    public IFModSoundBuilder SetMode2D();
    public IFModSoundBuilder SetModeMusic();
    public IFModSoundBuilder SetFadeDuration(float fadeDuration);
    public IFModSoundBuilder SetSound(string clipName);
    public IFModSoundBuilder SetSounds(bool randomizeOrder, params string[] clipNames);
    public void Register();
}