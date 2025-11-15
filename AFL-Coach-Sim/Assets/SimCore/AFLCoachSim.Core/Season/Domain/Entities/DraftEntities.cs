using System;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

#nullable enable

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Types of AFL drafts
    /// </summary>
    public enum DraftType
    {
        NationalDraft,
        RookieDraft,
        MidSeasonDraft,
        SupplementaryDraft
    }
    
    /// <summary>
    /// Status of a draft period
    /// </summary>
    public enum DraftStatus
    {
        NotStarted,
        InProgress,
        Paused,
        Completed,
        Cancelled
    }
    
    /// <summary>
    /// Types of draft prospects
    /// </summary>
    public enum DraftProspectType
    {
        Standard,
        Academy,
        Rookie,
        International,
        FatherSon,
        NGA // Next Generation Academy
    }
    
    /// <summary>
    /// Types of draft rules
    /// </summary>
    public enum DraftRuleType
    {
        Eligibility,
        Selection,
        Academy,
        FatherSon,
        Trading,
        Compensation
    }
    
    /// <summary>
    /// Status of a draft pick
    /// </summary>
    public enum DraftPickStatus
    {
        Available,
        Completed,
        Passed,
        Traded,
        Matched
    }
    
    /// <summary>
    /// Types of draft bids
    /// </summary>
    public enum DraftBidType
    {
        Standard,
        Academy,
        FatherSon,
        NextGenerationAcademy
    }
    
    /// <summary>
    /// Status of a draft bid
    /// </summary>
    public enum DraftBidStatus
    {
        Active,
        Accepted,
        Rejected,
        Withdrawn,
        Matched
    }
    /// <summary>
    /// Represents a draft pick in the AFL Draft
    /// </summary>
    public class DraftPick
    {
        public int Id { get; private set; }
        public int PickNumber { get; private set; }
        public int Round { get; private set; }
        public TeamId Team { get; private set; }
        public TeamId OriginalOwner { get; private set; }
        public DraftPickStatus Status { get; private set; }
        public int? SelectedProspectId { get; private set; }
        public DateTime? SelectionTime { get; private set; }
        public string? PassReason { get; private set; }
        public DateTime? PickStartedAt { get; private set; }
        public DateTime? SelectedAt { get; private set; }
        public DraftProspect? SelectedProspect { get; private set; }

        public DraftPick(int pickNumber, int round, TeamId team)
        {
            PickNumber = pickNumber;
            Round = round;
            Team = team;
            OriginalOwner = team; // Same as team by default
            Status = DraftPickStatus.Available;
        }
        
        public DraftPick(int pickNumber, int round, TeamId team, TeamId originalOwner, bool isMatchedPick = false)
        {
            PickNumber = pickNumber;
            Round = round;
            Team = team;
            OriginalOwner = originalOwner;
            Status = isMatchedPick ? DraftPickStatus.Matched : DraftPickStatus.Available;
        }

        public void MakeSelection(DraftProspect prospect, DateTime selectionTime)
        {
            SelectedProspectId = prospect.Id;
            SelectionTime = selectionTime;
            SelectedAt = selectionTime;
            SelectedProspect = prospect;
            Status = DraftPickStatus.Completed;
        }
        
        public void StartPick(DateTime startTime)
        {
            PickStartedAt = startTime;
        }

        public void Pass(string? reason = null)
        {
            PassReason = reason;
            Status = DraftPickStatus.Passed;
        }
    }

    /// <summary>
    /// Represents a draft prospect available for selection
    /// </summary>
    public class DraftProspect
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Age { get; private set; }
        public string Position { get; private set; }
        public string Club { get; private set; }
        public int OverallRating { get; private set; }
        public bool IsEligible { get; private set; }
        public DraftProspectType Type { get; private set; }
        public TeamId? AcademyTeam { get; private set; }
        public bool IsDrafted { get; private set; }
        public TeamId? DraftedBy { get; private set; }
        public int? DraftPickNumber { get; private set; }
        public int ProjectedDraftPosition { get; private set; }
        public DraftProspectStatus Status { get; private set; } = DraftProspectStatus.Available;
        public TeamId? FatherSonTeam { get; private set; }
        public TeamId? NGATeam { get; private set; }
        public int Ranking { get; private set; }

        public DraftProspect(string name, int age, string position, string club, int overallRating, 
                           DraftProspectType type = DraftProspectType.Standard, TeamId? academyTeam = null)
        {
            Name = name;
            Age = age;
            Position = position;
            Club = club;
            OverallRating = overallRating;
            Type = type;
            AcademyTeam = academyTeam;
            IsEligible = true;
            IsDrafted = false;
            Status = DraftProspectStatus.Available;
            ProjectedDraftPosition = CalculateProjectedPosition(overallRating, type);
            Ranking = CalculateProjectedPosition(overallRating, type);
            
            // Set special team associations based on type
            if (type == DraftProspectType.FatherSon)
                FatherSonTeam = academyTeam;
            else if (type == DraftProspectType.NGA)
                NGATeam = academyTeam;
        }

        public void Draft(TeamId team, int pickNumber)
        {
            DraftedBy = team;
            DraftPickNumber = pickNumber;
            IsDrafted = true;
            Status = DraftProspectStatus.Drafted;
        }
        
        public bool IsEligibleForDraft(DraftType draftType)
        {
            return draftType switch
            {
                DraftType.NationalDraft => IsEligible && Age >= 17 && Age <= 19,
                DraftType.RookieDraft => IsEligible && Age >= 17 && Age <= 23,
                DraftType.MidSeasonDraft => IsEligible && Age >= 17,
                DraftType.SupplementaryDraft => IsEligible,
                _ => IsEligible
            };
        }
        
        private static int CalculateProjectedPosition(int overallRating, DraftProspectType type)
        {
            // Simple projection based on rating - higher rating = earlier pick
            var basePosition = 100 - overallRating; // Rough inverse relationship
            
            // Adjust for prospect type
            return type switch
            {
                DraftProspectType.Academy => Math.Max(1, basePosition - 10),
                DraftProspectType.FatherSon => Math.Max(1, basePosition - 5),
                DraftProspectType.International => basePosition + 10,
                _ => basePosition
            };
        }
    }

    /// <summary>
    /// Represents a bid placed on a prospect during the draft
    /// </summary>
    public class DraftBid
    {
        public int Id { get; private set; }
        public TeamId BiddingTeam { get; private set; }
        public DraftProspect Prospect { get; private set; }
        public int PickNumber { get; private set; }
        public DraftBidType BidType { get; private set; }
        public DateTime BidTime { get; private set; }
        public DraftBidStatus Status { get; private set; }

        public DraftBid(TeamId biddingTeam, DraftProspect prospect, int pickNumber, 
                       DraftBidType bidType, DateTime bidTime)
        {
            BiddingTeam = biddingTeam;
            Prospect = prospect;
            PickNumber = pickNumber;
            BidType = bidType;
            BidTime = bidTime;
            Status = DraftBidStatus.Active;
        }

        public void Accept()
        {
            Status = DraftBidStatus.Accepted;
        }

        public void Reject()
        {
            Status = DraftBidStatus.Rejected;
        }
        
        public void Match(TeamId matchingTeam)
        {
            Status = DraftBidStatus.Matched;
        }
        
        public void Activate()
        {
            Status = DraftBidStatus.Active;
        }
    }

    /// <summary>
    /// Represents a rule that applies to the draft
    /// </summary>
    public class DraftRule
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DraftRuleType Type { get; private set; }
        public bool IsActive { get; private set; }
        public System.Action<TeamId, DraftProspect, DraftPick>? Validator { get; private set; }

        public DraftRule(string name, string description, DraftRuleType type)
        {
            Name = name;
            Description = description;
            Type = type;
            IsActive = true;
        }
        
        public DraftRule(string name, string description, bool isActive, System.Action<TeamId, DraftProspect, DraftPick> validator)
        {
            Name = name;
            Description = description;
            Type = DraftRuleType.Eligibility; // Default type
            IsActive = isActive;
            Validator = validator;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
        
        public void ValidateSelection(TeamId team, DraftProspect prospect, DraftPick pick)
        {
            if (IsActive && Validator != null)
            {
                Validator(team, prospect, pick);
            }
        }
    }
}