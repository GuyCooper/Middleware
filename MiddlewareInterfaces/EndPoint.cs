using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    public interface IEndpoint
    {
        string Id { get; }
        void SendData(Message message);
        void OnError(Message message, string error);
        void OnSucess(Message message);
    }
}
