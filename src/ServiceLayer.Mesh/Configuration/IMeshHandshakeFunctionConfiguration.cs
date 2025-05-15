using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Mesh.Configuration
{
    public interface IMeshHandshakeFunctionConfiguration
    {
        string NbssMeshMailboxId { get; }
    }
}
