using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OpenPrinterWeb.Hubs
{
    public class PrinterHub : Hub
    {
        public async Task SendStatusUpdate(string message)
        {
             await Clients.All.SendAsync("ReceiveStatusUpdate", message);
        }
    }
}
