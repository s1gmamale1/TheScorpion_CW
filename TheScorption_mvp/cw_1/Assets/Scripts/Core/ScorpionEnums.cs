namespace TheScorpion.Core
{
    public enum ElementType
    {
        None,
        Fire,
        Lightning
    }

    public enum GameState
    {
        PreGame,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public enum EnemyType
    {
        Basic,      // Hollow Monk
        Fast,       // Shadow Acolyte
        Heavy,      // Stone Sentinel
        Elemental,  // Elemental Ninja
        Boss        // The Fallen Guardian
    }

    public enum StyleRank
    {
        D = 0,  // 1.0x
        C = 1,  // 1.2x
        B = 2,  // 1.5x
        A = 3,  // 2.0x
        S = 4   // 2.5x
    }
}
