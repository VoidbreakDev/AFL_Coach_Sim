using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents a completed AFL trade between two teams
    /// </summary>
    public class Trade
    {
        public int Id { get; private set; }
        public TeamId Team1 { get; private set; }
        public TeamId Team2 { get; private set; }
        public DateTime ProposedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public TradeStatus Status { get; private set; }
        public string? Notes { get; private set; }
        public TradeType TradeType { get; private set; }
        
        private readonly List<TradeAsset> _team1Assets;
        private readonly List<TradeAsset> _team2Assets;
        
        public IReadOnlyList<TradeAsset> Team1Assets => _team1Assets.AsReadOnly();
        public IReadOnlyList<TradeAsset> Team2Assets => _team2Assets.AsReadOnly();
        
        // Trade metadata
        public bool IsMultiTeamTrade => false; // AFL currently only allows 2-team trades
        public int TotalPlayersInvolved => _team1Assets.Concat(_team2Assets).Count(a => a.Type == TradeAssetType.Player);
        public int TotalPicksInvolved => _team1Assets.Concat(_team2Assets).Count(a => a.Type == TradeAssetType.DraftPick);
        
        // Default constructor for EF Core
        protected Trade()
        {
            _team1Assets = new List<TradeAsset>();
            _team2Assets = new List<TradeAsset>();
        }

        public Trade(TeamId team1, TeamId team2, List<TradeAsset> team1Assets, List<TradeAsset> team2Assets, DateTime proposedAt, string? notes = null)
        {
            Team1 = team1;
            Team2 = team2;
            ProposedAt = proposedAt;
            Status = TradeStatus.Proposed;
            Notes = notes;
            
            _team1Assets = team1Assets ?? throw new ArgumentNullException(nameof(team1Assets));
            _team2Assets = team2Assets ?? throw new ArgumentNullException(nameof(team2Assets));
            
            TradeType = DetermineTradeType();
        }

        /// <summary>
        /// Completes the trade
        /// </summary>
        public void Complete()
        {
            if (Status != TradeStatus.Proposed)
                throw new InvalidOperationException($"Cannot complete trade in {Status} status");

            Status = TradeStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Rejects the trade
        /// </summary>
        public void Reject(string reason = "")
        {
            if (Status != TradeStatus.Proposed)
                throw new InvalidOperationException($"Cannot reject trade in {Status} status");

            Status = TradeStatus.Rejected;
            Notes = !string.IsNullOrEmpty(reason) ? reason : Notes;
        }

        /// <summary>
        /// Withdraws the trade
        /// </summary>
        public void Withdraw(string reason = "")
        {
            if (Status != TradeStatus.Proposed)
                throw new InvalidOperationException($"Cannot withdraw trade in {Status} status");

            Status = TradeStatus.Withdrawn;
            Notes = !string.IsNullOrEmpty(reason) ? reason : Notes;
        }

        /// <summary>
        /// Gets all assets involved in the trade
        /// </summary>
        public IEnumerable<TradeAsset> GetAllAssets()
        {
            return _team1Assets.Concat(_team2Assets);
        }

        /// <summary>
        /// Gets assets for a specific team
        /// </summary>
        public IEnumerable<TradeAsset> GetAssetsForTeam(TeamId teamId)
        {
            if (teamId == Team1) return _team1Assets;
            if (teamId == Team2) return _team2Assets;
            return Enumerable.Empty<TradeAsset>();
        }

        /// <summary>
        /// Gets assets received by a specific team (from the other team)
        /// </summary>
        public IEnumerable<TradeAsset> GetAssetsReceivedByTeam(TeamId teamId)
        {
            if (teamId == Team1) return _team2Assets;
            if (teamId == Team2) return _team1Assets;
            return Enumerable.Empty<TradeAsset>();
        }

        /// <summary>
        /// Calculates the estimated value of the trade
        /// </summary>
        public decimal CalculateTradeValue()
        {
            var team1Value = _team1Assets.Sum(a => a.EstimatedValue);
            var team2Value = _team2Assets.Sum(a => a.EstimatedValue);
            return (team1Value + team2Value) / 2; // Average value
        }

        /// <summary>
        /// Gets trade balance (positive means Team1 received more value)
        /// </summary>
        public decimal GetTradeBalance()
        {
            var team1Received = _team2Assets.Sum(a => a.EstimatedValue);
            var team2Received = _team1Assets.Sum(a => a.EstimatedValue);
            return team1Received - team2Received;
        }

        /// <summary>
        /// Gets a human-readable trade summary
        /// </summary>
        public string GetTradeSummary()
        {
            var team1Summary = string.Join(", ", _team1Assets.Select(a => a.Description));
            var team2Summary = string.Join(", ", _team2Assets.Select(a => a.Description));
            
            return $"{Team1}: {team1Summary} ↔ {Team2}: {team2Summary}";
        }

        /// <summary>
        /// Checks if a team was involved in this trade
        /// </summary>
        public bool InvolvesTeam(TeamId teamId)
        {
            return Team1 == teamId || Team2 == teamId;
        }

        /// <summary>
        /// Checks if the trade involved a specific player
        /// </summary>
        public bool InvolvesPlayer(int playerId)
        {
            return GetAllAssets().Any(a => a.Type == TradeAssetType.Player && a.PlayerId == playerId);
        }

        /// <summary>
        /// Checks if the trade involved draft picks
        /// </summary>
        public bool InvolvesDraftPicks()
        {
            return GetAllAssets().Any(a => a.Type == TradeAssetType.DraftPick);
        }

        private TradeType DetermineTradeType()
        {
            var allAssets = GetAllAssets().ToList();
            var hasPlayers = allAssets.Any(a => a.Type == TradeAssetType.Player);
            var hasPicks = allAssets.Any(a => a.Type == TradeAssetType.DraftPick);
            var hasFuturePicks = allAssets.Any(a => a.Type == TradeAssetType.FuturePick);
            
            if (hasPlayers && !hasPicks && !hasFuturePicks)
                return TradeType.PlayerOnly;
            
            if (!hasPlayers && (hasPicks || hasFuturePicks))
                return TradeType.PickOnly;
            
            if (hasPlayers && (hasPicks || hasFuturePicks))
                return TradeType.PlayerAndPick;
                
            return TradeType.Other;
        }
    }

    /// <summary>
    /// Represents a trade offer that hasn't been completed yet
    /// </summary>
    public class TradeOffer
    {
        public int Id { get; private set; }
        public TeamId ProposingTeam { get; private set; }
        public TeamId ReceivingTeam { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public TradeOfferStatus Status { get; private set; }
        public string? ResponseNote { get; private set; }
        public DateTime? ResponseAt { get; private set; }
        
        private readonly List<TradeAsset> _offeredAssets;
        private readonly List<TradeAsset> _requestedAssets;
        
        public IReadOnlyList<TradeAsset> OfferedAssets => _offeredAssets.AsReadOnly();
        public IReadOnlyList<TradeAsset> RequestedAssets => _requestedAssets.AsReadOnly();

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public TimeSpan TimeRemaining => ExpiresAt > DateTime.UtcNow ? ExpiresAt - DateTime.UtcNow : TimeSpan.Zero;
        
        // Default constructor for EF Core
        protected TradeOffer()
        {
            _offeredAssets = new List<TradeAsset>();
            _requestedAssets = new List<TradeAsset>();
        }

        public TradeOffer(TeamId proposingTeam, TeamId receivingTeam, List<TradeAsset> offeredAssets, List<TradeAsset> requestedAssets, DateTime expiresAt)
        {
            ProposingTeam = proposingTeam;
            ReceivingTeam = receivingTeam;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
            Status = TradeOfferStatus.Pending;
            
            _offeredAssets = offeredAssets ?? throw new ArgumentNullException(nameof(offeredAssets));
            _requestedAssets = requestedAssets ?? throw new ArgumentNullException(nameof(requestedAssets));
        }

        /// <summary>
        /// Accepts the trade offer
        /// </summary>
        public void Accept(string? note = null)
        {
            if (Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot accept offer in {Status} status");

            if (IsExpired)
                throw new InvalidOperationException("Cannot accept expired trade offer");

            Status = TradeOfferStatus.Accepted;
            ResponseNote = note;
            ResponseAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Rejects the trade offer
        /// </summary>
        public void Reject(string? note = null)
        {
            if (Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot reject offer in {Status} status");

            Status = TradeOfferStatus.Rejected;
            ResponseNote = note;
            ResponseAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Withdraws the trade offer
        /// </summary>
        public void Withdraw()
        {
            if (Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot withdraw offer in {Status} status");

            Status = TradeOfferStatus.Withdrawn;
            ResponseAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Extends the expiration time of the offer
        /// </summary>
        public void ExtendExpiration(TimeSpan extension)
        {
            if (Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException("Can only extend pending offers");

            if (extension.TotalHours < 1 || extension.TotalDays > 7)
                throw new ArgumentException("Extension must be between 1 hour and 7 days");

            ExpiresAt = ExpiresAt.Add(extension);
        }

        /// <summary>
        /// Gets the total estimated value of what's being offered
        /// </summary>
        public decimal GetOfferedValue()
        {
            return _offeredAssets.Sum(a => a.EstimatedValue);
        }

        /// <summary>
        /// Gets the total estimated value of what's being requested
        /// </summary>
        public decimal GetRequestedValue()
        {
            return _requestedAssets.Sum(a => a.EstimatedValue);
        }

        /// <summary>
        /// Gets the value balance of the trade offer
        /// </summary>
        public decimal GetValueBalance()
        {
            return GetOfferedValue() - GetRequestedValue();
        }

        /// <summary>
        /// Gets a summary of the trade offer
        /// </summary>
        public string GetOfferSummary()
        {
            var offered = string.Join(", ", _offeredAssets.Select(a => a.Description));
            var requested = string.Join(", ", _requestedAssets.Select(a => a.Description));
            
            return $"{ProposingTeam} offers {offered} for {requested}";
        }
    }

    /// <summary>
    /// Represents an asset that can be traded (player, draft pick, etc.)
    /// </summary>
    public class TradeAsset
    {
        public int Id { get; private set; }
        public TradeAssetType Type { get; private set; }
        public string Description { get; private set; }
        public decimal EstimatedValue { get; private set; }
        public int? PlayerId { get; private set; }
        public int? DraftYear { get; private set; }
        public int? DraftRound { get; private set; }
        public int? DraftPickNumber { get; private set; }
        public TeamId? OriginalOwner { get; private set; } // For traded picks
        public Dictionary<string, object> Metadata { get; private set; }

        // Default constructor for EF Core
        protected TradeAsset()
        {
            Description = string.Empty;
            Metadata = new Dictionary<string, object>();
        }

        public TradeAsset(TradeAssetType type, string description, decimal estimatedValue, 
            int? playerId = null, int? draftYear = null, int? draftRound = null, int? draftPickNumber = null,
            TeamId? originalOwner = null, Dictionary<string, object>? metadata = null)
        {
            Type = type;
            Description = description;
            EstimatedValue = estimatedValue;
            PlayerId = playerId;
            DraftYear = draftYear;
            DraftRound = draftRound;
            DraftPickNumber = draftPickNumber;
            OriginalOwner = originalOwner;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a player trade asset
        /// </summary>
        public static TradeAsset CreatePlayer(int playerId, string playerName, decimal estimatedValue, Dictionary<string, object>? metadata = null)
        {
            return new TradeAsset(TradeAssetType.Player, playerName, estimatedValue, playerId: playerId, metadata: metadata);
        }

        /// <summary>
        /// Creates a draft pick trade asset
        /// </summary>
        public static TradeAsset CreateDraftPick(int year, int round, int pickNumber, decimal estimatedValue, TeamId? originalOwner = null)
        {
            var description = originalOwner.HasValue ? 
                $"{year} Round {round} Pick #{pickNumber} (originally {originalOwner})" :
                $"{year} Round {round} Pick #{pickNumber}";
                
            return new TradeAsset(TradeAssetType.DraftPick, description, estimatedValue, 
                draftYear: year, draftRound: round, draftPickNumber: pickNumber, originalOwner: originalOwner);
        }

        /// <summary>
        /// Creates a future pick trade asset
        /// </summary>
        public static TradeAsset CreateFuturePick(int year, int round, decimal estimatedValue, TeamId originalOwner)
        {
            return new TradeAsset(TradeAssetType.FuturePick, $"{year} Round {round} Pick ({originalOwner})", 
                estimatedValue, draftYear: year, draftRound: round, originalOwner: originalOwner);
        }

        /// <summary>
        /// Checks if the asset is still available for trading
        /// </summary>
        public bool IsStillAvailable()
        {
            // Implementation would check if player is still eligible, pick still exists, etc.
            // For now, return true
            return true;
        }

        /// <summary>
        /// Updates the estimated value of the asset
        /// </summary>
        public void UpdateValue(decimal newValue, string reason = "")
        {
            EstimatedValue = newValue;
            if (!string.IsNullOrEmpty(reason))
            {
                Metadata["LastValueUpdate"] = DateTime.UtcNow;
                Metadata["ValueUpdateReason"] = reason;
            }
        }

        /// <summary>
        /// Checks if this is a premium asset (high value)
        /// </summary>
        public bool IsPremiumAsset()
        {
            return Type switch
            {
                TradeAssetType.Player => EstimatedValue > 1000, // High value player
                TradeAssetType.DraftPick => DraftRound == 1 && DraftPickNumber <= 10, // Top 10 pick
                TradeAssetType.FuturePick => DraftRound == 1, // First round future pick
                _ => EstimatedValue > 500
            };
        }
    }

    /// <summary>
    /// Represents a rule for trade validation
    /// </summary>
    public class TradePeriodRule
    {
        public string RuleId { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public Action<TeamId, TeamId, List<TradeAsset>, List<TradeAsset>> ValidationAction { get; private set; }

        public TradePeriodRule(string ruleId, string description, bool isActive, 
            Action<TeamId, TeamId, List<TradeAsset>, List<TradeAsset>> validationAction)
        {
            RuleId = ruleId;
            Description = description;
            IsActive = isActive;
            ValidationAction = validationAction;
        }

        public void ValidateTrade(TeamId team1, TeamId team2, List<TradeAsset> team1Assets, List<TradeAsset> team2Assets)
        {
            if (IsActive)
            {
                ValidationAction(team1, team2, team1Assets, team2Assets);
            }
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