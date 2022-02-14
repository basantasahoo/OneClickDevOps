using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneClickDevOpsGithub.Models
{
    public class AzureProduct
    {
        public string ProductName { get; set; }
        public string ImageSrc { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CF { get; set; }

    }

    public class CarbonFootPrintData
    {
        public string DataCenter { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal PUE { get; set; }
        public decimal KWH { get; set; }
    }

    public class resourceInstance
    {
        public string ResourceName { get; set; }
        public int Instance { get; set; }

        public decimal TotalCF { get; set; }

        public decimal TotalCO2 { get; set; }
    }

}
