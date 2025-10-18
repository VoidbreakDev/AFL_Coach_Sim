namespace AFLCoachSim.Core.Domain.ValueObjects
{
    public enum Role 
    { 
        KPD, KPF, SMLF, SMLB, MID, WING, HBF, HFF, RUC,
        
        // Traditional AFL position names
        FB,   // Full Back
        CHB,  // Center Half Back  
        C,    // Center
        RW,   // Right Wing
        LW,   // Left Wing
        CHF,  // Center Half Forward
        FF,   // Full Forward
        RUCK, // Ruckman (alias for RUC)
        
        // Compatibility aliases for broader categories
        Defender = KPD,
        Midfielder = MID,
        Forward = KPF,
        Ruck = RUC,
        
        // Additional descriptive aliases
        KeyForward = KPF,
        SmallForward = SMLF,
        ForwardPocket = SMLF, // Same as small forward
        KeyDefender = KPD,
        SmallDefender = SMLB,
        Sweeper = SMLB, // Map to small back
        Tagger = MID, // Tagging midfielder
        HalfForward = HFF
    }
}
