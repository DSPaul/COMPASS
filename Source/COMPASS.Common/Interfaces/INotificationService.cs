using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Interfaces
{
    public interface INotificationService
    {
        Task Show(Notification notification);
    }
}
