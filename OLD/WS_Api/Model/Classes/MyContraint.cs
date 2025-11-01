using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class MyConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext,
                            IRouter router,
                            string parameterName,
                            RouteValueDictionary values,
                            RouteDirection routeDirection)
        {
            var url = values["key"];
            //your logic to check
            return true;
        }
    }

}
