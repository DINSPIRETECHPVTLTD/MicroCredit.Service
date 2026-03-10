using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.Branch
{
    public class UpdateBranchRequest
    {
        public string Name { get; private set; } = string.Empty;
        public string Address1 { get; private set; } = string.Empty;
        public string Address2 { get; private set; } = string.Empty;
        public string City { get; private set; } = string.Empty;
        public string State { get; private set; } = string.Empty;
        public string Country { get; private set; } = string.Empty;
        public string ZipCode { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
    }
}
