using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAutomation.Models
{
    public class LoanFields
    {
        [JsonProperty("Loan.LoanNumber")]
        public string LoanNumber { get; set; }

        [JsonProperty("Fields.19")]
        public string Field19 { get; set; }

        [JsonProperty("Fields.608")]
        public string Field608 { get; set; }

        [JsonProperty("Loan.LoanAmount")]
        public string LoanAmount { get; set; }

        [JsonProperty("Loan.LTV")]
        public string LTV { get; set; }

        [JsonProperty("Fields.976")]
        public string Field976 { get; set; }

        [JsonProperty("Loan.Address1")]
        public string Address1 { get; set; }

        [JsonProperty("Loan.City")]
        public string City { get; set; }

        [JsonProperty("Loan.State")]
        public string State { get; set; }

        [JsonProperty("Fields.15")]
        public string Field15 { get; set; }

        [JsonProperty("Fields.1041")]
        public string Field1041 { get; set; }

        [JsonProperty("Loan.OccupancyStatus")]
        public string OccupancyStatus { get; set; }

        [JsonProperty("Fields.1401")]
        public string Field1401 { get; set; }

        [JsonProperty("Fields.CX.VP.DOC.TYPE")]
        public string DocType { get; set; }

        [JsonProperty("Fields.4000")]
        public string Field4000 { get; set; }

        [JsonProperty("Fields.4002")]
        public string Field4002 { get; set; }

        [JsonProperty("Fields.CX.CREDITSCORE")]
        public string CreditScore { get; set; }

        [JsonProperty("Fields.325")]
        public string Field325 { get; set; }

        [JsonProperty("Fields.3")]
        public string Field3 { get; set; }

        [JsonProperty("Fields.742")]
        public string Field742 { get; set; }

        [JsonProperty("Fields.CX.VP.BUSINESS.PURPOSE")]
        public string BusinessPurpose { get; set; }

        [JsonProperty("Fields.1550")]
        public string Field1550 { get; set; }

        [JsonProperty("Fields.675")]
        public string Field675 { get; set; }

        [JsonProperty("Fields.QM.X23")]
        public string QM_X23 { get; set; }

        [JsonProperty("Fields.QM.X25")]
        public string QM_X25 { get; set; }

        [JsonProperty("Fields.2278")]
        public string Field2278 { get; set; }
    }
}
