using System;
using System.ComponentModel.DataAnnotations;

namespace Surescripts.WebUI.Models
{
    public class Calculation
    {
        [Required]
        public string Id { get; set; }
        public string Worker { get; set; }
        public string Input { get; set; }
        public string CallbackUrl { get; set; }
        public DateTime StartTime { get; set; }
        public string Output { get; set; }
        public int Status { get; set; }
        public DateTime? StatusTime { get; set; }
    }
}