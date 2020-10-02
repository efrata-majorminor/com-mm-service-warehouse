using AutoMapper;
using Com.MM.Service.Warehouse.Lib.Helpers;
using Com.MM.Service.Warehouse.Lib.Models.Expeditions;
using Com.MM.Service.Warehouse.Lib.Models.InventoryModel;
using Com.MM.Service.Warehouse.Lib.Models.TransferModel;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.MM.Service.Warehouse.Lib.Facades
{
    public class ExpeditionFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<Expedition> dbSet;
        private readonly DbSet<Inventory> dbSetInventory;
        private readonly DbSet<InventoryMovement> dbSetInventoryMovement;
        private readonly DbSet<TransferOutDoc> dbSetTransfer;
        private readonly IServiceProvider serviceProvider;

        private readonly IMapper mapper;

        public ExpeditionFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<Expedition>();
            this.dbSetInventory = dbContext.Set<Inventory>();
            this.dbSetInventoryMovement = dbContext.Set<InventoryMovement>();
            this.dbSetTransfer = dbContext.Set<TransferOutDoc>();

            mapper = serviceProvider == null ? null : (IMapper)serviceProvider.GetService(typeof(IMapper));
        }

        public Tuple<List<Expedition>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<Expedition> Query = dbSet
                .Include(m => m.Items)
                    .ThenInclude(i => i.Details);

            List<string> searchAttributes = new List<string>()
            {
                "Code"
            };

            Query = QueryHelper<Expedition>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<Expedition>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<Expedition>.ConfigureOrder(Query, OrderDictionary);

            Pageable<Expedition> pageable = new Pageable<Expedition>(Query, Page - 1, Size);
            List<Expedition> Data = pageable.Data.ToList();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }

        public Expedition ReadById(int id)
        {
            var model = dbSet.Where(m => m.Id == id)
                 .Include(m => m.Items).ThenInclude(i => i.Details)
                 .FirstOrDefault();
            return model;
        }

        public string GenerateCode(string ModuleId)
        {
            var uid = ObjectId.GenerateNewId().ToString();
            var hashids = new Hashids(uid, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");
            var now = DateTime.Now;
            var begin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var diff = (now - begin).Milliseconds;
            string code = String.Format("{0}/{1}/{2}", hashids.Encode(diff), ModuleId, DateTime.Now.ToString("MM/yyyy"));
            return code;
        }
        public async Task<int> Create(Expedition model, string username, int clientTimeZoneOffset = 7)
        {
            int Created = 0;
            
            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    int totalweight = 0;
                    string code = GenerateCode("MM-KB/EXP");
                    
                    model.Code = code;
                    model.Date = DateTimeOffset.Now;
                    TransferOutDoc transferOutDoc = new TransferOutDoc();
                    foreach (var i in model.Items)
                    {
                        i.Id = 0;
                        totalweight += i.Weight;
                        string CodeTransferOut = GenerateCode("MM-KB/EXP");
                        var SPK = dbContext.SPKDocs.Where(x => x.PackingList == i.PackingList).Single();
                        SPK.IsDistributed = true;
                        transferOutDoc.Code = CodeTransferOut;
                        transferOutDoc.Reference = model.Code;
                        transferOutDoc.DestinationId = i.DestinationId;
                        transferOutDoc.DestinationCode = i.DestinationCode;
                        transferOutDoc.DestinationName = i.DestinationName;
                        transferOutDoc.Remark = model.Remark;
                        transferOutDoc.SourceId = i.SourceId;
                        transferOutDoc.SourceCode = i.SourceCode;
                        transferOutDoc.SourceName = i.SourceName;
                        List<TransferOutDocItem> transferOutDocItems = new List<TransferOutDocItem>();
                        foreach (var d in i.Details)
                        {
                            d.Id = 0;
                            var inven = dbContext.Inventories.Where(x => x.ItemArticleRealizationOrder == d.ArticleRealizationOrder && x.ItemCode == d.ItemCode && x.ItemName == d.ItemName && x.StorageId == i.SourceId).Single();
                            //inven.Quantity = inven.Quantity - d.SendQuantity;
                            
                            InventoryMovement movement = new InventoryMovement { 
                                After = inven.Quantity - d.SendQuantity,
                                Before = inven.Quantity,
                                Date = DateTimeOffset.Now,
                                ItemArticleRealizationOrder = d.ArticleRealizationOrder,
                                ItemCode = d.ItemCode,
                                ItemDomesticCOGS = d.DomesticCOGS,
                                ItemDomesticRetail = d.DomesticRetail,
                                ItemDomesticSale = d.DomesticSale,
                                ItemDomesticWholeSale = d.DomesticWholesale,
                                ItemInternationalCOGS = 0,
                                ItemInternationalRetail = 0,
                                ItemInternationalSale = 0,
                                ItemInternationalWholeSale = 0,
                                ItemId = d.ItemId,
                                ItemName = d.ItemName,
                                ItemSize = d.Size,
                                Quantity = d.Quantity,
                                Reference = CodeTransferOut,
                                Remark = d.Remark,
                                StorageCode = i.SourceCode,
                                StorageIsCentral = i.SourceName.Contains("GUDANG") ? true : false,
                                StorageId = i.SourceId,
                                StorageName = i.DestinationName,
                                Type = "OUT"
                            };

                            inven.Quantity = inven.Quantity - d.SendQuantity;
                            TransferOutDocItem transferItem = new TransferOutDocItem
                            {
                                ArticleRealizationOrder = d.ArticleRealizationOrder,
                                DomesticCOGS = d.DomesticCOGS,
                                DomesticRetail = d.DomesticRetail,
                                DomesticSale = d.DomesticSale,
                                DomesticWholeSale = d.DomesticWholesale,
                                ItemCode = d.ItemCode,
                                ItemId = d.ItemId,
                                ItemName = d.ItemName,
                                Quantity = d.Quantity,
                                Remark = d.Remark,
                                Size = d.Size,
                                Uom = d.Uom
                            };
                            EntityExtension.FlagForCreate(transferItem, username, USER_AGENT);
                            transferOutDocItems.Add(transferItem);
                            //transferOutDoc.Items.Add(transferItem);
                            //transferOutDoc.Items.Add(new TransferOutDocItem
                            //{
                            //    ArticleRealizationOrder = d.ArticleRealizationOrder
                            //    DomesticCOGS = d.DomesticCOGS,
                            //    DomesticRetail = d.DomesticRetail,
                            //    DomesticSale = d.DomesticSale,
                            //    DomesticWholeSale = d.DomesticWholesale,
                            //    ItemCode = d.ItemCode,
                            //    ItemId = d.ItemId,
                            //    ItemName = d.ItemName,
                            //    Quantity = d.Quantity,
                            //    Remark = d.Remark,
                            //    Size = d.Size,
                            //    Uom = d.Uom


                            //});
                            EntityExtension.FlagForCreate(d, username, USER_AGENT);
                            EntityExtension.FlagForCreate(movement, username, USER_AGENT);
                            this.dbSetInventoryMovement.Add(movement);
                        }
                        transferOutDoc.Items = transferOutDocItems;
                        EntityExtension.FlagForCreate(i, username, USER_AGENT);
                        EntityExtension.FlagForCreate(transferOutDoc, username, USER_AGENT);
                        this.dbSetTransfer.Add(transferOutDoc);
                        
                    }
                    model.Weight = totalweight;
                    model.Remark = "";
                    EntityExtension.FlagForCreate(model, username, USER_AGENT);

                    dbSet.Add(model);
                    Created = await dbContext.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Created;
        }
    }
}
