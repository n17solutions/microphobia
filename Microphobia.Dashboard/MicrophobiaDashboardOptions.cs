using System;

namespace N17Solutions.Microphobia.Dashboard
{
    public class MicrophobiaDashboardOptions
    {
        /// <summary>
        /// Gets or sets a route prefix for accessing the microphoia dashboard
        /// </summary>
        public string RoutePrefix { get; set; } = "microphobia";

        /// <summary>
        /// Gets or sets a title for the microphobia dashboard page
        /// </summary>
        public string DocumentTitle { get; set; } = "Microphobia Dashboard";
        
        /// <summary>
        /// Gets or sets a Uri to use for the Dashboard.
        /// </summary>
        /// <remarks>Used when hosting in a stand-alone environment</remarks>
        public Uri DashboardUri { get; set; } = new Uri("http://0.0.0.0:5000");
    }
}