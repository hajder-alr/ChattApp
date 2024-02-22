using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Database.Models
{
    public class User
    {
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}