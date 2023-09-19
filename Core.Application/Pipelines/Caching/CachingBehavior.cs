﻿using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Application.Pipelines.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICachableRequest // bu request'te ICachableRequest varsa çalış !!
{
    private readonly CacheSettings _cacheSettings;
    /// <summary>
    /// Farklı cache ortamlarını (ınmemory veya redis gibi) geçebiliyoruz bununla
    /// </summary>
    private readonly IDistributedCache _cache;

    public CachingBehavior(IDistributedCache cache, IConfiguration configuration) //IConfiguration configuration 
    {
        _cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? throw new InvalidOperationException(); //Download --> ....Get nuget package configurationbinder
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        //eğer BypassCache derse cachei atlarsa
        if (request.BypassCache)
        {
            return await next(); //direkt çalıştır.
        }

        TResponse response;
        byte[]? cachedResponse = await _cache.GetAsync(request.CacheKey, cancellationToken); //cacheler bytearray şeklinde tutulur ve nullebla olabilir.

        if (cachedResponse != null)
        {
            response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse)); //varsa cachedki datayı deserialze et bytearray tutuluyor çünkü
        }
        else
        {
            //cache'de yoksa datayı hem cache'e ekle hemde döndür.
            response = await getResponseAndAddToCache(request,next,cancellationToken); 
        }

        return response;
    }

    private async Task<TResponse?> getResponseAndAddToCache(TRequest request, 
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response = await next();

        TimeSpan slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromDays(_cacheSettings.SlidingExpiration); //veritabanında manuel işlem yapıyorsan sıkıntı bu
        DistributedCacheEntryOptions cacheOptions = new() { SlidingExpiration = slidingExpiration };

        byte[] serializeData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response)); //cache e atacağın için byte a çevir

        await _cache.SetAsync(request.CacheKey, serializeData, cacheOptions, cancellationToken);

        if(request.CacheGroupKey != null)
        {
            await addCacheKeyToGroup(request, slidingExpiration, cancellationToken);
        }

        return response;
    }

    private async Task addCacheKeyToGroup(TRequest request, TimeSpan slidingExpiration, CancellationToken cancellationToken)
    {
        byte[]? cacheGroupCache = await _cache.GetAsync(key: request.CacheGroupKey!, cancellationToken);
        HashSet<string> cacheKeysInGroup;
        if (cacheGroupCache != null)
        {
            cacheKeysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cacheGroupCache))!;
            if (!cacheKeysInGroup.Contains(request.CacheKey))
                cacheKeysInGroup.Add(request.CacheKey);
        }
        else
            cacheKeysInGroup = new HashSet<string>(new[] { request.CacheKey });
        byte[] newCacheGroupCache = JsonSerializer.SerializeToUtf8Bytes(cacheKeysInGroup);

        byte[]? cacheGroupCacheSlidingExpirationCache = await _cache.GetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            cancellationToken
        );
        int? cacheGroupCacheSlidingExpirationValue = null;
        if (cacheGroupCacheSlidingExpirationCache != null)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(Encoding.Default.GetString(cacheGroupCacheSlidingExpirationCache));
        if (cacheGroupCacheSlidingExpirationValue == null || slidingExpiration.TotalSeconds > cacheGroupCacheSlidingExpirationValue)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(slidingExpiration.TotalSeconds);
        byte[] serializeCachedGroupSlidingExpirationData = JsonSerializer.SerializeToUtf8Bytes(cacheGroupCacheSlidingExpirationValue);

        DistributedCacheEntryOptions cacheOptions =
            new() { SlidingExpiration = TimeSpan.FromSeconds(Convert.ToDouble(cacheGroupCacheSlidingExpirationValue)) };

        await _cache.SetAsync(key: request.CacheGroupKey!, newCacheGroupCache, cacheOptions, cancellationToken);

        await _cache.SetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            serializeCachedGroupSlidingExpirationData,
            cacheOptions,
            cancellationToken
        );
    }

}
