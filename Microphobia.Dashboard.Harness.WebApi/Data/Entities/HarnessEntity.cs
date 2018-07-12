using System;

namespace Microphobia.Dashboard.Harness.WebApi.Data.Entities
{
    public class HarnessEntity
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}