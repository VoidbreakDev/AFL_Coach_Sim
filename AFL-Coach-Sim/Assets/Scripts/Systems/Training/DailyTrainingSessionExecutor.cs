using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Core execution framework for daily training sessions
    /// Handles multi-program execution, participant tracking, injury risk, and development outcomes
    /// </summary>
    public class DailyTrainingSessionExecutor : MonoBehaviour
    {
        [Header("Execution Configuration")]
        // [SerializeField] private float sessionUpdateIntervalSeconds = 0.1f; // TODO: Implement session update interval
        [SerializeField] private bool enableRealTimeSimulation = false;
        [SerializeField] private float realTimeSpeedMultiplier = 60f; // 1 minute = 1 second
        
        [Header("Risk Management")]
        [SerializeField] private float baseInjuryRiskMultiplier = 1.0f;
        [SerializeField] private float fatigueInjuryThreshold = 75f;
        // [SerializeField] private bool enableAutomaticIntensityReduction = true; // TODO: Implement automatic intensity reduction
        
        [Header("Performance Tracking")]
        // [SerializeField] private bool trackDetailedMetrics = true; // TODO: Implement detailed metrics tracking
        [SerializeField] private bool logExecutionDetails = false;
        
        // Dependencies (inject via Initialize method)
        private InjuryManager injuryManager;
        private FatigueModel fatigueModel;
        
        // Current execution state
        private Dictionary<int, TrainingSessionExecution> activeSessions = new Dictionary<int, TrainingSessionExecution>();
        private Dictionary<int, PlayerSessionState> playerStates = new Dictionary<int, PlayerSessionState>();
        
        // Events for external systems
        public event System.Action<TrainingSessionExecution> OnSessionStarted;
        public event System.Action<TrainingSessionExecution> OnSessionCompleted;
        public event System.Action<TrainingSessionExecution> OnSessionCancelled;
        public event System.Action<int, PlayerSessionResult> OnPlayerSessionResult;
        public event System.Action<int, Injury> OnTrainingInjuryOccurred;
        public event System.Action<TrainingSessionExecution, string> OnSessionStatusChanged;
        
        private void Update()
        {
            if (enableRealTimeSimulation)
            {
                ProcessActiveSessionsRealTime();
            }
        }
        
        /// <summary>
        /// Initialize the executor with required dependencies
        /// </summary>
        public void Initialize(InjuryManager injuryManager = null, FatigueModel fatigueModel = null)
        {
            this.injuryManager = injuryManager;
            this.fatigueModel = fatigueModel;
            
            Debug.Log($"[DailyTrainingExecutor] Initialized with injury management: {injuryManager != null}, fatigue model: {fatigueModel != null}");
        }
        
        /// <summary>
        /// Execute a daily training session with all its components
        /// </summary>
        public TrainingSessionExecution ExecuteSession(DailyTrainingSession session, List<Player> participants)
        {
            if (session == null)
            {
                Debug.LogError("[DailyTrainingExecutor] Cannot execute null session");
                return null;
            }
            
            if (session.Status != TrainingSessionStatus.Scheduled)
            {
                Debug.LogWarning($"[DailyTrainingExecutor] Session {session.SessionId} is not scheduled (status: {session.Status})");
                return null;
            }
            
            // Create execution context
            var execution = new TrainingSessionExecution
            {
                SessionId = session.SessionId,
                Session = session,
                StartTime = DateTime.Now,
                Status = SessionExecutionStatus.Preparing,
                ParticipantResults = new Dictionary<int, PlayerSessionResult>(),
                ComponentResults = new List<ComponentExecutionResult>(),
                SessionMetrics = new SessionExecutionMetrics()
            };
            
            // Pre-execution preparation
            var prepResult = PrepareSessionExecution(execution, participants);
            if (!prepResult.Success)
            {
                execution.Status = SessionExecutionStatus.Failed;
                execution.CompletionMessage = prepResult.Message;
                OnSessionCancelled?.Invoke(execution);
                return execution;
            }
            
            execution.EligibleParticipants = prepResult.EligibleParticipants;
            activeSessions[session.SessionId] = execution;
            
            // Start execution
            if (enableRealTimeSimulation)
            {
                StartRealtimeExecution(execution);
            }
            else
            {
                ExecuteSessionImmediate(execution);
            }
            
            return execution;
        }
        
        /// <summary>
        /// Prepare session for execution - validate participants, check conditions
        /// </summary>
        private SessionPreparationResult PrepareSessionExecution(TrainingSessionExecution execution, List<Player> participants)
        {
            var result = new SessionPreparationResult();
            
            Debug.Log($"[DailyTrainingExecutor] Preparing session: {execution.Session.SessionName} with {participants.Count} potential participants");
            
            // Filter participants based on availability, injury status, and fatigue
            var eligibleParticipants = new List<Player>();
            var participantIssues = new List<string>();
            
            foreach (var player in participants)
            {
                var eligibility = CheckPlayerEligibility(player, execution.Session);
                if (eligibility.IsEligible)
                {
                    eligibleParticipants.Add(player);
                    
                    // Initialize player state for this session
                    playerStates[int.Parse(player.Id)] = new PlayerSessionState
                    {
                        PlayerId = int.Parse(player.Id),
                        Player = player,
                        SessionId = execution.SessionId,
                        StartingFatigue = GetPlayerFatigue(player),
                        StartingCondition = GetPlayerCondition(player),
                        ComponentsCompleted = new List<int>(),
                        TotalLoadAccumulated = 0f,
                        EffectivenessModifiers = new Dictionary<string, float>()
                    };
                }
                else
                {
                    participantIssues.Add($"{player.Name}: {eligibility.Reason}");
                }
            }
            
            // Check if we have minimum participants
            var minParticipants = CalculateMinimumParticipants(execution.Session);
            if (eligibleParticipants.Count < minParticipants)
            {
                result.Success = false;
                result.Message = $"Insufficient participants: {eligibleParticipants.Count}/{minParticipants} minimum required";
                result.Details = participantIssues;
                return result;
            }
            
            result.Success = true;
            result.EligibleParticipants = eligibleParticipants;
            result.Message = $"Session prepared with {eligibleParticipants.Count} participants";
            result.Details = participantIssues;
            
            if (logExecutionDetails && participantIssues.Any())
            {
                Debug.Log($"[DailyTrainingExecutor] Participant issues: {string.Join(", ", participantIssues)}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Execute session immediately (non-real-time)
        /// </summary>
        private void ExecuteSessionImmediate(TrainingSessionExecution execution)
        {
            execution.Status = SessionExecutionStatus.Running;
            OnSessionStarted?.Invoke(execution);
            OnSessionStatusChanged?.Invoke(execution, "Session started");
            
            try
            {
                // Execute each training component sequentially
                for (int componentIndex = 0; componentIndex < execution.Session.TrainingComponents.Count; componentIndex++)
                {
                    var component = execution.Session.TrainingComponents[componentIndex];
                    var componentResult = ExecuteTrainingComponent(execution, component, componentIndex);
                    
                    execution.ComponentResults.Add(componentResult);
                    
                    // Update player states after each component
                    UpdatePlayerStatesAfterComponent(execution, componentResult);
                    
                    // Check for early termination conditions
                    if (ShouldTerminateSession(execution))
                    {
                        break;
                    }
                }
                
                // Complete the session
                CompleteSessionExecution(execution);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyTrainingExecutor] Error executing session {execution.SessionId}: {ex.Message}");
                execution.Status = SessionExecutionStatus.Failed;
                execution.CompletionMessage = $"Execution error: {ex.Message}";
                OnSessionCancelled?.Invoke(execution);
            }
            finally
            {
                activeSessions.Remove(execution.SessionId);
            }
        }
        
        /// <summary>
        /// Start real-time execution (for detailed simulation)
        /// </summary>
        private void StartRealtimeExecution(TrainingSessionExecution execution)
        {
            execution.Status = SessionExecutionStatus.Running;
            execution.CurrentComponentIndex = 0;
            execution.ComponentStartTime = DateTime.Now;
            
            OnSessionStarted?.Invoke(execution);
            OnSessionStatusChanged?.Invoke(execution, "Real-time session started");
        }
        
        /// <summary>
        /// Process active sessions in real-time
        /// </summary>
        private void ProcessActiveSessionsRealTime()
        {
            foreach (var execution in activeSessions.Values.Where(e => e.Status == SessionExecutionStatus.Running).ToList())
            {
                ProcessRealtimeSession(execution);
            }
        }
        
        /// <summary>
        /// Process a single session in real-time
        /// </summary>
        private void ProcessRealtimeSession(TrainingSessionExecution execution)
        {
            var currentComponent = execution.Session.TrainingComponents[execution.CurrentComponentIndex];
            var componentDuration = currentComponent.Duration;
            var elapsedTime = DateTime.Now - execution.ComponentStartTime;
            
            // Check if current component is complete
            if (elapsedTime >= TimeSpan.FromSeconds(componentDuration.TotalSeconds / realTimeSpeedMultiplier))
            {
                // Complete current component
                var componentResult = ExecuteTrainingComponent(execution, currentComponent, execution.CurrentComponentIndex);
                execution.ComponentResults.Add(componentResult);
                
                UpdatePlayerStatesAfterComponent(execution, componentResult);
                
                // Move to next component or complete session
                execution.CurrentComponentIndex++;
                
                if (execution.CurrentComponentIndex >= execution.Session.TrainingComponents.Count || ShouldTerminateSession(execution))
                {
                    CompleteSessionExecution(execution);
                    activeSessions.Remove(execution.SessionId);
                }
                else
                {
                    execution.ComponentStartTime = DateTime.Now;
                    OnSessionStatusChanged?.Invoke(execution, $"Starting component {execution.CurrentComponentIndex + 1}/{execution.Session.TrainingComponents.Count}");
                }
            }
        }
        
        /// <summary>
        /// Execute a single training component
        /// </summary>
        private ComponentExecutionResult ExecuteTrainingComponent(TrainingSessionExecution execution, TrainingComponent component, int componentIndex)
        {
            var result = new ComponentExecutionResult
            {
                ComponentIndex = componentIndex,
                Component = component,
                StartTime = DateTime.Now,
                PlayerResults = new Dictionary<int, PlayerComponentResult>()
            };
            
            if (logExecutionDetails)
            {
                Debug.Log($"[DailyTrainingExecutor] Executing component {componentIndex + 1}: {component.ComponentType} ({component.Intensity})");
            }
            
            // Execute for each eligible participant
            foreach (var player in execution.EligibleParticipants)
            {
                var playerState = playerStates[int.Parse(player.Id)];
                var playerResult = ExecuteComponentForPlayer(component, player, playerState, execution.Session);
                
                result.PlayerResults[int.Parse(player.Id)] = playerResult;
                
                // Track any injuries
                if (playerResult.InjuryOccurred)
                {
                    OnTrainingInjuryOccurred?.Invoke(int.Parse(player.Id), playerResult.Injury);
                }
            }
            
            result.CompletionTime = DateTime.Now;
            result.Duration = result.CompletionTime - result.StartTime;
            
            return result;
        }
        
        /// <summary>
        /// Execute a training component for a specific player
        /// </summary>
        private PlayerComponentResult ExecuteComponentForPlayer(TrainingComponent component, Player player, PlayerSessionState playerState, DailyTrainingSession session)
        {
            var result = new PlayerComponentResult
            {
                PlayerId = int.Parse(player.Id),
                ComponentType = component.ComponentType,
                StartTime = DateTime.Now
            };
            
            try
            {
                // Calculate current effectiveness based on player state
                var effectiveness = CalculatePlayerEffectiveness(player, playerState, component, session);
                result.EffectivenessRating = effectiveness;
                
                // Check for injury risk
                var injuryRisk = CalculateInjuryRisk(player, playerState, component);
                result.InjuryRisk = injuryRisk;
                
                // Determine if injury occurs
                result.InjuryOccurred = UnityEngine.Random.Range(0f, 1f) < injuryRisk;
                
                if (result.InjuryOccurred)
                {
                    // Generate training injury
                    result.Injury = GenerateTrainingInjury(player, component, injuryRisk);
                    result.StatChanges = new PlayerStatsDelta(); // No improvement when injured
                    
                    Debug.LogWarning($"[DailyTrainingExecutor] {player.Name} injured during {component.ComponentType}: {result.Injury.Description}");
                }
                else
                {
                    // Calculate stat improvements
                    result.StatChanges = CalculateStatDevelopment(player, component, effectiveness);
                    
                    // Apply improvements to player
                    if (result.StatChanges != null)
                    {
                        result.StatChanges.ApplyTo(player.Stats);
                    }
                }
                
                // Calculate fatigue impact
                result.FatigueIncrease = CalculateFatigueIncrease(player, component, effectiveness);
                
                // Update player state
                playerState.TotalLoadAccumulated += component.LoadMultiplier;
                playerState.ComponentsCompleted.Add(0); // Would use actual component ID in real implementation
                
                result.LoadContribution = component.LoadMultiplier;
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyTrainingExecutor] Error executing component for {player.Name}: {ex.Message}");
                result.StatChanges = new PlayerStatsDelta();
                result.ErrorOccurred = true;
                result.ErrorMessage = ex.Message;
            }
            
            result.CompletionTime = DateTime.Now;
            return result;
        }
        
        /// <summary>
        /// Complete session execution and generate final results
        /// </summary>
        private void CompleteSessionExecution(TrainingSessionExecution execution)
        {
            execution.Status = SessionExecutionStatus.Completed;
            execution.EndTime = DateTime.Now;
            execution.ActualDuration = execution.EndTime - execution.StartTime;
            
            // Generate final results for each participant
            foreach (var player in execution.EligibleParticipants)
            {
                var playerState = playerStates[int.Parse(player.Id)];
                var sessionResult = GeneratePlayerSessionResult(player, playerState, execution);
                
                execution.ParticipantResults[int.Parse(player.Id)] = sessionResult;
                OnPlayerSessionResult?.Invoke(int.Parse(player.Id), sessionResult);
            }
            
            // Calculate session-wide metrics
            CalculateSessionMetrics(execution);
            
            // Update the original session object
            execution.Session.Status = TrainingSessionStatus.Completed;
            execution.Session.ActualParticipants = execution.EligibleParticipants.Select(p => int.Parse(p.Id)).ToList();
            execution.Session.CompletionTime = execution.EndTime;
            
            execution.CompletionMessage = $"Session completed successfully with {execution.EligibleParticipants.Count} participants";
            
            OnSessionCompleted?.Invoke(execution);
            OnSessionStatusChanged?.Invoke(execution, "Session completed");
            
            if (logExecutionDetails)
            {
                Debug.Log($"[DailyTrainingExecutor] Completed session {execution.SessionId}: {execution.CompletionMessage}");
            }
            
            // Clean up player states
            foreach (var player in execution.EligibleParticipants)
            {
                playerStates.Remove(int.Parse(player.Id));
            }
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Check if a player is eligible to participate in the session
        /// </summary>
        private PlayerEligibilityResult CheckPlayerEligibility(Player player, DailyTrainingSession session)
        {
            var result = new PlayerEligibilityResult { PlayerId = int.Parse(player.Id) };
            
            // Check injury status
            if (injuryManager != null && !injuryManager.CanPlayerTrain(int.Parse(player.Id)))
            {
                result.IsEligible = false;
                result.Reason = "Currently injured";
                return result;
            }
            
            // Check fatigue levels
            var fatigue = GetPlayerFatigue(player);
            if (fatigue > fatigueInjuryThreshold)
            {
                result.IsEligible = false;
                result.Reason = $"Fatigue too high ({fatigue:F1}%)";
                return result;
            }
            
            // Check training load limits (would integrate with weekly schedule manager)
            var currentLoad = GetPlayerCurrentWeeklyLoad(player);
            var sessionLoad = session.GetSessionLoad();
            if (currentLoad + sessionLoad > GetPlayerMaxWeeklyLoad(player))
            {
                result.IsEligible = false;
                result.Reason = "Weekly training load limit reached";
                return result;
            }
            
            result.IsEligible = true;
            result.Reason = "Eligible";
            return result;
        }
        
        /// <summary>
        /// Calculate minimum participants needed for the session
        /// </summary>
        private int CalculateMinimumParticipants(DailyTrainingSession session)
        {
            // Minimum participants based on session type
            return session.SessionType switch
            {
                DailySessionType.Main => 8,           // Need decent squad for main training
                DailySessionType.Tactical => 6,       // Tactical work needs some numbers
                DailySessionType.SkillsOnly => 3,     // Skills can work with fewer
                DailySessionType.Recovery => 1,       // Recovery is individual
                _ => 5                                 // Default minimum
            };
        }
        
        /// <summary>
        /// Calculate player effectiveness for this component
        /// </summary>
        private float CalculatePlayerEffectiveness(Player player, PlayerSessionState playerState, TrainingComponent component, DailyTrainingSession session)
        {
            float effectiveness = 1.0f;
            
            // Base effectiveness from training program
            if (component.Program != null)
            {
                effectiveness = component.Program.GetEffectivenessMultiplier(player);
            }
            
            // Apply fatigue penalty
            var fatigue = playerState.StartingFatigue + (playerState.TotalLoadAccumulated * 2f); // Accumulated fatigue
            if (fatigue > 50f)
            {
                effectiveness *= (100f - fatigue) / 50f; // Reduce effectiveness as fatigue increases
            }
            
            // Apply condition bonus/penalty
            var condition = GetPlayerCondition(player);
            effectiveness *= (condition / 100f);
            
            // Apply intensity modifier
            var intensityMultiplier = component.Intensity switch
            {
                TrainingIntensity.Light => 0.8f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.2f,
                TrainingIntensity.VeryHigh => 1.4f,
                _ => 1.0f
            };
            effectiveness *= intensityMultiplier;
            
            // Apply component-specific modifiers
            effectiveness *= GetComponentSpecificMultiplier(player, component);
            
            return Mathf.Clamp(effectiveness, 0.1f, 2.0f);
        }
        
        /// <summary>
        /// Calculate injury risk for this player and component
        /// </summary>
        private float CalculateInjuryRisk(Player player, PlayerSessionState playerState, TrainingComponent component)
        {
            float baseRisk = 0.02f; // 2% base risk
            
            // Apply component risk modifiers
            baseRisk *= component.Intensity switch
            {
                TrainingIntensity.Light => 0.5f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.5f,
                TrainingIntensity.VeryHigh => 2.0f,
                _ => 1.0f
            };
            
            // Apply player age risk
            if (player.Age > 30)
                baseRisk *= 1.3f;
            else if (player.Age < 20)
                baseRisk *= 1.1f;
            
            // Apply fatigue risk
            var currentFatigue = playerState.StartingFatigue + (playerState.TotalLoadAccumulated * 2f);
            if (currentFatigue > fatigueInjuryThreshold)
            {
                baseRisk *= 1.5f + ((currentFatigue - fatigueInjuryThreshold) / 100f);
            }
            
            // Apply player durability (if available)
            var durability = GetPlayerDurability(player);
            if (durability > 0)
            {
                baseRisk *= (100f - durability) / 100f + 0.5f;
            }
            
            // Apply global multiplier
            baseRisk *= baseInjuryRiskMultiplier;
            
            return Mathf.Clamp(baseRisk, 0.001f, 0.2f); // Cap at 20% max risk
        }
        
        /// <summary>
        /// Calculate stat development for this component
        /// </summary>
        private PlayerStatsDelta CalculateStatDevelopment(Player player, TrainingComponent component, float effectiveness)
        {
            if (component.Program == null)
                return new PlayerStatsDelta();
                
            // Use existing development system
            var development = player.Development?.CalculateDevelopment(player, component.Program, effectiveness) ?? new PlayerStatsDelta();
            
            // Apply component-specific scaling
            var componentScaling = component.LoadMultiplier / 15f; // Normalize to typical load
            return ScaleStatsDelta(development, componentScaling);
        }
        
        /// <summary>
        /// Calculate fatigue increase from this component
        /// </summary>
        private float CalculateFatigueIncrease(Player player, TrainingComponent component, float effectiveness)
        {
            float baseFatigue = component.LoadMultiplier * 0.5f; // Base fatigue from load
            
            // Intensity increases fatigue
            baseFatigue *= component.Intensity switch
            {
                TrainingIntensity.Light => 0.7f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.3f,
                TrainingIntensity.VeryHigh => 1.6f,
                _ => 1.0f
            };
            
            // Player fitness affects fatigue accumulation
            var fitness = player.Stats?.Stamina ?? 70f;
            baseFatigue *= (100f - fitness) / 100f + 0.5f;
            
            return Mathf.Clamp(baseFatigue, 0.5f, 15f);
        }
        
        /// <summary>
        /// Generate a training injury
        /// </summary>
        private Injury GenerateTrainingInjury(Player player, TrainingComponent component, float injuryRisk)
        {
            if (injuryManager != null)
            {
                // Use the injury manager to generate appropriate injury
                return injuryManager.RecordTrainingInjury(int.Parse(player.Id), injuryRisk, player.Age, GetPlayerDurability(player));
            }
            
            // Fallback basic injury generation
            var severities = new[] { InjurySeverity.Niggle, InjurySeverity.Minor, InjurySeverity.Moderate };
            var types = new[] { InjuryType.Muscle, InjuryType.Joint, InjuryType.Other };
            
            return new Injury(
                playerId: int.Parse(player.Id),
                type: types[UnityEngine.Random.Range(0, types.Length)],
                severity: severities[UnityEngine.Random.Range(0, severities.Length)],
                source: InjurySource.Training,
                description: $"Training {component.ComponentType.ToString().ToLower()} injury"
            );
        }
        
        /// <summary>
        /// Generate final session result for a player
        /// </summary>
        private PlayerSessionResult GeneratePlayerSessionResult(Player player, PlayerSessionState playerState, TrainingSessionExecution execution)
        {
            var componentResults = execution.ComponentResults
                .Where(cr => cr.PlayerResults.ContainsKey(int.Parse(player.Id)))
                .Select(cr => cr.PlayerResults[int.Parse(player.Id)])
                .ToList();
                
            var result = new PlayerSessionResult
            {
                PlayerId = int.Parse(player.Id),
                SessionId = execution.SessionId,
                PlayerName = player.Name,
                ComponentResults = componentResults,
                TotalStatChanges = componentResults
                    .Where(cr => cr.StatChanges != null)
                    .Aggregate(new PlayerStatsDelta(), (acc, cr) => AddStatDeltas(acc, cr.StatChanges)),
                TotalFatigueIncrease = componentResults.Sum(cr => cr.FatigueIncrease),
                TotalLoadContribution = componentResults.Sum(cr => cr.LoadContribution),
                AverageEffectiveness = componentResults.Any() ? componentResults.Average(cr => cr.EffectivenessRating) : 0f,
                TotalInjuries = componentResults.Count(cr => cr.InjuryOccurred),
                SessionDuration = execution.ActualDuration ?? TimeSpan.Zero
            };
            
            return result;
        }
        
        /// <summary>
        /// Calculate session-wide metrics
        /// </summary>
        private void CalculateSessionMetrics(TrainingSessionExecution execution)
        {
            var metrics = execution.SessionMetrics;
            
            metrics.TotalParticipants = execution.EligibleParticipants.Count;
            metrics.ComponentsExecuted = execution.ComponentResults.Count;
            metrics.TotalInjuries = execution.ComponentResults.Sum(cr => cr.PlayerResults.Values.Count(pr => pr.InjuryOccurred));
            metrics.AverageEffectiveness = execution.ComponentResults
                .SelectMany(cr => cr.PlayerResults.Values)
                .Average(pr => pr.EffectivenessRating);
            metrics.TotalTrainingLoad = execution.ComponentResults
                .SelectMany(cr => cr.PlayerResults.Values)
                .Sum(pr => pr.LoadContribution);
            metrics.ExecutionDuration = execution.ActualDuration ?? TimeSpan.Zero;
            
            // Calculate success rate
            var totalPlayerComponents = execution.ComponentResults.Sum(cr => cr.PlayerResults.Count);
            var successfulComponents = execution.ComponentResults.Sum(cr => cr.PlayerResults.Values.Count(pr => !pr.InjuryOccurred && !pr.ErrorOccurred));
            metrics.SuccessRate = totalPlayerComponents > 0 ? (float)successfulComponents / totalPlayerComponents : 0f;
        }
        
        /// <summary>
        /// Check if session should be terminated early
        /// </summary>
        private bool ShouldTerminateSession(TrainingSessionExecution execution)
        {
            // Terminate if too many injuries
            var recentInjuries = execution.ComponentResults
                .TakeLast(2)
                .Sum(cr => cr.PlayerResults.Values.Count(pr => pr.InjuryOccurred));
                
            if (recentInjuries > execution.EligibleParticipants.Count / 3)
            {
                execution.CompletionMessage = "Session terminated due to high injury rate";
                return true;
            }
            
            // Terminate if not enough healthy participants remaining
            var healthyParticipants = execution.EligibleParticipants.Count(p => 
                !execution.ComponentResults.Any(cr => 
                    cr.PlayerResults.ContainsKey(int.Parse(p.Id)) && cr.PlayerResults[int.Parse(p.Id)].InjuryOccurred));
                    
            if (healthyParticipants < CalculateMinimumParticipants(execution.Session) / 2)
            {
                execution.CompletionMessage = "Session terminated due to insufficient healthy participants";
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Update player states after component execution
        /// </summary>
        private void UpdatePlayerStatesAfterComponent(TrainingSessionExecution execution, ComponentExecutionResult componentResult)
        {
            foreach (var kvp in componentResult.PlayerResults)
            {
                var playerId = kvp.Key;
                var result = kvp.Value;
                
                if (playerStates.ContainsKey(playerId))
                {
                    var state = playerStates[playerId];
                    state.TotalLoadAccumulated += result.LoadContribution;
                    
                    // Update fatigue if we have fatigue model integration
                    if (fatigueModel != null)
                    {
                        // Would update fatigue model here
                    }
                }
            }
        }
        
        #region Player Attribute Helpers
        
        private float GetPlayerFatigue(Player player)
        {
            // Would integrate with actual fatigue system
            return UnityEngine.Random.Range(20f, 60f); // Placeholder
        }
        
        private float GetPlayerCondition(Player player)
        {
            return player.Stamina; // Player.Stamina is already a float, not nullable
        }
        
        private int GetPlayerDurability(Player player)
        {
            // Durability can be estimated from player stats (average of physical attributes)
            return Mathf.RoundToInt((player.Stats.Stamina + player.Stats.Tackling + player.Stats.Speed) / 3f);
        }
        
        /// <summary>
        /// Scales a PlayerStatsDelta by a multiplier
        /// </summary>
        private PlayerStatsDelta ScaleStatsDelta(PlayerStatsDelta delta, float multiplier)
        {
            return new PlayerStatsDelta
            {
                Kicking = delta.Kicking * multiplier,
                Handballing = delta.Handballing * multiplier,
                Tackling = delta.Tackling * multiplier,
                Speed = delta.Speed * multiplier,
                Stamina = delta.Stamina * multiplier,
                Knowledge = delta.Knowledge * multiplier,
                Playmaking = delta.Playmaking * multiplier
            };
        }
        
        /// <summary>
        /// Adds two PlayerStatsDelta objects together
        /// </summary>
        private PlayerStatsDelta AddStatDeltas(PlayerStatsDelta a, PlayerStatsDelta b)
        {
            return new PlayerStatsDelta
            {
                Kicking = a.Kicking + b.Kicking,
                Handballing = a.Handballing + b.Handballing,
                Tackling = a.Tackling + b.Tackling,
                Speed = a.Speed + b.Speed,
                Stamina = a.Stamina + b.Stamina,
                Knowledge = a.Knowledge + b.Knowledge,
                Playmaking = a.Playmaking + b.Playmaking
            };
        }
        
        private float GetPlayerCurrentWeeklyLoad(Player player)
        {
            // Would integrate with weekly schedule manager
            return 0f; // Placeholder
        }
        
        private float GetPlayerMaxWeeklyLoad(Player player)
        {
            return 100f; // Default max load
        }
        
        private float GetComponentSpecificMultiplier(Player player, TrainingComponent component)
        {
            // Apply position-specific multipliers for different component types
            return component.ComponentType switch
            {
                TrainingComponentType.Skills when player.Role == PlayerRole.Centre => 1.2f,
                TrainingComponentType.Fitness when player.Role == PlayerRole.Wing => 1.3f,
                TrainingComponentType.Tactical when player.Role == PlayerRole.FullBack => 1.1f,
                _ => 1.0f
            };
        }
        
        #endregion
        
        #endregion
        
        /// <summary>
        /// Get current execution status for a session
        /// </summary>
        public TrainingSessionExecution GetSessionExecution(int sessionId)
        {
            activeSessions.TryGetValue(sessionId, out var execution);
            return execution;
        }
        
        /// <summary>
        /// Get all currently active sessions
        /// </summary>
        public List<TrainingSessionExecution> GetActiveSessions()
        {
            return activeSessions.Values.ToList();
        }
        
        /// <summary>
        /// Cancel an active session
        /// </summary>
        public bool CancelSession(int sessionId, string reason = "Cancelled by request")
        {
            if (activeSessions.TryGetValue(sessionId, out var execution))
            {
                execution.Status = SessionExecutionStatus.Cancelled;
                execution.CompletionMessage = reason;
                execution.EndTime = DateTime.Now;
                
                OnSessionCancelled?.Invoke(execution);
                OnSessionStatusChanged?.Invoke(execution, $"Cancelled: {reason}");
                
                activeSessions.Remove(sessionId);
                
                // Clean up player states
                foreach (var player in execution.EligibleParticipants ?? new List<Player>())
                {
                    playerStates.Remove(int.Parse(player.Id));
                }
                
                return true;
            }
            
            return false;
        }
        
        private void OnDestroy()
        {
            // Clean up any remaining active sessions
            foreach (var execution in activeSessions.Values.ToList())
            {
                CancelSession(execution.SessionId, "System shutdown");
            }
        }
    }
}