using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NextWave.Erp.Authorization.QrLogin;

public class QrLoginHub : Hub
{
    private readonly IQrLoginManager _qrLoginManager;
    private readonly QrLoginImageService _qrLoginImageService;

    public QrLoginHub(IQrLoginManager qrLoginManager, QrLoginImageService qrLoginImageService)
    {
        _qrLoginManager = qrLoginManager;
        _qrLoginImageService = qrLoginImageService;
    } 
    
    public async Task SetSessionId()
    {
        var sessionId = await _qrLoginManager.GenerateSessionId(Context.ConnectionId);
        
        var qrCodeUrl = _qrLoginImageService.GenerateSetupCode(Context.ConnectionId, sessionId);
        
        await Clients.Caller.SendAsync("generateQrCode", qrCodeUrl);
    }
}
