using System;
using System.Collections.Generic;
using System.Text;

namespace Com.MM.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel
{
    public class StorageViewModel
    {
        public long _id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public bool isCentral { get; set; }
        
    }
}
