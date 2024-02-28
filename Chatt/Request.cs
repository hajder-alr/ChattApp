using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Class structure that defines received json from client
class Request
{
    public string? Type { get; set; }
    public object? Contents { get; set; }
}