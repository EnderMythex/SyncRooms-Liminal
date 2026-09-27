namespace SyncRooms.Core
{
    public readonly struct LevelInfo
    {
        public readonly string scene, number, title, subtitle;
        public LevelInfo(string scene, string number, string title, string subtitle)
        {
            this.scene = scene; this.number = number; this.title = title; this.subtitle = subtitle;
        }
    }

    public static class LevelCatalog
    {
        public const string MainMenu = "MainMenu";

        public const string Hub = "Hub";

        // Story: 4 -> 5 -> 94 (castle) -> 7 -> 37 (end).  Side path: 5 (boiler room) -> 2 -> 1 -> Hub.
        public static readonly LevelInfo[] Levels =
        {
            new("Level4",  "Level 4",  "Abandoned Office", "Survival Difficulty: Class 1 — Safe, Secure, Minimal Entity Count"),
            new("Level5",  "Level 5",  "Terror Hotel",     "Survival Difficulty: Class 3 — Unsafe, Secure, Moderate Entity Count"),
            new("Level94", "Level 94", "Motion",           "Survival Difficulty: Class 2 (Day) / Class 4 (Night) — Hide indoors when the music stops"),
            new("Level7",  "Level 7",  "Thalassophobia",   "Survival Difficulty: Class 3 — Unsafe, Unsecure, Unknown Entity Count"),
            new("Level37", "Level 37", "Sublimity",        "Survival Difficulty: Class 0 — Safe, Secure, Devoid of Entities"),
            new("Level2",  "Level 2",  "Pipe Dreams",      "Survival Difficulty: Class 2 — Unsafe, Secure, Low Entity Count"),
            new("Level1",  "Level 1",  "Parking Zone",     "Survival Difficulty: Class 1 — Safe, Secure, Minimal Entity Count"),
            new("Hub",     "The Hub",  "Le carrefour",     "Chaque porte mène quelque part."),
        };

        public static LevelInfo? Find(string scene)
        {
            foreach (var l in Levels)
                if (l.scene == scene) return l;
            return null;
        }
    }
}
