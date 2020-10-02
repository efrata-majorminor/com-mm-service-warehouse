using Com.MM.Service.Warehouse.Lib.Models.SPKDocsModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.MM.Service.Warehouse.Lib.Interfaces.PkbjInterfaces
{
    public interface IPkpbjFacade
    {
        Tuple<List<SPKDocs>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        Tuple<List<SPKDocs>, int, Dictionary<string, string>> ReadExpedition(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}");
        SPKDocs ReadById(int id);
        SPKDocs ReadByReference(string reference);
        Task<int> Create(SPKDocs model, string username, int clientTimeZoneOffset = 7);

    }
}
