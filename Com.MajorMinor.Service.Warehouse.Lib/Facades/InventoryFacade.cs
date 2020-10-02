using Com.MM.Service.Warehouse.Lib.Models.InventoryModel;
using Com.MM.Service.Warehouse.Lib.ViewModels.InventoryViewModel;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.MM.Service.Warehouse.Lib.Facades
{
    public class InventoryFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<Inventory> dbSet;
        private readonly IServiceProvider serviceProvider;

       // private readonly string GarmentPreSalesContractUri = "merchandiser/garment-pre-sales-contracts/";

        public InventoryFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<Inventory>();
        }

        public IQueryable<InventoryViewModel> GetQuery(string itemCode, string storageCode)
        {
            //GarmentCorrectionNote garmentCorrectionNote = new GarmentCorrectionNote();
            //var garmentCorrectionNotes = dbContext.Set<GarmentCorrectionNote>().AsQueryable();



            var Query = (from a in dbContext.Inventories


                         where
                         a.ItemCode == itemCode
                         && a.StorageCode == storageCode
                         //&& z.CodeRequirment == (string.IsNullOrWhiteSpace(category) ? z.CodeRequirment : category)


                         select new InventoryViewModel
                         {
                             item = new ViewModels.NewIntegrationViewModel.ItemViewModel {
                                 code = a.ItemCode,
                                 articleRealizationOrder = a.ItemArticleRealizationOrder

                             }, //a.ItemCode,
                             //ItemArticleRealization = a.ItemArticleRealizationOrder,
                             //ItemDomesticCOGS = a.ItemDomesticCOGS,
                             quantity = a.Quantity
                                
                             //Price = a.Price

                         });

            return Query;
        }

        public Tuple<List<InventoryViewModel>, int> GetItemPack(string itemCode, string storageCode, string order, int page, int size)
        {
            var Query = GetQuery(itemCode, storageCode);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(order);
            //if (OrderDictionary.Count.Equals(0))
            //{
            //	Query = Query.OrderByDescending(b => b.poExtDate);
            //}

            Pageable<InventoryViewModel> pageable = new Pageable<InventoryViewModel>(Query, page - 1, size);
            List<InventoryViewModel> Data = pageable.Data.ToList<InventoryViewModel>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData);
        }

        public List<Inventory> getDatabyCode(string itemCode, int StorageId)
        {
            var inventory = dbSet.Where(x => x.ItemCode == itemCode && x.StorageId == StorageId).ToList();
            return inventory;

        }

        public List<Inventory> getDatabyName(string itemName, int StorageId)
        {
            var inventory = dbSet.Where(x => x.ItemName ==itemName && x.StorageId == StorageId).ToList();
            return inventory;

        }

        public Inventory getStock(int source, int item)
        {
            var inventory = dbSet.Where(x => x.StorageId == source && x.ItemId == item).FirstOrDefault();
            return inventory;

        }





    }
}
