using System.Threading.Tasks;

namespace Com.MM.Service.Warehouse.Lib.Interfaces
{
    public interface ICreateable
    {
        Task<int> Create(object model);
    }
}
