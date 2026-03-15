using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
