using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryMVC.Helpers
{
    public static class ViewHelper
    {
        
        public static string IsActive(ViewContext viewContext, string controller)
        {
            if (viewContext.RouteData.Values["controller"]?.ToString() == controller)
            {
                return "active";
            }
            return "";
        }
    }
}