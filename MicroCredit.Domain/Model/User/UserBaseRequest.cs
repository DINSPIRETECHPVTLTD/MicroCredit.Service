using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Model.User;

public class UserBaseRequest
{
    public string FirstName { get; private set; } = string.Empty;

    public string SurName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Address1 { get; private set; }
    public string? Address2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PinCode { get; private set; }
    public UserLevel Level { get; private set; }

    public int? BranchId { get; private set; }
}
