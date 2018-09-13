using System.Collections.Generic;

namespace Lykke.Service.PaySettlement.Models
{
    public class ContinuationResult<T>
    {
        public IEnumerable<T> Entities { get; set; }
        public string ContinuationToken { get; set; }
    }
}
