using System.Threading.Tasks;

namespace FIGCommon.Interfaces
{
    public interface IMessageService
    {
        Task Send(string email, string subject, string message);
    }
}
