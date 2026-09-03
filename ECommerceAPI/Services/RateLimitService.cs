using System.Data;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services;

public class RateLimitService
{
    private readonly DataContext _dataContext;

    public RateLimitService(DataContext dataContext)
        => _dataContext = dataContext;

    public async Task<RateLimitResultDTO> CheckAsync(string policyName,
                                                     string client)
    {
        RateLimitPolicy? policy = await _dataContext.RateLimitPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name.Equals(policyName)
                                      && p.Enabled);

        if (policy is null)
            return new RateLimitResultDTO { Allowed = true }; // no policy = no limit

        DateTime now = DateTime.UtcNow;
        DateTime windowStart = GetWindowStart(now, policy.WindowSeconds);
        DateTime windowEnd = windowStart.AddSeconds(policy.WindowSeconds);

        // we execute the transaction inside a strategy, otherwise EF can't retry in case of failure
        var strategy = _dataContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () => {
            // start a transaction to make changes atomic
            await using var transaction =
                await _dataContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            RateLimitCounter? counter = await _dataContext.RateLimitCounters
                .FirstOrDefaultAsync(c => c.PolicyName.Equals(policyName)
                                          && c.Client.Equals(client)
                                          && c.WindowStart == windowStart);

            // if there is no counter, create one
            if (counter is null)
            {
                counter = new RateLimitCounter
                {
                    PolicyName = policyName,
                    Client = client,
                    WindowStart = windowStart,
                    RequestCount = 1
                };

                _dataContext.RateLimitCounters.Add(counter);

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new RateLimitResultDTO
                {
                    Allowed = true,
                    Limit = policy.PermitLimit,
                    Remaining = policy.PermitLimit - 1,
                    WindowEnd = windowEnd
                };
            }

            // if count limit is exceeded, return don't allow
            if (counter.RequestCount >= policy.PermitLimit)
            {
                await transaction.CommitAsync();

                return new RateLimitResultDTO
                {
                    Allowed = false,
                    Limit = policy.PermitLimit,
                    Remaining = 0,
                    WindowEnd = windowEnd
                };
            }

            // otherwise, just increase the counter
            counter.RequestCount++;

            await _dataContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RateLimitResultDTO
            {
                Allowed = true,
                Limit = policy.PermitLimit,
                Remaining = policy.PermitLimit - counter.RequestCount,
                WindowEnd = windowEnd
            };
        });
    }

    private static DateTime GetWindowStart(DateTime now,
                                           int windowSeconds)
    {
        DateTime epoch = DateTime.UnixEpoch;
        long elapsedSeconds = (long)(now - epoch).TotalSeconds;
        long windowNumber = elapsedSeconds / windowSeconds;

        return epoch.AddSeconds(windowNumber * windowSeconds);
    }
}

