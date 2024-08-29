using Microsoft.Extensions.ObjectPool;

namespace Genie.Common.Performance
{
    public class GeniePooledObject
    {
        public string EventChannel { get; set; } = Guid.NewGuid().ToString("N");
        public int Counter { get; set; }
    }
}
