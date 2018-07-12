using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace N17Solutions.Microphobia.Dashboard
{
    public class MicrophobiaControllerFeatureProvider<TController> : ControllerFeatureProvider
        where TController : ControllerBase
    {
        protected override bool IsController(TypeInfo typeInfo)
        {
            return typeof(TController).GetTypeInfo().IsAssignableFrom(typeInfo) && base.IsController(typeInfo);
        }
    }
    
    public abstract class DashboardController : ControllerBase {}
}