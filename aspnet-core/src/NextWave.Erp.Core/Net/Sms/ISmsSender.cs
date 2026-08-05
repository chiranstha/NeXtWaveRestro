using System.Threading.Tasks;

namespace NextWave.Erp.Net.Sms;

public interface ISmsSender
{
    Task SendAsync(string number, string message);
}

