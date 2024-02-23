using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Class structure that defines received json from client
class Message
{
    public string? Type { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? MessageContents { get; set; }
    public string? Sender { get; set; }
    public string? Recipient { get; set; }
}