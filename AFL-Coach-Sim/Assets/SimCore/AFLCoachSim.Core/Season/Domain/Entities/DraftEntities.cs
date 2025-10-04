using System;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents a draft pick in the AFL Draft
    /// </summary>
    public class DraftPick
    {
        public int Id { get; private set; }
        public int PickNumber { get; private set; }
        public int Round { get; private set; }
        public TeamId Team { get; private set; }
        public DraftPickStatus Status { get; private set; }
        public int? SelectedProspectId { get; private set; }
        public DateTime? SelectionTime { get; private set; }
        public string? PassReason { get; private set; }

        public DraftPick(int pickNumber, int round, TeamId team)
        {
            PickNumber = pickNumber;
            Round = round;
            Team = team;
            Status = DraftPickStatus.Available;
        }

        public void MakeSelection(DraftProspect prospect, DateTime selectionTime)
        {
            SelectedProspectId = prospect.Id;
            SelectionTime = selectionTime;
            Status = DraftPickStatus.Completed;
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
        }

        public void Draft(TeamId team, int pickNumber)
        {
            DraftedBy = team;
            DraftPickNumber = pickNumber;
            IsDrafted = true;
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

        public DraftRule(string name, string description, DraftRuleType type)
        {
            Name = name;
            Description = description;
            Type = type;
            IsActive = true;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}