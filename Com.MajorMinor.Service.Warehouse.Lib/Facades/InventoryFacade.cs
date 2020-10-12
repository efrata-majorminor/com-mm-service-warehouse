using Com.MM.Service.Warehouse.Lib.Models.InventoryModel;
using Com.MM.Service.Warehouse.Lib.ViewModels.InventoryViewModel;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        #region Monitoring By User
        public IQueryable<InventoriesReportViewModel> GetReportQuery(string storageId, string itemName, int offset, string username)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.Inventories
                         where a.IsDeleted == false
                         && a.StorageId == Convert.ToInt64((string.IsNullOrWhiteSpace(storageId) ? a.StorageId.ToString() : storageId))
                         && a.ItemName == (string.IsNullOrWhiteSpace(itemName) ? a.ItemName : itemName)

                         select new InventoriesReportViewModel
                         {
                             ItemCode = a.ItemCode,
                             ItemName = a.ItemName,
                             ItemArticleRealizationOrder = a.ItemArticleRealizationOrder,
                             ItemSize = a.ItemSize,
                             ItemUom = a.ItemUom,
                             ItemDomesticSale = a.ItemDomesticSale,
                             Quantity = a.Quantity,
                             StorageId = a.StorageId,
                             StorageCode = a.StorageCode,
                             StorageName = a.StorageName
                         });
            return Query;
        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoriesReportViewModel>, int> GetReport(string storageId, string itemName, int page, int size, string Order, int offset, string username)
        {
            var Query = GetReportQuery(storageId, itemName, offset, username);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            // Pageable<InventoriesReportViewModel> pageable = new Pageable<InventoriesReportViewModel>(Query, page - 1, size);
            List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            // int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, Data.Count());
        }
        #endregion

        #region Monitoring By Search
        public IQueryable<InventoriesReportViewModel> GetSearchQuery(string itemCode, int offset, string username)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.Inventories
                         where a.IsDeleted == false
                         && a.ItemCode == (string.IsNullOrWhiteSpace(itemCode) ? a.ItemCode : itemCode)

                         select new InventoriesReportViewModel
                         {
                             ItemCode = a.ItemCode,
                             ItemName = a.ItemName,
                             ItemArticleRealizationOrder = a.ItemArticleRealizationOrder,
                             ItemSize = a.ItemSize,
                             ItemUom = a.ItemUom,
                             ItemDomesticSale = a.ItemDomesticSale,
                             Quantity = a.Quantity,
                             StorageId = a.StorageId,
                             StorageCode = a.StorageCode,
                             StorageName = a.StorageName
                         });
            return Query;
        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoriesReportViewModel>, int> GetSearch(string itemCode, int page, int size, string Order, int offset, string username)
        {
            var Query = GetSearchQuery(itemCode, offset, username);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            // Pageable<InventoriesReportViewModel> pageable = new Pageable<InventoriesReportViewModel>(Query, page - 1, size);
            List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            // int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, Data.Count());
        }
        #endregion

        public Inventory getStockPOS(string sourcecode, string itemCode)
        {
            var inventory = dbSet.Where(x => x.StorageCode == sourcecode && x.ItemCode == itemCode).FirstOrDefault();
            return inventory;

        }


        #region Monitoring Inventory Movements
        public IQueryable<InventoryMovementsReportViewModel> GetMovementQuery(string storageId, string itemCode, string info, int offset, string username)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from c in dbContext.InventoryMovements
                         where c.IsDeleted == false
                         //&& c.StorageId == (string.IsNullOrWhiteSpace(storageId) ? c.StorageId : storageId)
                         && c.StorageId == Convert.ToInt64((string.IsNullOrWhiteSpace(storageId) ? c.StorageId.ToString() : storageId))
                         && c.ItemCode == (string.IsNullOrWhiteSpace(itemCode) ? c.ItemCode : itemCode)
                         //&& a.ItemName == (string.IsNullOrWhiteSpace(info) ? a.ItemName : info)

                         select new InventoryMovementsReportViewModel
                         {
                             ItemCode = c.ItemCode,
                             ItemName = c.ItemName,
                             ItemArticleRealizationOrder = c.ItemArticleRealizationOrder,
                             ItemSize = c.ItemSize,
                             ItemUom = c.ItemUom,
                             ItemDomesticSale = c.ItemDomesticSale,
                             Quantity = c.Quantity,
                             Before = c.Before,
                             After = c.After,
                             Type = c.Type,
                             Reference = c.Reference,
                             Remark = c.Remark,
                             StorageId = c.StorageId,
                             StorageCode = c.StorageCode,
                             StorageName = c.StorageName,
                             CreatedUtc = c.CreatedUtc,
                         });
            return Query;
        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoryMovementsReportViewModel>, int> GetMovements(string storageId, string itemCode, string info, string Order, int offset, string username, int page = 1, int size = 25)
        {
            var Query = GetMovementQuery(storageId, itemCode, info, offset, username);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            Pageable<InventoryMovementsReportViewModel> pageable = new Pageable<InventoryMovementsReportViewModel>(Query, page - 1, size);
            //List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            List<InventoryMovementsReportViewModel> Data = pageable.Data.ToList<InventoryMovementsReportViewModel>();
            int TotalData = pageable.TotalCount;

            //return Tuple.Create(Data, Data.Count());
            return Tuple.Create(Data, TotalData);

        }
        #endregion
    }
}
