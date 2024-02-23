using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string Contents { get; set; }
        public string Status { get; set; }
    }
}