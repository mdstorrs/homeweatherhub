using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class MyData
    {

        public static string ConnectionString
        {
            get
            {
//#if DEBUG
                return "Data Source=swh14.conetix.com.au;Initial Catalog=storrs;Persist Security Info=True;User ID=storrs;Password=eO1pw2_6;Connection Timeout=60;TrustServerCertificate=True;";
//#else
//                return "Data Source=swh14.conetix.com.au;Initial Catalog=storrs;Persist Security Info=True;User ID=storrs;Password=eO1pw2_6;Connection Timeout=60";
//#endif
            }
        }

    }
}
