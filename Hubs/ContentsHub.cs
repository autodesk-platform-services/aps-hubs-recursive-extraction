using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fileInfoExtract.Hubs
{
  public class ContentsHub : Microsoft.AspNetCore.SignalR.Hub
  {
    public async static Task SendContents(IHubContext<ContentsHub> hub, string connectionId, string jobGuid, string dataType, string projectGuid, string parentFolderId)
    {
      await hub.Clients.Client(connectionId).SendAsync("ReceiveContents", jobGuid, dataType, projectGuid, parentFolderId);
    }
  }
}