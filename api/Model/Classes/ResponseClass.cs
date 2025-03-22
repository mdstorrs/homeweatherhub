using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class ResponseClass
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
}
