using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Concurrent; 

namespace LibraryMVC.Hubs
{
    public class ReadingListHub : Hub
    {
        
        private static readonly ConcurrentDictionary<string, string> _userLists = new();

        public async Task SyncList(string userEmail, string listJson)
        {
            
            _userLists[userEmail] = listJson;

            
            await Clients.OthersInGroup(userEmail).SendAsync("ReceiveList", listJson);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userEmail = httpContext?.User?.Identity?.Name ?? "defaultUser";
            await Groups.AddToGroupAsync(Context.ConnectionId, userEmail);

            
            if (_userLists.TryGetValue(userEmail, out string listJson))
            {
                
                await Clients.Caller.SendAsync("ReceiveList", listJson);
            }

            await base.OnConnectedAsync();
        }

        
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var userEmail = httpContext?.User?.Identity?.Name ?? "defaultUser";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userEmail);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
