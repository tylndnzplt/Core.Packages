using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Pipelines.Caching;

public interface ICacheRemoverRequest
{
    string CacheKey { get; }
    bool BypassCache { get; }
    //oluşturulan cache hangi grup içerisinde ? 
    string? CacheGroupKey { get; }
}
