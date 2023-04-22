using System.Web;
using System.Web.Mvc;

namespace Enoca.Task.RabbitMQ
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
