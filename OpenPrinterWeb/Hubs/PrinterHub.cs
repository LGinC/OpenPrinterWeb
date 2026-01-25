using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Hubs
{
    [Authorize]
    public class PrinterHub : Hub
    {
        public async Task SendStatusUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", message);
        }
    }
}
