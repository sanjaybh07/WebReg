using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRegApiCore.Models
{
    public class regDetails
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public string clientId { get; set; }
        public string clientName { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string remarks { get; set; }
        public string gstNo { get; set; }
        public string acCode { get; set; }
        public DateTime? regnValidTill { get; set; }
        public DateTime? amcValidTill { get; set; }
        public int? hoAmcAmount { get; set; }
        public int? posAmcAmount { get; set; }
        public int? cloudRate { get; set; }
        public DateTime? startDate { get; set; }
        public string wizclipGrpCode { get; set; }
        public int? wizclipRate { get; set; }
        public int? frequency { get; set; }

        public List<Contact> regContacts { get; set; }

        public List<clientModules> regModules { get; set; }
        public List<clientLocations> regLocations { get; set; }
    }

    public class clientLocations
    {
        public string locationId { get; set; }
        public string locationName { get; set; }

        public string minXnDate { get; set; }
        public string maxXnDate { get; set; }
        public int? noOfUsers { get; set; }
        public string amcStartDate { get; set; }
        public int? amcAmount { get; set; }
        public int? wizclipRate { get; set; }
        public Boolean registered { get; set; }
        public String City { get; set; }
        public String State { get; set; }

        public String acCode { get; set; }


    }

    public class objDocs
    {
        public List<regDocs> docsData { get; set; }
    }
    public class regDocs
    {
        public string filePrefix { get; set; }
        public string fileName { get; set; }
        public string fileSize { get; set; }
        public string imageId { get; set; }
        public string docImage { get; set; }
    }

    public class posReg
    {
        public string clientId { get; set; }
        public string locationId { get; set; }
        public Boolean registered { get; set; }
        public int noOfUsers { get; set; }
    }

    public class objPosReg
    {
        public List<posReg> regPosData { get; set; }
    }
    public class clientModules
    {
        public string clientId { get; set; }
        public string moduleId { get; set; }

        public Boolean registered { get; set; }
    }

    public class objRegM
    {
        public List<Module> Modules { get; set; }
    }
    public class Module
    {
        public string productId { get; set; }

        public string moduleId { get; set; }
        public string moduleName { get; set; }
        public string moduleAlias { get; set; }
        public Boolean? active { get; set; }
    }

    public class objContacts
    {
        public List<Contact> Contacts { get; set; }
    }

    public class Contact
    {
        public String clientId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
        public string designation { get; set; }
        public bool inactive { get; set; }

        public string rowId { get; set; }
    }
}