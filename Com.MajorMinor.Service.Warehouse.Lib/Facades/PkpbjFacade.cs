using System;
using System.Collections.Generic;
using System.Linq;
using Com.MM.Service.Warehouse.Lib.Helpers;
using Com.MM.Service.Warehouse.Lib.Models.SPKDocsModel;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Com.MM.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
using Com.Moonlay.Models;
using MongoDB.Bson;
using HashidsNet;
using Com.MM.Service.Warehouse.Lib.Interfaces;
using Com.MM.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;
using System.Net.Http;
using System.Text;
using Com.MM.Service.Warehouse.Lib.Models.TransferModel;
using Com.MM.Service.Warehouse.Lib.Interfaces.PkbjInterfaces;

namespace Com.MM.Service.Warehouse.Lib.Facades
{
    public class PkpbjFacade : IPkpbjFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        public readonly IServiceProvider serviceProvider;
        private readonly DbSet<SPKDocs> dbSet;
        private readonly DbSet<TransferOutDoc> dbSetTransferOut;

        public object Request { get; private set; }
        public object ApiVersion { get; private set; }

        public PkpbjFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<SPKDocs>();
        }

        public Tuple<List<SPKDocs>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<SPKDocs> Query = this.dbSet.Include(x=>x.Items);
            
            List<string> searchAttributes = new List<string>()
            {
                "PackingList", "SourceName", "DestinationName"
            };
            
            Query = QueryHelper<SPKDocs>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<SPKDocs>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<SPKDocs>.ConfigureOrder(Query, OrderDictionary);

            Pageable<SPKDocs> pageable = new Pageable<SPKDocs>(Query, Page - 1, Size);
            List<SPKDocs> Data = pageable.Data.ToList<SPKDocs>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }

        public Tuple<List<SPKDocs>, int, Dictionary<string, string>> ReadExpedition(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<SPKDocs> Query = this.dbSet.Include(x=>x.Items);

            List<string> searchAttributes = new List<string>()
            {
                "PackingList", "SourceName", "DestinationName"
            };

            Query = QueryHelper<SPKDocs>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<SPKDocs>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<SPKDocs>.ConfigureOrder(Query, OrderDictionary);

            Pageable<SPKDocs> pageable = new Pageable<SPKDocs>(Query, Page - 1, Size);
            List<SPKDocs> Data = pageable.Data.ToList<SPKDocs>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }
        public SPKDocs ReadById(int id)
        {
            var a = this.dbSet.Where(p => p.Id == id)
                .Include(p => p.Items)
                .FirstOrDefault();
            return a;
        }
        public SPKDocs ReadByReference(string reference)
        {
            var model = dbSet.Where(m => m.Reference == reference && m.DestinationCode != "GTM.01")
                 .Include(m => m.Items)
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
        public async Task<int> Create(SPKDocs model, string username, int clientTimeZoneOffset = 7)
        {
            int Created = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    string packingList = GenerateCode("MM-KB/PLB");
                    string code = GenerateCode("MM-PK/PBJ");
                    string password = String.Join("",GenerateCode(DateTime.Now.ToString("dd")).Split("/"));
                    //(generateCode(("0" + date.getDate()).slice(-2))).split('/').join('')

                    foreach (var i in model.Items)
                    {
                        EntityExtension.FlagForCreate(i, username, USER_AGENT);
                        var inven = GetItems(i.ItemCode, i.ItemName, i.ItemArticleRealizationOrder);
                        if (inven == null)
                        {
                            ItemCoreViewModel item = new ItemCoreViewModel
                            {
                                dataDestination = new List<ItemViewModelRead> {
                                    new ItemViewModelRead{
                                        ArticleRealizationOrder = i.ItemArticleRealizationOrder,
                                        code = i.ItemCode,
                                        name = i.ItemName,
                                        Remark = i.Remark,
                                        Size = i.ItemSize,
                                        Uom = i.ItemUom,

                                    }

                                },
                                DomesticCOGS = i.ItemDomesticCOGS,
                                DomesticRetail = i.ItemDomesticRetail,
                                DomesticSale = i.ItemDomesticSale,
                                DomesticWholesale = i.ItemDomesticWholesale
                            };
                            
                            string itemsUri = "items/finished-goods";
                            var httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
                            var response = await httpClient.PostAsync($"{APIEndpoint.Core}{itemsUri}", new StringContent(JsonConvert.SerializeObject(item).ToString(), Encoding.UTF8, General.JsonMediaType));

                            response.EnsureSuccessStatusCode();

                        }
                        

                    }
                    model.Code = code;
                    model.PackingList = packingList;
                    model.Password = password;
                    model.IsReceived = false;
                    model.IsDraft = false;
                    model.IsDistributed = false;
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

        private ItemCoreViewModel GetItems(string code, string name, string ro)
        {
            string itemUri = "items/finished-goods/readinven";
            string queryUri = "?code=" + code + "&ro=" + ro + "&name=" + name;
            string uri = itemUri + queryUri;
            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
            var response = httpClient.GetAsync($"{APIEndpoint.Core}{uri}").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                ItemCoreViewModel viewModel = JsonConvert.DeserializeObject<ItemCoreViewModel>(result.GetValueOrDefault("data").ToString());
                return viewModel;//.Where(x => x.dataDestination[0].name == name && x.dataDestination[0].code == code).FirstOrDefault();
                //throw new Exception(string.Format("{0}, {1}, {2}", response.StatusCode, response.Content, APIEndpoint.Purchasing));
            }
            else
            {
                return null;
            }
        }
    }
}
