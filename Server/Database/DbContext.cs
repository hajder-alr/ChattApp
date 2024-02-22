using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database
{
    internal class DbContext
    {
        private DbContext? instance = null;

        private DbContext(string connectionString)
        {

        }
    }
}
