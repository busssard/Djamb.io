using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Djambi.Api.Db.Model
{
    [Table("MagicLinks")]
    public class MagicLinkSqlModel
    {
        [Key]
        [Required]
        public int MagicLinkId { get; set; }

        [Required]
        [StringLength(64)]
        public string Token { get; set; }

        [Required]
        public int UserId { get; set; }
        public UserSqlModel User { get; set; }

        [Required]
        [StringLength(254)]
        public string Email { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        [Required]
        public DateTime ExpiresOn { get; set; }

        public DateTime? UsedOn { get; set; }
    }
}
