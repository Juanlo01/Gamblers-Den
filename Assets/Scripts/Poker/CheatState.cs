// Shared flag between a CPU_Animator (Unity main thread) and its CheatingPlayer
// decorator (poker engine background thread). One-shot: set true when a cheat
// attempt goes uncaught, consumed (set back to false) the next time the bot acts.
public class CheatState
{
    public volatile bool Active;
}
