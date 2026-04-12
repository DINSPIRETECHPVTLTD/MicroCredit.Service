using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public int OrgId { get; set; }

    public string Level { get; set; } = null!;

    public int? BranchId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<Branch> BranchCreatedByNavigations { get; set; } = new List<Branch>();

    public virtual ICollection<Branch> BranchModifiedByNavigations { get; set; } = new List<Branch>();

    public virtual ICollection<Center> CenterCreatedByNavigations { get; set; } = new List<Center>();

    public virtual ICollection<Center> CenterModifiedByNavigations { get; set; } = new List<Center>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> InverseModifiedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Investment> InvestmentCreatedBies { get; set; } = new List<Investment>();

    public virtual ICollection<Investment> InvestmentUsers { get; set; } = new List<Investment>();

    public virtual ICollection<LedgerTransaction> LedgerTransactionCreatedByNavigations { get; set; } = new List<LedgerTransaction>();

    public virtual ICollection<LedgerTransaction> LedgerTransactionPaidFromUsers { get; set; } = new List<LedgerTransaction>();

    public virtual ICollection<LedgerTransaction> LedgerTransactionPaidToUsers { get; set; } = new List<LedgerTransaction>();

    public virtual ICollection<Ledger> Ledgers { get; set; } = new List<Ledger>();

    public virtual ICollection<Loan> LoanCreatedByNavigations { get; set; } = new List<Loan>();

    public virtual ICollection<Loan> LoanModifiedByNavigations { get; set; } = new List<Loan>();

    public virtual ICollection<LoanScheduler> LoanSchedulerCollectedByNavigations { get; set; } = new List<LoanScheduler>();

    public virtual ICollection<LoanScheduler> LoanSchedulerCreatedByNavigations { get; set; } = new List<LoanScheduler>();

    public virtual ICollection<Member> MemberCreatedByNavigations { get; set; } = new List<Member>();

    public virtual ICollection<MemberMembershipFee> MemberMembershipFeeCollectedByNavigations { get; set; } = new List<MemberMembershipFee>();

    public virtual ICollection<MemberMembershipFee> MemberMembershipFeeCreatedByNavigations { get; set; } = new List<MemberMembershipFee>();

    public virtual ICollection<MemberMembershipFee> MemberMembershipFeeModifiedByNavigations { get; set; } = new List<MemberMembershipFee>();

    public virtual ICollection<Member> MemberModifiedByNavigations { get; set; } = new List<Member>();

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Organization Org { get; set; } = null!;

    public virtual ICollection<Organization> OrganizationCreatedByNavigations { get; set; } = new List<Organization>();

    public virtual ICollection<Organization> OrganizationModifiedByNavigations { get; set; } = new List<Organization>();

    public virtual ICollection<Poc> PocCollectionByNavigations { get; set; } = new List<Poc>();

    public virtual ICollection<Poc> PocCreatedByNavigations { get; set; } = new List<Poc>();

    public virtual ICollection<Poc> PocModifiedByNavigations { get; set; } = new List<Poc>();
}
