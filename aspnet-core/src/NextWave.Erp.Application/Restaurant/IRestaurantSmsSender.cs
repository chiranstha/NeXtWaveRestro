using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public interface IRestaurantSmsSender
    {
        Task SendAsync(string number, string message);
    }
}
