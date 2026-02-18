using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Djambi.Api.Enums;

namespace Djambi.Api.Db.Model
{
    [Table("Games")]
    public class GameSqlModel
    {
        [Key]
        [Required]
        public int GameId { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }
        public UserSqlModel CreatedByUser { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        [Required]
        public GameStatus GameStatusId { get; set; }

        public IList<PlayerSqlModel> Players { get; set; } = new List<PlayerSqlModel>();

        public IList<EventSqlModel> Events { get; set; } = new List<EventSqlModel>();

        public string Description { get; set; }

        [Required]
        public byte RegionCount { get; set; }

        [Required]
        public bool AllowGuests { get; set; }

        [Required]
        public bool IsPublic { get; set; }

        [Required]
        public byte RulesetKindId { get; set; } = 1; // Default: TotalWar

        [StringLength(12)]
        public string InviteCode { get; set; }

        public int? TurnTimeLimitSeconds { get; set; }

        // Nullable — JSON map of playerId -> botName (e.g. {"3":"minimax","4":"random"})
        public string BotAssignmentsJson { get; set; }

        // Nullable
        public string TurnCycleJson { get; set; }

        // Nullable
        public string PiecesJson { get; set; }

        // Nullable
        public string CurrentTurnJson { get; set; }
    }
}
