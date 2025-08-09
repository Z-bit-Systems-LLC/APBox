using Microsoft.AspNetCore.SignalR;
using ApBox.Core.PacketTracing.Services;
using ApBox.Core.PacketTracing.Models;
using ApBox.Core.PacketTracing;

namespace ApBox.Web.Hubs
{
    public class PacketTraceHub : Hub
    {
        private readonly IPacketTraceService _traceService;
        
        public PacketTraceHub(IPacketTraceService traceService)
        {
            _traceService = traceService;
        }
        
        public async Task StartTracing(string readerId)
        {
            _traceService.StartTracing(readerId);
            await Clients.All.SendAsync("TracingStarted", readerId);
        }
        
        public async Task StopTracing(string readerId)
        {
            _traceService.StopTracing(readerId);
            await Clients.All.SendAsync("TracingStopped", readerId);
        }
        
        public async Task SendPacketTrace(PacketTraceEntry entry)
        {
            await Clients.All.SendAsync("PacketReceived", entry);
        }
        
        public async Task SendStatistics(TracingStatistics stats)
        {
            await Clients.All.SendAsync("StatisticsUpdated", stats);
        }
        
        public override async Task OnConnectedAsync()
        {
            // Send current state to new client
            var stats = _traceService.GetStatistics();
            await Clients.Caller.SendAsync("StatisticsUpdated", stats);
            
            await base.OnConnectedAsync();
        }
    }
}