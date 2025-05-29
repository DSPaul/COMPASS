using System.Threading.Tasks;
using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Services
{
    public interface INotificationService
    {
        Task ShowDialog(Notification notification);
    }
}
