public sealed class SessionFlow
{
    public bool Paused { get; private set; }
    public bool GameOver { get; private set; }
    public bool Transitioning { get; private set; }
    public bool CanAcceptInput => !Paused && !GameOver && !Transitioning;
    public bool TryPause()
    { if (GameOver || Transitioning || Paused) return false; Paused=true; return true; }
    public void Resume() { Paused=false; }
    public void EndGame() { GameOver=true; Paused=false; }
    public bool TryBeginTransition()
    { if (Transitioning) return false; Transitioning=true; Paused=false; return true; }
}
