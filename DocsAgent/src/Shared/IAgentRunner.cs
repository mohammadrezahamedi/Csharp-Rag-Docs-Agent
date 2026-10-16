using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsAgent.Shared;

public interface IAgentRunner
{
    Task<string> AskAsync(string question);
}