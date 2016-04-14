using System.Collections.Generic;
using Data.Entities;
using System.Threading.Tasks;
using System;
using Data.Interfaces.DataTransferObjects;

namespace Hub.Interfaces
{
    public interface ITerminal
    {
        IEnumerable<TerminalDO> GetAll();

        Task<IList<ActivityTemplateDO>> GetAvailableActivities(string uri);

        TerminalDO GetByKey(int terminalId);
        TerminalDO RegisterOrUpdate(TerminalDO terminalDo);

        Task<TerminalDO> GetTerminalByPublicIdentifier(string terminalId);
        Task<bool> IsUserSubscribedToTerminal(string terminalId, string userId);
        Task<List<SolutionPageDTO>> GetSolutionDocumentations(string terminalName);
    }
}