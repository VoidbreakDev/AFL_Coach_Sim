using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents the AFL Draft Period including National Draft, Rookie Draft, etc.
    /// </summary>
    public class DraftPeriod
    {
        public int Id { get; private set; }
        public int Year { get; private set; }
        public DraftType DraftType { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public DraftStatus Status { get; private set; }
        public int TotalRounds { get; private set; }
        public int CurrentRound { get; private set; }
        public int CurrentPickNumber { get; private set; }
        public bool LivePicking { get; private set; }
        public TimeSpan? PickTimeLimit { get; private set; }
        public string Venue { get; private set; }
        
        private readonly List<DraftPick> _picks;
        private readonly List<DraftProspect> _prospects;
        private readonly List<DraftBid> _bids;
        private readonly List<DraftRule> _rules;
        
        public IReadOnlyList<DraftPick> Picks => _picks.AsReadOnly();
        public IReadOnlyList<DraftProspect> Prospects => _prospects.AsReadOnly();
        public IReadOnlyList<DraftBid> Bids => _bids.AsReadOnly();
        public IReadOnlyList<DraftRule> Rules => _rules.AsReadOnly();

        // Statistics
        public int TotalPicks => _picks.Count;
        public int CompletedPicks => _picks.Count(p => p.Status == DraftPickStatus.Completed);
        public int PassedPicks => _picks.Count(p => p.Status == DraftPickStatus.Passed);
        public int BiddedPicks => _bids.Count(b => b.Status == DraftBidStatus.Active);

        // Default constructor for EF Core
        protected DraftPeriod()
        {
            Venue = string.Empty;
            _picks = new List<DraftPick>();
            _prospects = new List<DraftProspect>();
            _bids = new List<DraftBid>();
            _rules = new List<DraftRule>();
        }

        public DraftPeriod(int year, DraftType draftType, DateTime startDate, int totalRounds, 
            string venue, TimeSpan? pickTimeLimit = null, List<DraftRule>? rules = null)
        {
            Year = year;
            DraftType = draftType;
            StartDate = startDate;
            TotalRounds = totalRounds;
            Venue = venue;
            PickTimeLimit = pickTimeLimit ?? GetDefaultPickTimeLimit(draftType);
            Status = DraftStatus.NotStarted;
            CurrentRound = 1;
            CurrentPickNumber = 1;
            LivePicking = false;
            
            _picks = new List<DraftPick>();
            _prospects = new List<DraftProspect>();
            _bids = new List<DraftBid>();
            _rules = rules ?? CreateDefaultRules(draftType);
        }

        /// <summary>
        /// Starts the draft
        /// </summary>
        public void StartDraft()
        {
            if (Status != DraftStatus.NotStarted)
                throw new InvalidOperationException($"Cannot start draft in {Status} status");

            if (DateTime.UtcNow < StartDate)
                throw new InvalidOperationException("Cannot start draft before start date");

            Status = DraftStatus.InProgress;
            LivePicking = true;
        }

        /// <summary>
        /// Completes the draft
        /// </summary>
        public void CompleteDraft()
        {
            if (Status != DraftStatus.InProgress)
                throw new InvalidOperationException($"Cannot complete draft in {Status} status");

            Status = DraftStatus.Completed;
            EndDate = DateTime.UtcNow;
            LivePicking = false;
        }

        /// <summary>
        /// Pauses the draft
        /// </summary>
        public void PauseDraft()
        {
            if (Status != DraftStatus.InProgress)
                throw new InvalidOperationException($"Cannot pause draft in {Status} status");

            Status = DraftStatus.Paused;
            LivePicking = false;
        }

        /// <summary>
        /// Resumes the draft
        /// </summary>
        public void ResumeDraft()
        {
            if (Status != DraftStatus.Paused)
                throw new InvalidOperationException($"Cannot resume draft in {Status} status");

            Status = DraftStatus.InProgress;
            LivePicking = true;
        }

        /// <summary>
        /// Makes a draft selection
        /// </summary>
        public DraftPick MakeSelection(TeamId team, int prospectId, int pickNumber)
        {
            if (Status != DraftStatus.InProgress)
                throw new InvalidOperationException("Draft is not in progress");

            var pick = _picks.FirstOrDefault(p => p.PickNumber == pickNumber);
            if (pick == null)
                throw new ArgumentException("Invalid pick number");

            if (pick.Team != team)
                throw new UnauthorizedAccessException("Team does not own this pick");

            if (pick.Status != DraftPickStatus.Available)
                throw new InvalidOperationException($"Pick {pickNumber} is not available");

            var prospect = _prospects.FirstOrDefault(p => p.Id == prospectId);
            if (prospect == null)
                throw new ArgumentException("Invalid prospect");

            if (!prospect.IsEligible)
                throw new InvalidOperationException("Prospect is not eligible");

            // Validate selection against draft rules
            ValidateSelection(team, prospect, pick);

            // Make the selection
            pick.MakeSelection(prospect, DateTime.UtcNow);
            prospect.Draft(team, pickNumber);

            // Advance to next pick
            AdvanceToNextPick();

            return pick;
        }

        /// <summary>
        /// Passes on a draft pick
        /// </summary>
        public void PassPick(TeamId team, int pickNumber, string reason = null)
        {
            var pick = _picks.FirstOrDefault(p => p.PickNumber == pickNumber);
            if (pick == null)
                throw new ArgumentException("Invalid pick number");

            if (pick.Team != team)
                throw new UnauthorizedAccessException("Team does not own this pick");

            if (pick.Status != DraftPickStatus.Available)
                throw new InvalidOperationException($"Pick {pickNumber} is not available");

            pick.Pass(reason);
            AdvanceToNextPick();
        }

        /// <summary>
        /// Places a bid on a prospect (for Academy/Father-Son players)
        /// </summary>
        public DraftBid PlaceBid(TeamId biddingTeam, int prospectId, int pickNumber, DraftBidType bidType)
        {
            if (Status != DraftStatus.InProgress)
                throw new InvalidOperationException("Draft is not in progress");

            var prospect = _prospects.FirstOrDefault(p => p.Id == prospectId);
            if (prospect == null)
                throw new ArgumentException("Invalid prospect");

            if (!CanBidOnProspect(biddingTeam, prospect, bidType))
                throw new InvalidOperationException("Team cannot bid on this prospect");

            var bid = new DraftBid(biddingTeam, prospect, pickNumber, bidType, DateTime.UtcNow);
            _bids.Add(bid);

            // Process bid immediately if it's valid
            ProcessBid(bid);

            return bid;
        }

        /// <summary>
        /// Matches a bid (for Academy/Father-Son selections)
        /// </summary>
        public DraftPick MatchBid(int bidId, TeamId matchingTeam)
        {
            var bid = _bids.FirstOrDefault(b => b.Id == bidId);
            if (bid == null)
                throw new ArgumentException("Invalid bid");

            if (bid.Status != DraftBidStatus.Active)
                throw new InvalidOperationException("Bid is not active");

            if (!CanMatchBid(matchingTeam, bid))
                throw new InvalidOperationException("Team cannot match this bid");

            // Match the bid
            bid.Match(matchingTeam);

            // Create matched pick
            var matchedPick = CreateMatchedPick(bid, matchingTeam);
            _picks.Add(matchedPick);

            return matchedPick;
        }

        /// <summary>
        /// Gets available prospects for a team
        /// </summary>
        public IEnumerable<DraftProspect> GetAvailableProspects(TeamId team)
        {
            return _prospects.Where(p => p.IsEligible && p.Status == DraftProspectStatus.Available)
                            .OrderBy(p => p.Ranking);
        }

        /// <summary>
        /// Gets draft order for current round
        /// </summary>
        public IEnumerable<DraftPick> GetDraftOrder(int round)
        {
            return _picks.Where(p => p.Round == round).OrderBy(p => p.PickNumber);
        }

        /// <summary>
        /// Gets next available pick
        /// </summary>
        public DraftPick? GetNextPick()
        {
            return _picks.Where(p => p.Status == DraftPickStatus.Available)
                        .OrderBy(p => p.PickNumber)
                        .FirstOrDefault();
        }

        /// <summary>
        /// Gets team's remaining picks
        /// </summary>
        public IEnumerable<DraftPick> GetTeamPicks(TeamId team)
        {
            return _picks.Where(p => p.Team == team && p.Status == DraftPickStatus.Available);
        }

        /// <summary>
        /// Gets draft statistics
        /// </summary>
        public DraftStatistics GetStatistics()
        {
            return new DraftStatistics
            {
                TotalPicks = TotalPicks,
                CompletedPicks = CompletedPicks,
                PassedPicks = PassedPicks,
                BiddedPicks = BiddedPicks,
                ProspectsRemaining = _prospects.Count(p => p.Status == DraftProspectStatus.Available),
                AveragePickTime = CalculateAveragePickTime(),
                FastestPick = GetFastestPickTime(),
                SlowestPick = GetSlowestPickTime(),
                MostActiveTeam = GetMostActiveTeam(),
                SurprisePicks = GetSurprisePickCount(),
                TradedPicks = _picks.Count(p => p.OriginalOwner != p.Team)
            };
        }

        /// <summary>
        /// Checks if draft is complete
        /// </summary>
        public bool IsComplete()
        {
            return Status == DraftStatus.Completed || 
                   CurrentRound > TotalRounds ||
                   _picks.All(p => p.Status != DraftPickStatus.Available);
        }

        /// <summary>
        /// Gets time remaining for current pick
        /// </summary>
        public TimeSpan? GetPickTimeRemaining()
        {
            if (!LivePicking || !PickTimeLimit.HasValue) return null;

            var currentPick = GetNextPick();
            if (currentPick?.PickStartedAt == null) return PickTimeLimit;

            var elapsed = DateTime.UtcNow - currentPick.PickStartedAt.Value;
            var remaining = PickTimeLimit.Value - elapsed;
            
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void AdvanceToNextPick()
        {
            var nextPick = GetNextPick();
            if (nextPick != null)
            {
                CurrentPickNumber = nextPick.PickNumber;
                CurrentRound = nextPick.Round;
                nextPick.StartPick(DateTime.UtcNow);
            }
            else
            {
                // No more picks available
                if (IsComplete())
                {
                    CompleteDraft();
                }
            }
        }

        private void ValidateSelection(TeamId team, DraftProspect prospect, DraftPick pick)
        {
            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.ValidateSelection(team, prospect, pick);
            }
        }

        private bool CanBidOnProspect(TeamId team, DraftProspect prospect, DraftBidType bidType)
        {
            return bidType switch
            {
                DraftBidType.Academy => prospect.AcademyTeam == team,
                DraftBidType.FatherSon => prospect.FatherSonTeam == team,
                DraftBidType.NextGenerationAcademy => prospect.NGATeam == team,
                _ => false
            };
        }

        private bool CanMatchBid(TeamId team, DraftBid bid)
        {
            return bid.BidType switch
            {
                DraftBidType.Academy => bid.Prospect.AcademyTeam == team,
                DraftBidType.FatherSon => bid.Prospect.FatherSonTeam == team,
                DraftBidType.NextGenerationAcademy => bid.Prospect.NGATeam == team,
                _ => false
            };
        }

        private void ProcessBid(DraftBid bid)
        {
            // Mark bid as active
            bid.Activate();

            // Implementation would handle bid processing logic
            // e.g., notifications to matching teams, bid priority, etc.
        }

        private DraftPick CreateMatchedPick(DraftBid bid, TeamId matchingTeam)
        {
            // Create a special matched pick
            var pick = new DraftPick(
                bid.PickNumber,
                bid.PickNumber <= _picks.Count ? _picks.First(p => p.PickNumber == bid.PickNumber).Round : CurrentRound,
                matchingTeam,
                matchingTeam, // Original owner same as current for matched picks
                true); // Is matched pick

            pick.MakeSelection(bid.Prospect, DateTime.UtcNow);
            bid.Prospect.Draft(matchingTeam, bid.PickNumber);

            return pick;
        }

        private TimeSpan CalculateAveragePickTime()
        {
            var completedPicks = _picks.Where(p => p.Status == DraftPickStatus.Completed && 
                                                  p.PickStartedAt.HasValue && 
                                                  p.SelectedAt.HasValue);

            if (!completedPicks.Any()) return TimeSpan.Zero;

            var totalTime = completedPicks.Sum(p => (p.SelectedAt!.Value - p.PickStartedAt!.Value).TotalSeconds);
            return TimeSpan.FromSeconds(totalTime / completedPicks.Count());
        }

        private TimeSpan GetFastestPickTime()
        {
            var completedPicks = _picks.Where(p => p.Status == DraftPickStatus.Completed && 
                                                  p.PickStartedAt.HasValue && 
                                                  p.SelectedAt.HasValue);

            if (!completedPicks.Any()) return TimeSpan.Zero;

            return completedPicks.Min(p => p.SelectedAt!.Value - p.PickStartedAt!.Value);
        }

        private TimeSpan GetSlowestPickTime()
        {
            var completedPicks = _picks.Where(p => p.Status == DraftPickStatus.Completed && 
                                                  p.PickStartedAt.HasValue && 
                                                  p.SelectedAt.HasValue);

            if (!completedPicks.Any()) return TimeSpan.Zero;

            return completedPicks.Max(p => p.SelectedAt!.Value - p.PickStartedAt!.Value);
        }

        private TeamId GetMostActiveTeam()
        {
            var teamPickCount = _picks.Where(p => p.Status == DraftPickStatus.Completed)
                                     .GroupBy(p => p.Team)
                                     .ToDictionary(g => g.Key, g => g.Count());

            return teamPickCount.Any() 
                ? teamPickCount.OrderByDescending(kvp => kvp.Value).First().Key 
                : TeamId.None;
        }

        private int GetSurprisePickCount()
        {
            // Count picks where player was selected significantly higher than projected
            return _picks.Count(p => p.Status == DraftPickStatus.Completed && 
                               p.SelectedProspect != null &&
                               p.SelectedProspect.ProjectedDraftPosition > p.PickNumber + 20);
        }

        private static TimeSpan GetDefaultPickTimeLimit(DraftType draftType)
        {
            return draftType switch
            {
                DraftType.NationalDraft => TimeSpan.FromMinutes(5),
                DraftType.RookieDraft => TimeSpan.FromMinutes(3),
                DraftType.MidSeasonDraft => TimeSpan.FromMinutes(2),
                DraftType.SupplementaryDraft => TimeSpan.FromMinutes(2),
                _ => TimeSpan.FromMinutes(5)
            };
        }

        private static List<DraftRule> CreateDefaultRules(DraftType draftType)
        {
            var rules = new List<DraftRule>
            {
                new DraftRule("EligibilityCheck", "Players must be eligible for this draft type", true,
                    (team, prospect, pick) => ValidateEligibility(prospect, draftType)),
                new DraftRule("AgeRequirement", "Players must meet age requirements", true,
                    (team, prospect, pick) => ValidateAge(prospect, draftType)),
                new DraftRule("ListSizeCheck", "Teams must have available list spots", true,
                    (team, prospect, pick) => ValidateListSize(team, draftType))
            };

            return rules;
        }

        private static void ValidateEligibility(DraftProspect prospect, DraftType draftType)
        {
            if (!prospect.IsEligibleForDraft(draftType))
                throw new InvalidOperationException($"Prospect {prospect.Name} is not eligible for {draftType} draft");
        }

        private static void ValidateAge(DraftProspect prospect, DraftType draftType)
        {
            // Implementation would check age requirements based on draft type
        }

        private static void ValidateListSize(TeamId team, DraftType draftType)
        {
            // Implementation would check team list sizes
        }
    }

    /// <summary>
    /// Statistics for the draft period
    /// </summary>
    public class DraftStatistics
    {
        public int TotalPicks { get; set; }
        public int CompletedPicks { get; set; }
        public int PassedPicks { get; set; }
        public int BiddedPicks { get; set; }
        public int ProspectsRemaining { get; set; }
        public TimeSpan AveragePickTime { get; set; }
        public TimeSpan FastestPick { get; set; }
        public TimeSpan SlowestPick { get; set; }
        public TeamId MostActiveTeam { get; set; }
        public int SurprisePicks { get; set; }
        public int TradedPicks { get; set; }
    }
}