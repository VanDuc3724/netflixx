using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class CreateAddressViewModel
    {
        public int Id { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string AddressLine { get; set; }
        public string? PostalCode { get; set; }
    }
}