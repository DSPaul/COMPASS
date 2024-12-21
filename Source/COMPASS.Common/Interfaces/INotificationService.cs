using System.Threading.Tasks;
using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces
{
    public interface INotificationService
    {
        void Show(Notification notification);
    }
}
