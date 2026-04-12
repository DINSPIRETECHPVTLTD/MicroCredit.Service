using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class MasterLookup
{
    public int Id { get; set; }

    public string LookupKey { get; set; } = null!;

    public string LookupCode { get; set; } = null!;

    public string LookupValue { get; set; } = null!;

    public decimal? NumericValue { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}
