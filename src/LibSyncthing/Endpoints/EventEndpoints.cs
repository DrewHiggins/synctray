using LibSyncthing.Models;

namespace LibSyncthing.Endpoints;

public static class EventEndpoints
{
    public static Task<List<Event>> GetEventsAsync(this SyncthingClient client, long since, CancellationToken ct = default)
        => client.GetAsync<List<Event>>($"/rest/events?since={since}&timeout=60", ct);
}
