using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents the AFL Trade Period with all its trades and rules
    /// </summary>
    public class TradePeriod
    {
        public int Id { get; private set; }
        public int SeasonYear { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public TradePeriodStatus Status { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? OpenedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }
        public TimeSpan? ExtensionTime { get; private set; } // For deadline day extensions
        
        private readonly List<Trade> _trades;
        private readonly List<TradeOffer> _pendingOffers;
        private readonly List<TradePeriodRule> _rules;
        
        public IReadOnlyList<Trade> Trades => _trades.AsReadOnly();
        public IReadOnlyList<TradeOffer> PendingOffers => _pendingOffers.AsReadOnly();
        public IReadOnlyList<TradePeriodRule> Rules => _rules.AsReadOnly();

        // Statistics
        public int TotalTrades => _trades.Count;
        public int CompletedTrades => _trades.Count(t => t.Status == TradeStatus.Completed);
        public int RejectedTrades => _trades.Count(t => t.Status == TradeStatus.Rejected);
        public int WithdrawnTrades => _trades.Count(t => t.Status == TradeStatus.Withdrawn);

        // Default constructor for EF Core
        protected TradePeriod()
        {
            _trades = new List<Trade>();
            _pendingOffers = new List<TradeOffer>();
            _rules = new List<TradePeriodRule>();
        }

        public TradePeriod(int seasonYear, DateTime startDate, DateTime endDate, List<TradePeriodRule>? rules = null)
        {
            SeasonYear = seasonYear;
            StartDate = startDate;
            EndDate = endDate;
            Status = TradePeriodStatus.NotStarted;
            IsActive = false;
            
            _trades = new List<Trade>();
            _pendingOffers = new List<TradeOffer>();
            _rules = rules ?? CreateDefaultRules();
        }

        /// <summary>
        /// Opens the trade period
        /// </summary>
        public void Open()
        {
            if (Status != TradePeriodStatus.NotStarted)
                throw new InvalidOperationException($"Cannot open trade period in {Status} status");

            if (DateTime.UtcNow < StartDate)
                throw new InvalidOperationException("Cannot open trade period before start date");

            Status = TradePeriodStatus.Open;
            IsActive = true;
            OpenedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Closes the trade period
        /// </summary>
        public void Close()
        {
            if (Status != TradePeriodStatus.Open)
                throw new InvalidOperationException($"Cannot close trade period in {Status} status");

            Status = TradePeriodStatus.Closed;
            IsActive = false;
            ClosedAt = DateTime.UtcNow;

            // Reject all pending offers
            foreach (var offer in _pendingOffers.Where(o => o.Status == TradeOfferStatus.Pending))
            {
                offer.Reject("Trade period closed");
            }
        }

        /// <summary>
        /// Extends the trade period deadline (typically on deadline day)
        /// </summary>
        public void ExtendDeadline(TimeSpan extension)
        {
            if (Status != TradePeriodStatus.Open)
                throw new InvalidOperationException("Can only extend deadline during open trade period");

            if (extension.TotalMinutes <= 0 || extension.TotalHours > 24)
                throw new ArgumentException("Extension must be between 1 minute and 24 hours");

            EndDate = EndDate.Add(extension);
            ExtensionTime = (ExtensionTime ?? TimeSpan.Zero).Add(extension);
        }

        /// <summary>
        /// Submits a new trade offer
        /// </summary>
        public TradeOffer SubmitTradeOffer(TeamId proposingTeam, TeamId receivingTeam, List<TradeAsset> offeredAssets, List<TradeAsset> requestedAssets)
        {
            if (!IsActive)
                throw new InvalidOperationException("Trade period is not active");

            ValidateTradeOffer(proposingTeam, receivingTeam, offeredAssets, requestedAssets);

            var tradeOffer = new TradeOffer(
                proposingTeam, 
                receivingTeam, 
                offeredAssets, 
                requestedAssets,
                DateTime.UtcNow.Add(GetTradeOfferExpiration()));

            _pendingOffers.Add(tradeOffer);
            return tradeOffer;
        }

        /// <summary>
        /// Accepts a trade offer, converting it to a completed trade
        /// </summary>
        public Trade AcceptTradeOffer(int offerId)
        {
            var offer = _pendingOffers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
                throw new ArgumentException("Trade offer not found");

            if (offer.Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot accept trade offer in {offer.Status} status");

            if (offer.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Trade offer has expired");

            // Validate trade is still possible (assets still available, etc.)
            ValidateTradeExecution(offer);

            // Accept the offer
            offer.Accept();

            // Create completed trade
            var trade = new Trade(
                offer.ProposingTeam,
                offer.ReceivingTeam,
                offer.OfferedAssets.ToList(),
                offer.RequestedAssets.ToList(),
                DateTime.UtcNow);

            trade.Complete();
            _trades.Add(trade);

            // Remove from pending offers
            _pendingOffers.Remove(offer);

            return trade;
        }

        /// <summary>
        /// Rejects a trade offer
        /// </summary>
        public void RejectTradeOffer(int offerId, string reason = "")
        {
            var offer = _pendingOffers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
                throw new ArgumentException("Trade offer not found");

            if (offer.Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot reject trade offer in {offer.Status} status");

            offer.Reject(reason);
            _pendingOffers.Remove(offer);
        }

        /// <summary>
        /// Withdraws a trade offer (by the proposing team)
        /// </summary>
        public void WithdrawTradeOffer(int offerId, TeamId teamId)
        {
            var offer = _pendingOffers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
                throw new ArgumentException("Trade offer not found");

            if (offer.ProposingTeam != teamId)
                throw new UnauthorizedAccessException("Only the proposing team can withdraw the offer");

            if (offer.Status != TradeOfferStatus.Pending)
                throw new InvalidOperationException($"Cannot withdraw trade offer in {offer.Status} status");

            offer.Withdraw();
            _pendingOffers.Remove(offer);
        }

        /// <summary>
        /// Gets all trades for a specific team
        /// </summary>
        public IEnumerable<Trade> GetTradesForTeam(TeamId teamId)
        {
            return _trades.Where(t => t.Team1 == teamId || t.Team2 == teamId);
        }

        /// <summary>
        /// Gets all pending offers for a specific team
        /// </summary>
        public IEnumerable<TradeOffer> GetPendingOffersForTeam(TeamId teamId)
        {
            return _pendingOffers.Where(o => o.ProposingTeam == teamId || o.ReceivingTeam == teamId);
        }

        /// <summary>
        /// Gets trade statistics
        /// </summary>
        public TradePeriodStatistics GetStatistics()
        {
            return new TradePeriodStatistics
            {
                TotalOffers = _pendingOffers.Count + _trades.Count,
                CompletedTrades = CompletedTrades,
                RejectedOffers = RejectedTrades + _pendingOffers.Count(o => o.Status == TradeOfferStatus.Rejected),
                WithdrawnOffers = WithdrawnTrades + _pendingOffers.Count(o => o.Status == TradeOfferStatus.Withdrawn),
                PlayersTraded = _trades.SelectMany(t => t.GetAllAssets()).Count(a => a.Type == TradeAssetType.Player),
                PicksTraded = _trades.SelectMany(t => t.GetAllAssets()).Count(a => a.Type == TradeAssetType.DraftPick),
                AverageTradeValue = CalculateAverageTradeValue(),
                MostActiveTradingTeam = GetMostActiveTeam(),
                DeadlineDayTrades = _trades.Count(t => t.CompletedAt?.Date == EndDate.Date)
            };
        }

        /// <summary>
        /// Checks if trading is currently allowed
        /// </summary>
        public bool IsTradingAllowed()
        {
            return IsActive && 
                   Status == TradePeriodStatus.Open && 
                   DateTime.UtcNow >= StartDate && 
                   DateTime.UtcNow <= EndDate;
        }

        /// <summary>
        /// Gets time remaining in trade period
        /// </summary>
        public TimeSpan TimeRemaining()
        {
            if (!IsActive) return TimeSpan.Zero;
            
            var remaining = EndDate - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private void ValidateTradeOffer(TeamId proposingTeam, TeamId receivingTeam, List<TradeAsset> offeredAssets, List<TradeAsset> requestedAssets)
        {
            if (proposingTeam == receivingTeam)
                throw new ArgumentException("Teams cannot trade with themselves");

            if (!offeredAssets.Any() || !requestedAssets.Any())
                throw new ArgumentException("Both teams must offer something in the trade");

            // Check trade rules
            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.ValidateTrade(proposingTeam, receivingTeam, offeredAssets, requestedAssets);
            }
        }

        private void ValidateTradeExecution(TradeOffer offer)
        {
            // Additional validation before executing the trade
            // Check if assets are still available, players haven't retired, etc.
            foreach (var asset in offer.OfferedAssets.Concat(offer.RequestedAssets))
            {
                if (!asset.IsStillAvailable())
                {
                    throw new InvalidOperationException($"Asset {asset.Description} is no longer available");
                }
            }
        }

        private TimeSpan GetTradeOfferExpiration()
        {
            // Standard 48-hour expiration, or until trade period ends, whichever is sooner
            var standardExpiration = TimeSpan.FromHours(48);
            var timeUntilClose = EndDate - DateTime.UtcNow;
            
            return standardExpiration < timeUntilClose ? standardExpiration : timeUntilClose;
        }

        private decimal CalculateAverageTradeValue()
        {
            if (!_trades.Any()) return 0;

            var totalValue = _trades.Sum(t => t.CalculateTradeValue());
            return totalValue / _trades.Count;
        }

        private TeamId GetMostActiveTeam()
        {
            var teamTradeCount = new Dictionary<TeamId, int>();
            
            foreach (var trade in _trades)
            {
                teamTradeCount[trade.Team1] = teamTradeCount.GetValueOrDefault(trade.Team1) + 1;
                teamTradeCount[trade.Team2] = teamTradeCount.GetValueOrDefault(trade.Team2) + 1;
            }

            return teamTradeCount.Any() 
                ? teamTradeCount.OrderByDescending(kvp => kvp.Value).First().Key 
                : TeamId.None;
        }

        private static List<TradePeriodRule> CreateDefaultRules()
        {
            return new List<TradePeriodRule>
            {
                new TradePeriodRule("ListSizeLimit", "Teams must maintain minimum list size", true,
                    (team1, team2, offered, requested) => ValidateListSizes(team1, team2, offered, requested)),
                new TradePeriodRule("DraftPickLimit", "Cannot trade future first-round picks beyond 2 years", true,
                    (team1, team2, offered, requested) => ValidateDraftPickRestrictions(offered, requested)),
                new TradePeriodRule("PlayerEligibility", "Players must be eligible for trading", true,
                    (team1, team2, offered, requested) => ValidatePlayerEligibility(offered, requested))
            };
        }

        private static void ValidateListSizes(TeamId team1, TeamId team2, List<TradeAsset> offered, List<TradeAsset> requested)
        {
            // Implementation would check minimum list sizes
        }

        private static void ValidateDraftPickRestrictions(List<TradeAsset> offered, List<TradeAsset> requested)
        {
            // Implementation would check draft pick trading rules
        }

        private static void ValidatePlayerEligibility(List<TradeAsset> offered, List<TradeAsset> requested)
        {
            // Implementation would check player eligibility (contracts, etc.)
        }
    }

    /// <summary>
    /// Statistics for the trade period
    /// </summary>
    public class TradePeriodStatistics
    {
        public int TotalOffers { get; set; }
        public int CompletedTrades { get; set; }
        public int RejectedOffers { get; set; }
        public int WithdrawnOffers { get; set; }
        public int PlayersTraded { get; set; }
        public int PicksTraded { get; set; }
        public decimal AverageTradeValue { get; set; }
        public TeamId MostActiveTradingTeam { get; set; }
        public int DeadlineDayTrades { get; set; }
    }
}