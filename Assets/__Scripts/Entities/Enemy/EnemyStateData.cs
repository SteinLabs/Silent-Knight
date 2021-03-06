﻿using SilentKnight.Utility;

namespace SilentKnight.Entities
{
    /// <summary>
    /// Data container for enemy unit state information
    /// </summary>
    struct EnemyStateData
    {
        public float AfflictionEndTime { get; set; }
        public bool BossFightInit { get; set; }
        public Enums.AFFLICTION CurrentAffliction { get; set; }
        public bool Deleted { get; set; }
        public float LastAttackTime { get; set; }
        public float SpeedTemp { get; set; }
    }
}